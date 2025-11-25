using System.Reflection;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Synonms.Structur.Application.Lookups;
using Synonms.Structur.Application.Mapping;
using Synonms.Structur.Application.Products;
using Synonms.Structur.Application.Products.Context;
using Synonms.Structur.Application.Products.Persistence;
using Synonms.Structur.Application.Products.Resolution;
using Synonms.Structur.Application.Routing;
using Synonms.Structur.Application.Schema.Errors;
using Synonms.Structur.Application.Schema.Forms;
using Synonms.Structur.Application.Services;
using Synonms.Structur.Application.Tenants;
using Synonms.Structur.Application.Tenants.Context;
using Synonms.Structur.Application.Tenants.Persistence;
using Synonms.Structur.Application.Tenants.Resolution;
using Synonms.Structur.Application.Users;
using Synonms.Structur.Application.Users.Context;
using Synonms.Structur.Application.Users.Persistence;
using Synonms.Structur.Application.Users.Resolution;
using Synonms.Structur.Core.Attributes;
using Synonms.Structur.Core.Cqrs;
using Synonms.Structur.Core.DependencyInjection;
using Synonms.Structur.Domain.Entities;
using Synonms.Structur.Domain.Events;
using Synonms.Structur.Domain.Lookups;
using Synonms.Structur.Domain.Services;
using Synonms.Structur.WebApi.Auth;
using Synonms.Structur.WebApi.Controllers;
using Synonms.Structur.WebApi.Correlation;
using Synonms.Structur.WebApi.Domain;
using Synonms.Structur.WebApi.Http;
using Synonms.Structur.WebApi.Mediation.Commands;
using Synonms.Structur.WebApi.Mediation.Queries;
using Synonms.Structur.WebApi.OpenApi;
using Synonms.Structur.WebApi.Products;
using Synonms.Structur.WebApi.Products.Resolution;
using Synonms.Structur.WebApi.Routing;
using Synonms.Structur.WebApi.Serialisation;
using Synonms.Structur.WebApi.Tenants;
using Synonms.Structur.WebApi.Tenants.Resolution;
using Synonms.Structur.WebApi.Users;
using Synonms.Structur.WebApi.Users.Resolution;
using CorsConstants = Synonms.Structur.WebApi.Cors.CorsConstants;

