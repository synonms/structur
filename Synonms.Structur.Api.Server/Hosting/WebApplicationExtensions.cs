using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Synonms.Structur.Api.Server.Auth;
using Synonms.Structur.Api.Server.Correlation;
using Synonms.Structur.Api.Server.Cors;
using Synonms.Structur.Api.Server.DependencyInjection;
using Synonms.Structur.Api.Server.Http;
using Synonms.Structur.Api.Server.Products;
using Synonms.Structur.Api.Server.Tenants;
using Synonms.Structur.Api.Server.Users;
using Synonms.Structur.Api.Server.Versioning;

namespace Synonms.Structur.Api.Server.Hosting;

public static class WebApplicationExtensions
{
    public static WebApplication UseStructur(this WebApplication webApplication, StructurOptions options) =>
        webApplication.UseStructur<NoStructurUser, NoStructurProduct, NoStructurTenant>(options);

    public static WebApplication UseStructur<TUser, TProduct, TTenant>(this WebApplication webApplication, StructurOptions options)
        where TUser : StructurUser
        where TProduct : StructurProduct
        where TTenant : StructurTenant
    {
        webApplication.UseHttpsRedirection();

        webApplication.UseMiddleware<OptionsMiddleware>();

        if (webApplication.Environment.IsDevelopment())
        {
            if (string.IsNullOrWhiteSpace(options.OpenApiDocumentPath))
            {
                webApplication.MapOpenApi();
            }
            else
            {
                webApplication.MapOpenApi(options.OpenApiDocumentPath);
            }
            webApplication.UseSwaggerUI(options.SwaggerUiConfigurationAction);
        }

        webApplication.UseMiddleware<CorrelationMiddleware>();
        
        options.PreRoutingPipelineConfigurationAction?.Invoke(webApplication);
        webApplication.UseRouting();
        options.PostRoutingPipelineConfigurationAction?.Invoke(webApplication);
        
        webApplication.UseCors(CorsConstants.PolicyName);

        options.PreAuthenticationPipelineConfigurationAction?.Invoke(webApplication);
        webApplication.UseAuthentication();
        options.PostAuthenticationPipelineConfigurationAction?.Invoke(webApplication);
        
        webApplication.UseMiddleware<VersionMiddleware>();
        webApplication.UseMiddleware<UserMiddleware<TUser>>();
        webApplication.UseMiddleware<TenantMiddleware<TUser, TTenant>>();
        webApplication.UseMiddleware<ProductMiddleware<TUser, TProduct>>();
        webApplication.UseMiddleware<PermissionsMiddleware<TUser, TProduct, TTenant>>();

        options.PreAuthorizationPipelineConfigurationAction?.Invoke(webApplication);
        webApplication.UseAuthorization();
        options.PostAuthorizationPipelineConfigurationAction?.Invoke(webApplication);
        
        ControllerActionEndpointConventionBuilder controllerActionEndpointConventionBuilder = webApplication.MapControllers();

        options.ControllerActionConfigurationAction?.Invoke(controllerActionEndpointConventionBuilder);
        
        return webApplication;
    }
}