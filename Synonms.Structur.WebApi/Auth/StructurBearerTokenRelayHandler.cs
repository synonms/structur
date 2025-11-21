using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Synonms.Structur.WebApi.Auth;

public class StructurBearerTokenRelayHandler : DelegatingHandler
{
    private readonly ILogger<StructurBearerTokenRelayHandler> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public StructurBearerTokenRelayHandler(ILogger<StructurBearerTokenRelayHandler> logger, IHttpContextAccessor httpContextAccessor)
    {
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }
    
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Relaying access token...");
        
        HttpContext? httpContext = _httpContextAccessor.HttpContext;

        if (httpContext is null)
        {
            _logger.LogDebug("HTTP Context not found - skipping token relay.");
        }
        else
        {
            string? accessToken = await httpContext.GetTokenAsync(JwtBearerDefaults.AuthenticationScheme, "access_token");

            if (string.IsNullOrWhiteSpace(accessToken))
            {
                _logger.LogDebug("Access Token not available - skipping token relay.");
            }
            else
            {
                _logger.LogDebug("Access Token found - adding to request.");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            }
        }

        _logger.LogDebug("Access token relay complete.");
        
        return await base.SendAsync(request, cancellationToken);
    }
}