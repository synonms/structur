using Microsoft.AspNetCore.Authorization;

namespace Synonms.Structur.Api.Server.Auth;

public interface IPolicyRegistrar
{
    void Register(AuthorizationOptions authorisationOptions);
}