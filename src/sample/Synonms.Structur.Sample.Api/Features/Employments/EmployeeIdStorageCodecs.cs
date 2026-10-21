using Synonms.Structur.Core.Entities;
using Synonms.Structur.Domain.Persistence;
using Synonms.Structur.Sample.Api.Features.Employees;

namespace Synonms.Structur.Sample.Api.Features.Employments;

public sealed class EmployeeIdStorageCodec : IStorageValueCodec<EntityId<Employee>, Guid>
{
    public Guid Capture(EntityId<Employee> value) => value.Value;

    public EntityId<Employee> Restore(Guid value) => new(value);
}

public sealed class OptionalEmployeeIdStorageCodec : IStorageValueCodec<EntityId<Employee>?, Guid>
{
    public Guid Capture(EntityId<Employee>? value) =>
        value?.Value ?? throw new ArgumentNullException(nameof(value));

    public EntityId<Employee>? Restore(Guid value) => new(value);
}
