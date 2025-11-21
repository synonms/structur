using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Synonms.Structur.WebApi.Http;

namespace Synonms.Structur.WebApi.Correlation;

public class CorrelationMiddleware : IMiddleware
{
    private readonly ILogger<CorrelationMiddleware> _logger;

    public CorrelationMiddleware(ILogger<CorrelationMiddleware> logger)
    {
        _logger = logger;
    }
    
    public async Task InvokeAsync(HttpContext httpContext, RequestDelegate next)
    {
        _logger.LogDebug("Executing Correlation Middleware...");

        if (httpContext.Items.ContainsKey(HttpContextItemKeys.RequestId))
        {
            _logger.LogDebug("Request Id already present - skipping.");
        }
        else
        {
            Guid requestId = Guid.NewGuid();
            
            httpContext.Items[HttpContextItemKeys.RequestId] = requestId;

            httpContext.Response.Headers[HttpHeaders.RequestId] = new StringValues(requestId.ToString());
            
            _logger.LogDebug("Request Id {requestId} set.", requestId);
        }

        if (httpContext.Items.ContainsKey(HttpContextItemKeys.CorrelationId))
        {
            _logger.LogDebug("Correlation Id already present - skipping.");
        }
        else
        {
            Guid correlationId = Guid.NewGuid();
            
            if (httpContext.Request.Headers.TryGetValue(HttpHeaders.CorrelationId, out StringValues correlationHeader))
            {
                if (Guid.TryParse(correlationHeader, out Guid incomingCorrelationId))
                {
                    _logger.LogDebug("Incoming Correlation Id detected.");

                    correlationId = incomingCorrelationId;
                }
            }

            httpContext.Items[HttpContextItemKeys.CorrelationId] = correlationId;

            httpContext.Response.Headers[HttpHeaders.CorrelationId] = new StringValues(correlationId.ToString());
            
            _logger.LogDebug("Correlation Id {correlationId} set.", correlationId);
        }

        _logger.LogDebug("Correlation Middleware complete.");

        await next(httpContext);    
    }
}