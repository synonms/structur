using System.Linq.Expressions;
using System.Reflection;
using Synonms.Structur.Core.Entities;
using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.ValueObjects;

namespace Synonms.Structur.Domain.Aggregates;

public abstract class AggregateRoot<TAggregateRoot> : Entity<TAggregateRoot>
    where TAggregateRoot : AggregateRoot<TAggregateRoot>
{
    protected AggregateRoot() : this(EntityId<TAggregateRoot>.Uninitialised, Guid.Empty, UserAction.Empty)
    {
    }
    
    protected AggregateRoot(EntityId<TAggregateRoot> id, UserAction createdAction) : this(id, Guid.Empty, createdAction)
    {
    }

    protected AggregateRoot(EntityId<TAggregateRoot> id, Guid tenantId, UserAction createdAction) : base(id)
    {
        TenantId = tenantId;
        CreatedAction = createdAction;
    }

    public Guid TenantId { get; private set; }
    
    public EntityTag EntityTag { get; private set; } = EntityTag.New();
    
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
    
    protected void MarkAsUpdated(UserAction updatedAction)
    {
        UpdatedAction = updatedAction;
        
        EntityTag = EntityTag.New();
    }
    
    protected void UpdateMandatoryValue<T>(Expression<Func<TAggregateRoot, T>> property, T newValue, UserAction updatedAction) where T : notnull
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
    
    protected void UpdateOptionalValue<T>(Expression<Func<TAggregateRoot, T?>> property, T? newValue, UserAction updatedAction)
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
    
    private static bool TryGetPropertyInfo<T>(Expression<Func<TAggregateRoot, T>> property, out PropertyInfo? propertyInfo)
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
    
    private bool TryGetMandatoryValue<T>(Expression<Func<TAggregateRoot, T>> property, out T value)
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
    
    private bool TryGetOptionalValue<T>(Expression<Func<TAggregateRoot, T?>> property, out T? value)
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
    
    private bool TrySetValue<T>(Expression<Func<TAggregateRoot, T>> property, T? value, UserAction updatedAction)
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
