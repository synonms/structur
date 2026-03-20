using System.Reflection;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Synonms.Structur.Api.Core.Serialisation;
using Synonms.Structur.Api.Server.Auth;
using Synonms.Structur.Api.Server.Controllers;
using Synonms.Structur.Api.Server.Correlation;
using Synonms.Structur.Api.Server.Events;
using Synonms.Structur.Api.Server.Http;
using Synonms.Structur.Api.Server.Lookups;
using Synonms.Structur.Api.Server.Mapping;
using Synonms.Structur.Api.Server.Mediation.Commands;
using Synonms.Structur.Api.Server.Mediation.Queries;
using Synonms.Structur.Api.Server.OpenApi;
using Synonms.Structur.Api.Server.Products;
using Synonms.Structur.Api.Server.Products.Context;
using Synonms.Structur.Api.Server.Products.Persistence;
using Synonms.Structur.Api.Server.Products.Resolution;
using Synonms.Structur.Api.Server.Routing;
using Synonms.Structur.Api.Server.Schema.Errors;
using Synonms.Structur.Api.Server.Schema.Forms;
using Synonms.Structur.Api.Server.Tenants;
using Synonms.Structur.Api.Server.Tenants.Context;
using Synonms.Structur.Api.Server.Tenants.Persistence;
using Synonms.Structur.Api.Server.Tenants.Resolution;
using Synonms.Structur.Api.Server.Users;
using Synonms.Structur.Api.Server.Users.Context;
using Synonms.Structur.Api.Server.Users.Persistence;
using Synonms.Structur.Api.Server.Users.Resolution;
using Synonms.Structur.Core.Attributes;
using Synonms.Structur.Core.Cqrs;
using Synonms.Structur.Core.DependencyInjection;
using Synonms.Structur.Domain.Aggregates;
using Synonms.Structur.Domain.Events;
using Synonms.Structur.Domain.Lookups;
using Synonms.Structur.Domain.Services;
using CorsConstants = Synonms.Structur.Api.Server.Cors.CorsConstants;

namespace Synonms.Structur.Api.Server.DependencyInjection;

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

        serviceCollection.AddScoped<IUserActionProvider, EmptyUserActionProvider>();

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

        Console.WriteLine($"Processing {aggregateRootLayout.ProjectionTypes.Count} projection types for {aggregateRootLayout.AggregateRootType.Name}");
        
        foreach (Type projectionType in aggregateRootLayout.ProjectionTypes)
        {
            Console.WriteLine($"Processing {projectionType.Name}...");
            
            StructurProjectionAttribute? projectionAttribute = projectionType.GetCustomAttribute<StructurProjectionAttribute>();

            if (projectionAttribute is null)
            {
                Console.WriteLine("No ProjectionAttribute found!");
                return serviceCollection;
            }

            Type getProjectionRequestType = typeof(GetProjectionQuery<,>).MakeGenericType(aggregateRootLayout.AggregateRootType, projectionType);
            Type getProjectionResponseType = typeof(GetProjectionQueryResponse<,>).MakeGenericType(aggregateRootLayout.AggregateRootType, projectionType);
            Type getProjectionRequestHandlerInterfaceType = typeof(IQueryHandler<,>).MakeGenericType(getProjectionRequestType, getProjectionResponseType);
            Type getProjectionRequestHandlerImplementationType = typeof(GetProjectionQueryProcessor<,>).MakeGenericType(aggregateRootLayout.AggregateRootType, projectionType);

            Console.WriteLine($"Registering {getProjectionRequestHandlerInterfaceType.Name} -> {getProjectionRequestHandlerImplementationType.Name}");

            serviceCollection.AddTransient(getProjectionRequestHandlerInterfaceType, getProjectionRequestHandlerImplementationType);
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

    private static IServiceCollection RegisterDomainDependenciesFrom(this IServiceCollection serviceCollection, params Assembly[] assemblies)
    {
        serviceCollection.RegisterAllImplementationsOf(typeof(IDomainEventHandler), serviceCollection.AddScoped, assemblies);
        serviceCollection.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

        serviceCollection.RegisterAllImplementationsOf(typeof(IDomainEventFactory<,>), serviceCollection.AddScoped, assemblies);
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
        serviceCollection.AddScoped<IUserContextAccessor>(sp => sp.GetRequiredService<IUserContextAccessor<TUser>>());
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
        serviceCollection.AddScoped<ITenantContext<TTenant>, TenantContext<TTenant>>();
        serviceCollection.AddScoped<ITenantContext>(sp => sp.GetRequiredService<ITenantContext<TTenant>>());
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
        serviceCollection.AddScoped<IProductContext<TProduct>, ProductContext<TProduct>>();
        serviceCollection.AddScoped<IProductContext>(sp => sp.GetRequiredService<IProductContext<TProduct>>());
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