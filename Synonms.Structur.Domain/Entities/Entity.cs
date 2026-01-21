using System.Linq.Expressions;
using System.Reflection;
using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Faults;
using Synonms.Structur.Domain.Validation;
using Synonms.Structur.Domain.ValueObjects;

namespace Synonms.Structur.Domain.Entities;

public static class Entity
{
    public static EntityBuilder<TEntity> CreateBuilder<TEntity>() where TEntity : Entity<TEntity> => new();
}

public abstract class Entity<TEntity>
    where TEntity : Entity<TEntity>
{
    protected Entity()
    {
        Id = EntityId<TEntity>.New(); 
    }

    protected Entity(EntityId<TEntity> id, UserAction createdAction)
    {
        Id = id;
        CreatedAction = createdAction;
    }
    
    public EntityId<TEntity> Id { get; protected init; }

    public UserAction CreatedAction { get; protected init; }

    public UserAction? UpdatedAction { get; protected set; }

    public UserAction? DeletedAction { get; protected set; }

    public Maybe<Fault> Delete(UserActionDto deletedActionDto) =>
        UserAction.CreateMandatory(nameof(DeletedAction), deletedActionDto)
            .Match(
                userAction =>
                {
                    DeletedAction = userAction;
                    return Maybe<Fault>.None;
                }, 
                domainRuleFaults => new DomainRulesFault(domainRuleFaults));
    
    protected virtual void MarkAsUpdated(UserAction updatedAction)
    {
        UpdatedAction = updatedAction;
    }
    
    protected void UpdateMandatoryValue<T>(Expression<Func<TEntity, T>> property, T newValue, UserAction updatedAction) where T : notnull
    {
        if (TryGetMandatoryValue(property, out T originalValue) is false)
        {
            return;
        }
        
        if (newValue.Equals(originalValue))
        {
            return;
        }

        TrySetValue(property, newValue, updatedAction);
    }
    
    protected void UpdateOptionalValue<T>(Expression<Func<TEntity, T?>> property, T? newValue, UserAction updatedAction)
    {
        if (TryGetOptionalValue(property, out T? originalValue) is false)
        {
            return;
        }

        if (newValue is null && originalValue is null)
        {
            return;
        }

        if (newValue is not null && originalValue is not null && newValue.Equals(originalValue))
        {
            return;
        }

        TrySetValue(property, newValue, updatedAction);
    }
    
    public override bool Equals(object? obj)
    {
        if ((obj is Entity<TEntity> other) is false)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (GetType() != other.GetType())
        {
            return false;
        }

        if (Id.IsEmpty || other.Id.IsEmpty)
        {
            return false;
        }

        return Id.Equals(other.Id);
    }

    public override int GetHashCode()
    {
        return (GetType().ToString() + Id).GetHashCode();
    }

    public static bool operator ==(Entity<TEntity>? left, Entity<TEntity>? right)
    {
        if (ReferenceEquals(left, null) && ReferenceEquals(right, null))
        {
            return true;
        }

        if (ReferenceEquals(left, null) || ReferenceEquals(right, null))
        {
            return false;
        }

        return left.Equals(right);
    }

    public static bool operator !=(Entity<TEntity>? left, Entity<TEntity>? right) =>
        !(left == right);
    
    
    private static bool TryGetPropertyInfo<T>(Expression<Func<TEntity, T>> property, out PropertyInfo? propertyInfo)
    {
        propertyInfo = null;
        
        if (property.Body is not MemberExpression memberExpression)
        {
            return false;
        }

        if (memberExpression.Member is not PropertyInfo memberExpressionAsPropertyInfo)
        {
            return false;
        }

        propertyInfo = memberExpressionAsPropertyInfo;
        return true;
    }
    
    private bool TryGetMandatoryValue<T>(Expression<Func<TEntity, T>> property, out T value)
    {
        value = default;

        if (TryGetPropertyInfo(property, out PropertyInfo? propertyInfo) is false || propertyInfo is null)
        {
            return false;
        }
        
        object? extractedValue = propertyInfo.GetValue(this);

        if (extractedValue is not T extractedValueAsT)
        {
            return false;
        }

        value = extractedValueAsT;
        return true;
    }
    
    private bool TryGetOptionalValue<T>(Expression<Func<TEntity, T?>> property, out T? value)
    {
        value = default;

        if (TryGetPropertyInfo(property, out PropertyInfo? propertyInfo) is false || propertyInfo is null)
        {
            return false;
        }
        
        object? extractedValue = propertyInfo.GetValue(this);

        if (extractedValue is null)
        {
            return true;
        }
        
        if (extractedValue is not T extractedValueAsT)
        {
            return false;
        }

        value = extractedValueAsT;
        return true;
    }
    
    private bool TrySetValue<T>(Expression<Func<TEntity, T>> property, T? value, UserAction updatedAction)
    {
        if (TryGetPropertyInfo(property, out PropertyInfo? propertyInfo) is false || propertyInfo is null)
        {
            return false;
        }

        if (propertyInfo.CanWrite is false)
        {
            return false;
        }

        propertyInfo.SetValue(this, value);
        
        MarkAsUpdated(updatedAction);

        return true;
    }
}