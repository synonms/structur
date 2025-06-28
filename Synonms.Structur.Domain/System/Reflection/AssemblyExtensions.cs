using System.Reflection;

namespace Synonms.Structur.Domain.System.Reflection;

public static class AssemblyExtensions
{
    public static IEnumerable<Type> GetAggregateRoots(this Assembly assembly) =>
        assembly.GetTypes().Where(x => x.IsAggregateRoot());

    public static IEnumerable<Type> GetAggregateMembers(this Assembly assembly) =>
        assembly.GetTypes().Where(x => x.IsAggregateMember());
}