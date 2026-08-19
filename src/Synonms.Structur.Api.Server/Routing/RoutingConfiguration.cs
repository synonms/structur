using Microsoft.AspNetCore.Routing;

namespace Synonms.Structur.Api.Server.Routing;

public static class RoutingConfiguration
{
    public static readonly LinkOptions DefaultLinkOptions = new()
    {
        LowercaseUrls = true
    };
}