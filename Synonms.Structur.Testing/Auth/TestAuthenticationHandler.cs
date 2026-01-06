using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Synonms.Structur.Testing.Auth;

public class TestAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string AuthenticationScheme = "TestAuthenticationScheme";
    public const string AuthenticationType = "TestAuthenticationType";
    public const string PermissionsClaimType = "permissions";
    public const string SubjectClaimType = "sub";
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TestAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, IHttpContextAccessor httpContextAccessor) 
        : base(options, logger, encoder)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        string? userId = _httpContextAccessor.HttpContext?.Request.Headers
            .SingleOrDefault(x => x.Key == TestHttpHeaders.UserId).Value.ToString();

        if (string.IsNullOrWhiteSpace(userId))
        {
            AuthenticateResult noUserResult = AuthenticateResult.Fail($"No user defined in request - populate '{TestHttpHeaders.UserId}' header with user Id if required.");
            return Task.FromResult(noUserResult);
        }
        
        List<string> permissions = _httpContextAccessor.HttpContext?.Request.Headers
            .Where(x => x.Key == TestHttpHeaders.Permissions)
            .Select(x => x.Value.ToString())
            .ToList() ?? new List<string>();

        List<Claim> claims = permissions.Select(x => new Claim(PermissionsClaimType, x)).ToList();
        claims.Add(new Claim(SubjectClaimType, userId));
        
        ClaimsIdentity identity = new (claims, AuthenticationType);
        ClaimsPrincipal principal = new (identity);
        AuthenticationTicket ticket = new (principal, AuthenticationScheme);

        AuthenticateResult successResult = AuthenticateResult.Success(ticket);

        return Task.FromResult(successResult);
    }
}