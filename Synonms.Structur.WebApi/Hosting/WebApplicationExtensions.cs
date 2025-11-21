using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Synonms.Structur.Application.Products;
using Synonms.Structur.Application.Tenants;
using Synonms.Structur.Application.Users;
using Synonms.Structur.WebApi.Auth;
using Synonms.Structur.WebApi.Correlation;
using Synonms.Structur.WebApi.Cors;
using Synonms.Structur.WebApi.DependencyInjection;
using Synonms.Structur.WebApi.Http;
using Synonms.Structur.WebApi.Products;
using Synonms.Structur.WebApi.Tenants;
using Synonms.Structur.WebApi.Users;

namespace Synonms.Structur.WebApi.Hosting;

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