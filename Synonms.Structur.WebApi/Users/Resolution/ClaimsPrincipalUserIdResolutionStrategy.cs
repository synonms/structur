using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Identity.Web;
using Synonms.Structur.Application.Users.Resolution;
using Synonms.Structur.Core.Functional;

namespace Synonms.Structur.WebApi.Users.Resolution;

public class ClaimsPrincipalUserIdResolutionStrategy : IUserIdResolutionStrategy
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ClaimsPrincipalUserIdResolutionStrategy(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Maybe<Guid> Resolve()
    {
        if (_httpContextAccessor.HttpContext is null)
        {
            return Maybe<Guid>.None;
        }

        ClaimsPrincipal claimsPrincipal = _httpContextAccessor.HttpContext.User;
        
        string? subject = claimsPrincipal.FindFirst(ClaimConstants.Sub)?.Value;
        
        if (string.IsNullOrWhiteSpace(subject) || Guid.TryParse(subject, out Guid userId) is false)
        {
            string? nameIdentifier = claimsPrincipal.FindFirst(ClaimConstants.NameIdentifierId)?.Value;

            if (string.IsNullOrWhiteSpace(nameIdentifier) || Guid.TryParse(nameIdentifier, out userId) is false)
            {
                return Maybe<Guid>.None;
            }
        }

        return userId;
    }
}