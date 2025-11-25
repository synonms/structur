using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Synonms.Structur.Core.Cqrs;

public static class ServiceCollectionExtensions
{
    public static CqrsBuilder RegisterCqrs(this IServiceCollection serviceCollection, Assembly[] handlerAssemblies)
    {
        serviceCollection.Scan(typeSourceSelector =>
            typeSourceSelector
                .FromAssemblies(handlerAssemblies)
                .AddClasses(implementationTypeFilter => implementationTypeFilter.AssignableTo(typeof(ICommandHandler<,>)), publicOnly: false).AsImplementedInterfaces()
                .WithScopedLifetime());

        serviceCollection.Scan(typeSourceSelector =>
            typeSourceSelector
                .FromAssemblies(handlerAssemblies)
                .AddClasses(implementationTypeFilter => implementationTypeFilter.AssignableTo(typeof(IQueryHandler<,>)), publicOnly: false).AsImplementedInterfaces()
                .WithScopedLifetime());
        
        return new CqrsBuilder(serviceCollection);
    }
}