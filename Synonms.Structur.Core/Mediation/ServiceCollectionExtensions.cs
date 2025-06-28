using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Synonms.Structur.Core.System.Reflection;

namespace Synonms.Structur.Core.Mediation;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection RegisterMediator(this IServiceCollection serviceCollection, Assembly[] assemblies)
    {
        foreach (Assembly assembly in assemblies)
        {
            List<Type> commandHandlerTypes = assembly.GetImplementationsOfGenericInterface(typeof(ICommandHandler<>)).ToList();

            commandHandlerTypes.ForEach(implementationType =>
            {
                List<Type> implementedInterfaces = implementationType.GetInterfaces().ToList();
                implementedInterfaces.ForEach(implementedInterface =>
                {
                    serviceCollection.AddTransient(implementedInterface, implementationType);
                });
            });
            
            List<Type> queryHandlerTypes = assembly.GetImplementationsOfGenericInterface(typeof(IQueryHandler<,>)).ToList();

            queryHandlerTypes.ForEach(implementationType =>
            {
                List<Type> implementedInterfaces = implementationType.GetInterfaces().ToList();
                implementedInterfaces.ForEach(implementedInterface =>
                {
                    serviceCollection.AddTransient(implementedInterface, implementationType);
                });
            });
        }

        serviceCollection.AddSingleton<IMediator, Mediator>();
        
        return serviceCollection;
    }
}