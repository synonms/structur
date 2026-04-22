using Synonms.Structur.Api.Core.Schema.Resources;
using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Aggregates;

namespace Synonms.Structur.Api.Server.Domain;

public static class AggregateMemberExtensions
{
    public static Maybe<Fault> MergeChanges<TAggregateMember, TChildResource>(this ICollection<TAggregateMember> aggregateMemberCollection, 
        IEnumerable<TChildResource> updatedResources, 
        Func<TChildResource, OneOf<TAggregateMember, IEnumerable<DomainRuleFault>>> createFunc,
        Func<TAggregateMember, TChildResource, Maybe<Fault>> updateFunc)
        where TAggregateMember : AggregateMember<TAggregateMember>
        where TChildResource : ChildResource
    {
        List<TChildResource> updatedResourcesList = updatedResources.ToList();
        
        List<TAggregateMember> aggregateMembersToDelete = aggregateMemberCollection
            .Where(aggregateMember => updatedResourcesList.All(updatedResource => updatedResource.Id != aggregateMember.Id.Value))
            .ToList();

        foreach (TAggregateMember aggregateMemberToDelete in aggregateMembersToDelete)
        {
            if (aggregateMemberCollection.Remove(aggregateMemberToDelete) is false)
            {
                return new InternalFault($"Failed to remove {nameof(TAggregateMember)} id [{aggregateMemberToDelete.Id.Value}].");
            }
        }

        foreach (TAggregateMember aggregateMember in aggregateMemberCollection)
        {
            TChildResource? matchingResource = updatedResourcesList.SingleOrDefault(x => x.Id == aggregateMember.Id.Value);

            if (matchingResource is not null)
            {
                Fault? editFault = updateFunc(aggregateMember, matchingResource).Match(fault => fault, () => null as Fault);

                if (editFault is not null)
                {
                    return editFault;
                }
            }
        }

        List<TChildResource> resourcesToAdd = updatedResourcesList
            .Where(updatedResource => aggregateMemberCollection.All(aggregateMember => aggregateMember.Id.Value != updatedResource.Id))
            .ToList();

        return resourcesToAdd
            .Select(createFunc)
            .Reduce(aggregateMembersToAdd => aggregateMembersToAdd)
            .Match(
                aggregateMembersToAdd =>
                {
                    foreach (TAggregateMember aggregateMember in aggregateMembersToAdd)
                    {
                        aggregateMemberCollection.Add(aggregateMember);
                    }

                    return Maybe<Fault>.None;
                },
                faults => new DomainRulesFault(faults));
    }
}