using Microsoft.AspNetCore.Authorization;
using Synonms.Structur.Domain.Entities;

namespace Synonms.Structur.WebApi.Auth;

public abstract class AggregatePolicyRegistrar<TAggregateRoot> : IPolicyRegistrar
    where TAggregateRoot : AggregateRoot<TAggregateRoot>
{
    protected const string Separator = ".";
    protected readonly string ResourceName;
    protected readonly string CreatePermission;
    protected readonly string ReadPermission;
    protected readonly string UpdatePermission;
    protected readonly string DeletePermission;
    protected readonly string AuthPolicyName;
    protected readonly string CreatePolicy;
    protected readonly string ReadPolicy;
    protected readonly string UpdatePolicy;
    protected readonly string DeletePolicy;

    protected AggregatePolicyRegistrar()
    {
        ResourceName = typeof(TAggregateRoot).Name.ToLowerInvariant();

        CreatePermission = ResourceName + Separator + Permissions.Create;
        ReadPermission = ResourceName + Separator + Permissions.Read;
        UpdatePermission = ResourceName + Separator + Permissions.Update;
        DeletePermission = ResourceName + Separator + Permissions.Delete;

        AuthPolicyName = typeof(TAggregateRoot).Name;

        CreatePolicy = Policies.CreatePrefix + AuthPolicyName;
        ReadPolicy = Policies.ReadPrefix + AuthPolicyName;
        UpdatePolicy = Policies.UpdatePrefix + AuthPolicyName;
        DeletePolicy = Policies.DeletePrefix + AuthPolicyName;
    }

    public virtual void Register(AuthorizationOptions authorisationOptions)
    {
        RequireDefaultPermissionsClaims(authorisationOptions);
    }

    protected void RequireDefaultPermissionsClaims(AuthorizationOptions authorisationOptions)
    {
        if (string.IsNullOrWhiteSpace(AuthPolicyName))
        {
            return;
        }
        
        authorisationOptions.AddPolicy(CreatePolicy, policy => policy.RequireClaim(Permissions.ClaimType, CreatePermission));
        authorisationOptions.AddPolicy(ReadPolicy, policy => policy.RequireClaim(Permissions.ClaimType, ReadPermission));
        authorisationOptions.AddPolicy(UpdatePolicy, policy => policy.RequireClaim(Permissions.ClaimType, UpdatePermission));
        authorisationOptions.AddPolicy(DeletePolicy, policy => policy.RequireClaim(Permissions.ClaimType, DeletePermission));
    }
    
    protected void AllowAll(AuthorizationOptions authorisationOptions)
    {
        if (string.IsNullOrWhiteSpace(AuthPolicyName))
        {
            return;
        }
        
        authorisationOptions.AddPolicy(CreatePolicy, _ => {});
        authorisationOptions.AddPolicy(ReadPolicy, _ => {});
        authorisationOptions.AddPolicy(UpdatePolicy, _ => {});
        authorisationOptions.AddPolicy(DeletePolicy, _ => {});
    }
}