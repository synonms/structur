using Microsoft.AspNetCore.Authorization;

namespace Synonms.Structur.WebApi.Auth;

public interface IPolicyRegistrar
{
    void Register(AuthorizationOptions authorisationOptions);
}