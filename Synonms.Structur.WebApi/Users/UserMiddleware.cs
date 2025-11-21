using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Synonms.Structur.Application.Users;
using Synonms.Structur.Application.Users.Context;
using Synonms.Structur.Application.Users.Resolution;
using Synonms.Structur.Core.Functional;

namespace Synonms.Structur.WebApi.Users;

public class UserMiddleware<TUser> : IMiddleware
    where TUser : StructurUser
{
    private readonly ILogger<UserMiddleware<TUser>> _logger;
    private readonly IUserContextFactory<TUser> _userContextFactory;
    private readonly IUserContextAccessor<TUser> _userContextAccessor;
    private readonly IUserIdResolver _userIdResolver;

    public UserMiddleware(
        ILogger<UserMiddleware<TUser>> logger, 
        IUserContextFactory<TUser> userContextFactory, 
        IUserContextAccessor<TUser> userContextAccessor, 
        IUserIdResolver userIdResolver)
    {
        _logger = logger;
        _userContextFactory = userContextFactory;
        _userContextAccessor = userContextAccessor;
        _userIdResolver = userIdResolver;
    }
    
    public async Task InvokeAsync(HttpContext httpContext, RequestDelegate next)
    {
        _logger.LogDebug("Executing User Middleware...");
        
        if (httpContext.User.Identity is not { IsAuthenticated: true })
        {
            _logger.LogDebug("Authenticated user not found - User middleware complete.");
            await next(httpContext);
            return;
        }

        if (_userContextAccessor.UserContext is not null)
        {
            _logger.LogDebug("User Context already present - User middleware complete.");
            await next(httpContext);
            return;
        }
        
        UserContext<TUser> userContext = await _userIdResolver
            .ResolveAsync()
            .MatchAsync(
                async userId =>
                {
                    _logger.LogDebug("Successfully resolved User Id {userId}.", userId);
                    return await _userContextFactory.CreateAsync(userId, CancellationToken.None);
                },
                async () => 
                {
                    _logger.LogDebug("Failed to resolve User Id.");
                    return await _userContextFactory.CreateAsync(null, CancellationToken.None);
                });
        
        _userContextAccessor.UserContext = userContext;
        
        _logger.LogDebug("User middleware complete.");
        await next(httpContext);
    }
}