namespace Synonms.Structur.WebApi.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddStructur(this IServiceCollection serviceCollection, StructurOptions options) =>
        serviceCollection.AddStructur<NoStructurUser, NoStructurProduct, NoStructurTenant>(options);
    
    public static IServiceCollection AddStructur<TUser, TProduct, TTenant>(this IServiceCollection serviceCollection, StructurOptions options)
        where TUser : StructurUser
        where TProduct : StructurProduct
        where TTenant : StructurTenant
    {
        serviceCollection.AddHttpContextAccessor();

        IResourceDirectory resourceDirectory = new ResourceDirectory(options.Assemblies);
        serviceCollection.AddSingleton(resourceDirectory);

        IRouteNameProvider routeNameProvider = new RouteNameProvider();
        serviceCollection.AddSingleton(routeNameProvider);
        
        serviceCollection.RegisterCqrs(options.Assemblies);
        serviceCollection.RegisterAllImplementationsOf(typeof(IResourceMapper<,>), serviceCollection.AddSingleton, options.Assemblies);

        serviceCollection.AddSingleton<IErrorCollectionDocumentFactory, ErrorCollectionDocumentFactory>();
        
        serviceCollection.AddScoped<OptionsMiddleware>();
        serviceCollection.AddScoped<CorrelationMiddleware>();
        serviceCollection.AddScoped<StructurBearerTokenRelayHandler>();
        serviceCollection.AddScoped<StructurCorrelationRelayHandler>();

        serviceCollection.AddScoped<IRouteGenerator, HttpRouteGenerator>();
        serviceCollection.AddScoped(typeof(ICreateFormDocumentFactory<,>), typeof(CreateFormDocumentFactory<,>));
        serviceCollection.AddScoped(typeof(IEditFormDocumentFactory<,>), typeof(EditFormDocumentFactory<,>));
        
        foreach ((string _, IResourceDirectory.AggregateRootLayout aggregateRootLayout) in resourceDirectory.GetAllRoots())
        { 
            serviceCollection
                .RegisterRequestHandlers(aggregateRootLayout)
                .RegisterResourceMappers(aggregateRootLayout);
        }

        foreach (IResourceDirectory.AggregateMemberLayout aggregateMemberLayout in resourceDirectory.GetAllMembers())
        { 
            serviceCollection
                .RegisterChildResourceMappers(aggregateMemberLayout);
        }
        
        // Replace default mappers where an explicit one is provided
        serviceCollection.RegisterAllImplementationsOf(typeof(IResourceMapper<,>), serviceCollection.AddSingleton, options.Assemblies);

        serviceCollection.AddScoped<IChildResourceMapperFactory, ChildResourceMapperFactory>();
        serviceCollection.AddScoped<IResourceMapperFactory, ResourceMapperFactory>();

        serviceCollection.RegisterApplicationDependenciesFrom(options.Assemblies);
        serviceCollection.RegisterDomainDependenciesFrom(options.Assemblies);

        if (options.CorsConfiguration is not null)
        {
            serviceCollection.WithCorsPolicy(options.CorsConfiguration);
        }

        if (options.UseEmptyLookups)
        {
            serviceCollection.AddScoped(typeof(ILookupRepository<>), typeof(EmptyLookupRepository<>));
            serviceCollection.AddScoped(typeof(ILookupOptionsProvider), typeof(EmptyLookupOptionsProvider));
        }

        serviceCollection.WithUsers<TUser>();
        serviceCollection.WithTenants<TUser, TTenant>();
        serviceCollection.WithProducts<TUser, TProduct>();
        serviceCollection.AddScoped<PermissionsMiddleware<TUser, TProduct, TTenant>>();
        
        serviceCollection.WithOpenApi(options.OpenApiConfigurationAction);
        
        AuthenticationBuilder authenticationBuilder = serviceCollection.AddAuthentication(options.DefaultAuthenticationScheme);
        options.AuthenticationConfigurationAction?.Invoke(authenticationBuilder);
        
        serviceCollection.AddAuthorization(authorizationOptions =>
        {
            authorizationOptions.AddStructurAuthorisationPolicies(options.Assemblies);
            options.AuthorizationConfiguration?.Invoke(authorizationOptions);
        });

        serviceCollection.WithControllers(options, routeNameProvider, resourceDirectory);
        
        return serviceCollection;
    }
    
    private static IServiceCollection RegisterRequestHandlers(this IServiceCollection serviceCollection, IResourceDirectory.AggregateRootLayout aggregateRootLayout)
    {
        StructurResourceAttribute? resourceAttribute = aggregateRootLayout.AggregateRootType.GetCustomAttribute<StructurResourceAttribute>();

        if (resourceAttribute is null)
        {
            return serviceCollection;
        }
        
        Type findResourceRequestType = typeof(FindResourceQuery<,>).MakeGenericType(aggregateRootLayout.AggregateRootType, aggregateRootLayout.ResourceType);
        Type findResourceResponseType = typeof(FindResourceQueryResponse<,>).MakeGenericType(aggregateRootLayout.AggregateRootType, aggregateRootLayout.ResourceType);
        Type findResourceRequestHandlerInterfaceType = typeof(IQueryHandler<,>).MakeGenericType(findResourceRequestType, findResourceResponseType);
        Type findResourceRequestHandlerImplementationType = typeof(FindResourceQueryProcessor<,>).MakeGenericType(aggregateRootLayout.AggregateRootType, aggregateRootLayout.ResourceType);

        serviceCollection.AddTransient(findResourceRequestHandlerInterfaceType, findResourceRequestHandlerImplementationType);
        
        Type readResourceCollectionRequestType = typeof(ReadResourceCollectionQuery<,>).MakeGenericType(aggregateRootLayout.AggregateRootType, aggregateRootLayout.ResourceType);
        Type readResourceCollectionResponseType = typeof(ReadResourceCollectionQueryResponse<,>).MakeGenericType(aggregateRootLayout.AggregateRootType, aggregateRootLayout.ResourceType);
        Type readResourceCollectionRequestHandlerInterfaceType = typeof(IQueryHandler<,>).MakeGenericType(readResourceCollectionRequestType, readResourceCollectionResponseType);
        Type readResourceCollectionRequestHandlerImplementationType = typeof(ReadResourceCollectionQueryProcessor<,>).MakeGenericType(aggregateRootLayout.AggregateRootType, aggregateRootLayout.ResourceType);

        serviceCollection.AddTransient(readResourceCollectionRequestHandlerInterfaceType, readResourceCollectionRequestHandlerImplementationType);

        if (resourceAttribute.IsCreateDisabled is false)
        {
            Type createResourceRequestType = typeof(CreateResourceCommand<,>).MakeGenericType(aggregateRootLayout.AggregateRootType, aggregateRootLayout.ResourceType);
            Type createResourceResponseType = typeof(CreateResourceCommandResponse<>).MakeGenericType(aggregateRootLayout.AggregateRootType);
            Type createResourceRequestHandlerInterfaceType = typeof(ICommandHandler<,>).MakeGenericType(createResourceRequestType, createResourceResponseType);
            Type createResourceRequestHandlerImplementationType = typeof(CreateResourceCommandProcessor<,>).MakeGenericType(aggregateRootLayout.AggregateRootType, aggregateRootLayout.ResourceType);

            serviceCollection.AddTransient(createResourceRequestHandlerInterfaceType, createResourceRequestHandlerImplementationType);
        }

        if (resourceAttribute.IsUpdateDisabled is false)
        {
            Type updateResourceRequestType = typeof(UpdateResourceCommand<,>).MakeGenericType(aggregateRootLayout.AggregateRootType, aggregateRootLayout.ResourceType);
            Type updateResourceResponseType = typeof(UpdateResourceCommandResponse<>).MakeGenericType(aggregateRootLayout.AggregateRootType);
            Type updateResourceRequestHandlerInterfaceType = typeof(ICommandHandler<,>).MakeGenericType(updateResourceRequestType, updateResourceResponseType);
            Type updateResourceRequestHandlerImplementationType = typeof(UpdateResourceCommandProcessor<,>).MakeGenericType(aggregateRootLayout.AggregateRootType, aggregateRootLayout.ResourceType);

            serviceCollection.AddTransient(updateResourceRequestHandlerInterfaceType, updateResourceRequestHandlerImplementationType);
        }

        if (resourceAttribute.IsDeleteDisabled is false)
        {
            Type deleteResourceRequestType = typeof(DeleteResourceCommand<>).MakeGenericType(aggregateRootLayout.AggregateRootType);
            Type deleteResourceResponseType = typeof(DeleteResourceCommandResponse<>).MakeGenericType(aggregateRootLayout.AggregateRootType);
            Type deleteResourceRequestHandlerInterfaceType = typeof(ICommandHandler<,>).MakeGenericType(deleteResourceRequestType, deleteResourceResponseType);
            Type deleteResourceRequestHandlerImplementationType = typeof(DeleteResourceCommandProcessor<,>).MakeGenericType(aggregateRootLayout.AggregateRootType, aggregateRootLayout.ResourceType);

            serviceCollection.AddTransient(deleteResourceRequestHandlerInterfaceType, deleteResourceRequestHandlerImplementationType);
        }

        return serviceCollection;
    }

    private static IServiceCollection RegisterResourceMappers(this IServiceCollection serviceCollection, IResourceDirectory.AggregateRootLayout aggregateRootLayout)
    {
        Type resourceMapperInterfaceType = typeof(IResourceMapper<,>).MakeGenericType(aggregateRootLayout.AggregateRootType, aggregateRootLayout.ResourceType);
        Type resourceMapperImplementationType = typeof(DefaultResourceMapper<,>).MakeGenericType(aggregateRootLayout.AggregateRootType, aggregateRootLayout.ResourceType);

        serviceCollection.AddScoped(resourceMapperInterfaceType, resourceMapperImplementationType);
        serviceCollection.AddScoped(typeof(IResourceMapper), resourceMapperImplementationType);
        
        return serviceCollection;
    }
    
    private static IServiceCollection RegisterChildResourceMappers(this IServiceCollection serviceCollection, IResourceDirectory.AggregateMemberLayout aggregateMemberLayout)
    {
        Type childResourceMapperInterfaceType = typeof(IChildResourceMapper<,>).MakeGenericType(aggregateMemberLayout.AggregateMemberType, aggregateMemberLayout.ChildResourceType);
        Type childResourceMapperImplementationType = typeof(DefaultChildResourceMapper<,>).MakeGenericType(aggregateMemberLayout.AggregateMemberType, aggregateMemberLayout.ChildResourceType);

        serviceCollection.AddScoped(childResourceMapperInterfaceType, childResourceMapperImplementationType);
        serviceCollection.AddScoped(typeof(IChildResourceMapper), childResourceMapperImplementationType);

        return serviceCollection;
    }

    private static IServiceCollection RegisterApplicationDependenciesFrom(this IServiceCollection serviceCollection, params Assembly[] assemblies)
    {
        serviceCollection.RegisterAllImplementationsOf(typeof(IApplicationService), serviceCollection.AddScoped, assemblies);
        
        return serviceCollection;
    }

    private static IServiceCollection RegisterDomainDependenciesFrom(this IServiceCollection serviceCollection, params Assembly[] assemblies)
    {
        serviceCollection.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

        serviceCollection.RegisterAllImplementationsOf(typeof(IDomainEventFactory<,>), serviceCollection.AddSingleton, assemblies);
        
        serviceCollection.RegisterAllImplementationsOf(typeof(IDomainService), serviceCollection.AddScoped, assemblies);
        serviceCollection.RegisterAllImplementationsOf(typeof(IReadAggregateRepository<>), serviceCollection.AddScoped, assemblies);

        return serviceCollection;
    }

    private static IServiceCollection WithCorsPolicy(this IServiceCollection serviceCollection, Action<CorsPolicyBuilder> configurePolicy)
    {
        serviceCollection.AddCors(corsOptions =>
        {
            corsOptions.AddPolicy(CorsConstants.PolicyName, configurePolicy);
        });

        return serviceCollection;
    }

    private static IServiceCollection WithUsers<TUser>(this IServiceCollection serviceCollection)
        where TUser : StructurUser
    {
        serviceCollection.AddScoped<UserMiddleware<TUser>>();
        serviceCollection.AddScoped<IUserContextAccessor<TUser>, UserContextAccessor<TUser>>();
        serviceCollection.AddScoped<IUserContextFactory<TUser>, UserContextFactory<TUser>>();
        serviceCollection.AddScoped<IUserIdResolver, UserIdResolver>();

        serviceCollection.AddScoped<IUserIdResolutionStrategy, ClaimsPrincipalUserIdResolutionStrategy>();

        if (typeof(TUser) == typeof(NoStructurUser))
        {
            serviceCollection.AddScoped<IUserRepository<NoStructurUser>, NoStructurUserRepository>();
        }
        
        return serviceCollection;
    }

    private static IServiceCollection WithTenants<TUser, TTenant>(this IServiceCollection serviceCollection)
        where TUser : StructurUser
        where TTenant : StructurTenant
    {
        serviceCollection.AddScoped<TenantMiddleware<TUser, TTenant>>();
        serviceCollection.AddScoped<ITenantContextAccessor<TTenant>, TenantContextAccessor<TTenant>>();
        serviceCollection.AddScoped<ITenantContextFactory<TTenant>, TenantContextFactory<TTenant>>();
        serviceCollection.AddScoped<ITenantIdResolver, TenantIdResolver>();

        serviceCollection.AddScoped<ITenantIdResolutionStrategy, HeaderTenantIdResolutionStrategy>();
        serviceCollection.AddScoped<ITenantIdResolutionStrategy, QueryStringTenantIdResolutionStrategy>();

        if (typeof(TTenant) == typeof(NoStructurTenant))
        {
            serviceCollection.AddScoped<ITenantRepository<NoStructurTenant>, NoStructurTenantRepository>();
        }

        return serviceCollection;
    }

    private static IServiceCollection WithProducts<TUser, TProduct>(this IServiceCollection serviceCollection)
        where TUser : StructurUser
        where TProduct : StructurProduct
    {
        serviceCollection.AddScoped<ProductMiddleware<TUser, TProduct>>();
        serviceCollection.AddScoped<IProductContextAccessor<TProduct>, ProductContextAccessor<TProduct>>();
        serviceCollection.AddScoped<IProductContextFactory<TProduct>, ProductContextFactory<TProduct>>();
        serviceCollection.AddScoped<IProductIdResolver, ProductIdResolver>();

        serviceCollection.AddScoped<IProductIdResolutionStrategy, HeaderProductIdResolutionStrategy>();
        serviceCollection.AddScoped<IProductIdResolutionStrategy, QueryStringProductIdResolutionStrategy>();

        if (typeof(TProduct) == typeof(NoStructurProduct))
        {
            serviceCollection.AddScoped<IProductRepository<NoStructurProduct>, NoStructurProductRepository>();
        }

        return serviceCollection;
    }
    
    private static IServiceCollection WithControllers(this IServiceCollection serviceCollection, StructurOptions structurOptions, IRouteNameProvider routeNameProvider, IResourceDirectory resourceDirectory)
    {
        IMvcBuilder mvcBuilder = serviceCollection.AddControllers(mvcOptions =>
            {
                mvcOptions.Conventions.Add(new ControllerModelConvention(routeNameProvider));

                mvcOptions.ConfigureForStructur();
                
                structurOptions.MvcOptionsConfigurationAction?.Invoke(mvcOptions);
            })
            .ConfigureApplicationPartManager(applicationPartManager =>
            {
                applicationPartManager.FeatureProviders.Add(new ControllerFeatureProvider(resourceDirectory));
                
                structurOptions.ApplicationPartManagerConfigurationAction?.Invoke(applicationPartManager);
            })
            .AddJsonOptions(jsonOptions =>
            {
                jsonOptions.JsonSerializerOptions.ConfigureForStructurFramework();
                
                structurOptions.JsonOptionsConfigurationAction?.Invoke(jsonOptions);
            });

        structurOptions.MvcBuilderConfigurationAction?.Invoke(mvcBuilder);
        
        return serviceCollection;
    }
    
    private static IServiceCollection WithOpenApi(this IServiceCollection serviceCollection, Action<OpenApiOptions>? configurationAction = null)
    {
//        serviceCollection.AddEndpointsApiExplorer();
        serviceCollection.AddOpenApi(options =>
        {
            options.AddDocumentTransformer<StructurDocumentTransformer>();
            configurationAction?.Invoke(options);
        });

        return serviceCollection;
    }
}