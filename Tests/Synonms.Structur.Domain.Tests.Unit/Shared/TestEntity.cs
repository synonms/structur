using Synonms.Structur.Domain.Entities;

namespace Synonms.Structur.Domain.Tests.Unit.Shared;

internal class TestEntity : Entity<TestEntity>
{
    public TestEntity() : this(EntityId<TestEntity>.New(), string.Empty)
    {
    }
                                                      
    public TestEntity(EntityId<TestEntity> id, string someProperty) 
    {
        Id = id;
        SomeProperty = someProperty;
    }
                                                              
    public string SomeProperty { get; }
}