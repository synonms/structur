using Synonms.Structur.Api.Core.Schema.Resources;
using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Aggregates;

namespace Synonms.Structur.Api.Server.Domain;

public static class AggregateMemberExtensions
{
    public static Maybe<Fault> MergeChanges<TAggregateMember, TChildResource>(
        this ICollection<TAggregateMember> aggregateMemberCollection,
        IEnumerable<TChildResource> updatedResources,
        ResourceMetadata metadata,
        string propertyName,
        Func<TChildResource, Guid, OneOf<TAggregateMember, IEnumerable<DomainRuleFault>>> createFunc,
        Func<TAggregateMember, TChildResource, Maybe<Fault>> updateFunc)
        where TAggregateMember : AggregateMember<TAggregateMember> =>
        MergeChanges(
            aggregateMemberCollection,
            updatedResources.Select((resource, index) => (Resource: resource, Id: metadata.GetOrAddEmbeddedMetadata(resource!, $"{propertyName}[{index}]").Id)),
            createFunc,
            updateFunc);

    private static Maybe<Fault> MergeChanges<TAggregateMember, TChildResource>(
        ICollection<TAggregateMember> aggregateMemberCollection,
        IEnumerable<(TChildResource Resource, Guid Id)> updatedResources,
        Func<TChildResource, Guid, OneOf<TAggregateMember, IEnumerable<DomainRuleFault>>> createFunc,
        Func<TAggregateMember, TChildResource, Maybe<Fault>> updateFunc)
        where TAggregateMember : AggregateMember<TAggregateMember>
    {
        List<(TChildResource Resource, Guid Id)> updatedResourcesList = updatedResources.ToList();

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
            TChildResource? matchingResource = updatedResourcesList
                .Where(updatedResource => updatedResource.Id == aggregateMember.Id.Value)
                .Select(updatedResource => updatedResource.Resource)
                .SingleOrDefault();

            if (matchingResource is not null)
            {
                Fault? editFault = updateFunc(aggregateMember, matchingResource).Match(fault => fault, () => null as Fault);

                if (editFault is not null)
                {
                    return editFault;
                }
            }
        }

        List<(TChildResource Resource, Guid Id)> resourcesToAdd = updatedResourcesList
            .Where(updatedResource => aggregateMemberCollection.All(aggregateMember => aggregateMember.Id.Value != updatedResource.Id))
            .ToList();

        return resourcesToAdd
            .Select(updatedResource => createFunc(updatedResource.Resource, updatedResource.Id))
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
