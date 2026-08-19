using System.Security.Claims;

namespace Synonms.Structur.Infrastructure.Auth;

public static class ClaimsPrincipalExtensions
{
    private const string NameIdentifierClaim = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier";
    private const string SubClaim = "sub";
    
    public static Guid? GetUserId(this ClaimsPrincipal claimsPrincipal)
    {
        string? subject = claimsPrincipal.FindFirst(SubClaim)?.Value;
        
        if (string.IsNullOrWhiteSpace(subject) || Guid.TryParse(subject, out Guid userId) is false)
        {
            string? nameIdentifier = claimsPrincipal.FindFirst(NameIdentifierClaim)?.Value;
            
            if (string.IsNullOrWhiteSpace(nameIdentifier) || Guid.TryParse(nameIdentifier, out userId) is false)
            {
                return null;
            }
        }

        return userId;
    }
}