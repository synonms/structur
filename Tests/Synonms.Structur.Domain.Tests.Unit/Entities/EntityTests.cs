using Synonms.Structur.Core.Entities;
using Synonms.Structur.Domain.Tests.Unit.Shared;

namespace Synonms.Structur.Domain.Tests.Unit.Entities;

public class EntityTests
{
    [Fact]
    public void Construction_Default_NewIdIsGenerated()
    {
        TestEntity entity = new ();
        
        Assert.False(entity.Id.IsEmpty);
        Assert.NotEqual(Guid.Empty, entity.Id.Value);
    }
    
    [Fact]
    public void Equality_DifferentId_ReturnsNotEqual()
    {
        const string someProperty = "test";
        TestEntity entity1 = new(EntityId<TestEntity>.New(), someProperty);
        TestEntity entity2 = new(EntityId<TestEntity>.New(), someProperty);
        
        Assert.False(entity1 == entity2);
        Assert.False(entity2 == entity1);
        Assert.True(entity1 != entity2);
        Assert.True(entity2 != entity1);
        Assert.False(entity1.Equals(entity2));
        Assert.False(entity2.Equals(entity1));
    }

    [Fact]
    public void Equality_EmptyIds_ReturnsNotEqual()
    {
        const string someProperty = "test";
        TestEntity entity1 = new(new EntityId<TestEntity>(Guid.Empty), someProperty);
        TestEntity entity2 = new(new EntityId<TestEntity>(Guid.Empty), someProperty);
        
        Assert.False(entity1 == entity2);
        Assert.False(entity2 == entity1);
        Assert.True(entity1 != entity2);
        Assert.True(entity2 != entity1);
        Assert.False(entity1.Equals(entity2));
        Assert.False(entity2.Equals(entity1));
    }
    
    [Fact]
    public void Equality_OneNull_ReturnsNotEqual()
    {
        TestEntity entity = new(EntityId<TestEntity>.New(), "test");
        
        Assert.False(entity == null);
        Assert.False(null == entity);
        Assert.True(entity != null);
        Assert.True(null != entity);
        Assert.False(entity!.Equals(null));
    }
    
    [Fact]
    public void Equality_SameReference_ReturnsEqual()
    {
        TestEntity entity1 = new(EntityId<TestEntity>.New(), "test");
        TestEntity entity2 = entity1;
        
        Assert.True(entity1 == entity2);
        Assert.True(entity2 == entity1);
        Assert.False(entity1 != entity2);
        Assert.False(entity2 != entity1);
        Assert.True(entity1.Equals(entity2));
        Assert.True(entity2.Equals(entity1));
    }

    [Fact]
    public void Equality_SameTypeAndId_ReturnsEqual()
    {
        EntityId<TestEntity> id = EntityId<TestEntity>.New();
        TestEntity entity1 = new(id, "one");
        TestEntity entity2 = new(id, "two");
        
        Assert.True(entity1 == entity2);
        Assert.True(entity2 == entity1);
        Assert.False(entity1 != entity2);
        Assert.False(entity2 != entity1);
        Assert.True(entity1.Equals(entity2));
        Assert.True(entity2.Equals(entity1));
    }
    
    [Fact]
    public void HashCode_DifferentId_ReturnsDifferentCode()
    {
        const string someProperty = "test";
        TestEntity entity1 = new(EntityId<TestEntity>.New(), someProperty);
        TestEntity entity2 = new(EntityId<TestEntity>.New(), someProperty);
        
        Assert.NotEqual(entity1.GetHashCode(), entity2.GetHashCode());
    }
    
    [Fact]
    public void HashCode_SameTypeAndId_ReturnsSameCode()
    {
        EntityId<TestEntity> id = EntityId<TestEntity>.New();
        TestEntity entity1 = new(id, "one");
        TestEntity entity2 = new(id, "two");
        
        Assert.Equal(entity1.GetHashCode(), entity2.GetHashCode());
    }
}