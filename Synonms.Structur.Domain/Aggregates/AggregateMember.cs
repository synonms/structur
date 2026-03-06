using System.Linq.Expressions;
using System.Reflection;
using Synonms.Structur.Core.Entities;

namespace Synonms.Structur.Domain.Aggregates;

public abstract class AggregateMember<TAggregateMember> : Entity<TAggregateMember>
    where TAggregateMember : AggregateMember<TAggregateMember>
{
    protected AggregateMember() : this(EntityId<TAggregateMember>.Uninitialised)
    {
    }
    
    protected AggregateMember(EntityId<TAggregateMember> id) : base(id)
    {
    }
    
    
    protected void UpdateMandatoryValue<T>(Expression<Func<TAggregateMember, T>> property, T newValue, Action rootUpdatedAction) where T : notnull
    {
        if (TryGetMandatoryValue(property, out T originalValue) is false)
        {
            return;
        }
        
        if (newValue.Equals(originalValue))
        {
            return;
        }

        TrySetValue(property, newValue, rootUpdatedAction);
    }
    
    protected void UpdateOptionalValue<T>(Expression<Func<TAggregateMember, T?>> property, T? newValue, Action rootUpdatedAction)
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

        TrySetValue(property, newValue, rootUpdatedAction);
    }
    
    private static bool TryGetPropertyInfo<T>(Expression<Func<TAggregateMember, T>> property, out PropertyInfo? propertyInfo)
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
    
    private bool TryGetMandatoryValue<T>(Expression<Func<TAggregateMember, T>> property, out T value)
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
    
    private bool TryGetOptionalValue<T>(Expression<Func<TAggregateMember, T?>> property, out T? value)
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
    
    private bool TrySetValue<T>(Expression<Func<TAggregateMember, T>> property, T? value, Action rootUpdatedAction)
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
        
        rootUpdatedAction.Invoke();

        return true;
    }
}