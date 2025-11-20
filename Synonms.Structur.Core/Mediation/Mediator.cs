using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Core.System.Reflection;

namespace Synonms.Structur.Core.Mediation;

public class Mediator : IMediator
{
    private readonly ILogger<Mediator> _logger;
    private readonly Dictionary<Type, ICommandHandler> _commandHandlers = new();
    private readonly Dictionary<Type, IQueryHandler> _queryHandlers = new();

    public Mediator(ILogger<Mediator> logger, IServiceScopeFactory serviceScopeFactory, Assembly[] assemblies)
    {
        _logger = logger;
        
        IServiceScope serviceScope = serviceScopeFactory.CreateScope();
        
        RegisterCommandHandlersFrom(assemblies, serviceScope);
        RegisterQueryHandlersFrom(assemblies, serviceScope);
    }

    public async Task<Result<TCommandResponse>> SendCommandAsync<TCommand, TCommandResponse>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : Command
        where TCommandResponse : CommandResponse
    {
        if (_commandHandlers.TryGetValue(typeof(TCommand), out ICommandHandler? commandHandler) is false)
        {
            return new MediationFault("Handler not found for command type {CommandType}.", typeof(TCommand).Name);
        }

        if (commandHandler is ICommandHandler<TCommand, TCommandResponse> commandHandlerForCommand)
        {
            return await commandHandlerForCommand.HandleAsync(command, cancellationToken);
        }
        
        return new MediationFault("Invalid handler {CommandHandlerType} found for command type {CommandType}.", commandHandler.GetType().Name, typeof(TCommand).Name);
    }
    
    public async Task<Result<TQueryResponse>> SendQueryAsync<TQuery, TQueryResponse>(TQuery query, CancellationToken cancellationToken = default)
        where TQuery : Query
        where TQueryResponse : QueryResponse
    {
        if (_queryHandlers.TryGetValue(typeof(TQuery), out IQueryHandler? queryHandler) is false)
        {
            return new MediationFault("Handler not found for query type {QueryType}.", typeof(TQuery).Name);
        }

        if (queryHandler is IQueryHandler<TQuery, TQueryResponse> queryHandlerForQuery)
        {
            return await queryHandlerForQuery.HandleAsync(query, cancellationToken);
        }
        
        return new MediationFault("Invalid handler {QueryHandlerType} found for query type {QueryType}.", queryHandler.GetType().Name, typeof(TQuery).Name);
    }
    
    private void RegisterCommandHandlersFrom(Assembly[] assemblies, IServiceScope serviceScope)
    {
        foreach (Assembly assembly in assemblies)
        {
            List<Type> commandHandlerTypes = assembly.GetImplementationsOfGenericInterface(typeof(ICommandHandler<,>)).ToList();

            foreach (Type commandHandlerType in commandHandlerTypes)
            {
                Type? interfaceType = commandHandlerType.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommandHandler<,>));
                Type? commandType = interfaceType?.GetGenericArguments().FirstOrDefault();
                Type? commandResponseType = interfaceType?.GetGenericArguments().LastOrDefault();

                if (interfaceType is not null && commandType is not null)
                {
                    if (serviceScope.ServiceProvider.GetService(interfaceType) is ICommandHandler commandHandlerImplementation)
                    {
                        _commandHandlers.Add(commandHandlerType, commandHandlerImplementation);
                        
                        _logger.LogDebug("Mediator registered handler {CommandHandlerType} for command {CommandType},", commandHandlerImplementation.GetType().Name, commandType.Name);
                    }
                }
                else
                {
                    if (interfaceType is null)
                    {
                        _logger.LogWarning("Command handler interface type is null for {CommandHandlerType}.", commandHandlerType.Name);
                    }

                    if (commandType is null)
                    {
                        _logger.LogWarning("Command handler command type is null for {CommandHandlerType}.", commandHandlerType.Name);
                    }
                }
            }
        }
    }
    
    private void RegisterQueryHandlersFrom(Assembly[] assemblies, IServiceScope serviceScope)
    {
        foreach (Assembly assembly in assemblies)
        {
            List<Type> queryHandlerTypes = assembly.GetImplementationsOfGenericInterface(typeof(IQueryHandler<,>)).ToList();

            foreach (Type queryHandlerType in queryHandlerTypes)
            {
                Type? interfaceType = queryHandlerType.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IQueryHandler<,>));
                Type? queryType = interfaceType?.GetGenericArguments().FirstOrDefault();
                Type? queryResponseType = interfaceType?.GetGenericArguments().LastOrDefault();

                if (interfaceType is not null && queryType is not null)
                {
                    if (serviceScope.ServiceProvider.GetService(interfaceType) is IQueryHandler queryHandlerImplementation)
                    {
                        _queryHandlers.Add(queryHandlerType, queryHandlerImplementation);
                        
                        _logger.LogDebug("Mediator registered handler {QueryHandlerType} for query {QueryType},", queryHandlerImplementation.GetType().Name, queryType.Name);
                    }
                }
                else
                {
                    if (interfaceType is null)
                    {
                        _logger.LogWarning("Query handler interface type is null for {QueryHandlerType}.", queryHandlerType.Name);
                    }

                    if (queryType is null)
                    {
                        _logger.LogWarning("Query handler query type is null for {QueryHandlerType}.", queryHandlerType.Name);
                    }
                }
            }
        }
    }
}