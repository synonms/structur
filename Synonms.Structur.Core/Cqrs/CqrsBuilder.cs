using Microsoft.Extensions.DependencyInjection;

namespace Synonms.Structur.Core.Cqrs;

public class CqrsBuilder
{
    private readonly IServiceCollection _serviceCollection;

    public CqrsBuilder(IServiceCollection serviceCollection)
    {
        _serviceCollection = serviceCollection;
    }

    /// <summary>
    /// NOTE: Add behaviour decorators in the reverse order of execution, i.e. last first.
    /// </summary>
    /// <param name="behaviourType"></param>
    /// <returns></returns>
    public CqrsBuilder WithCommandBehaviour(Type behaviourType)
    {
        _serviceCollection.Decorate(typeof(ICommandHandler<,>), behaviourType);
            
        return this;
    }
    
    /// <summary>
    /// NOTE: Add behaviour decorators in the reverse order of execution, i.e. last first.
    /// </summary>
    /// <param name="behaviourType"></param>
    /// <returns></returns>
    public CqrsBuilder WithQueryBehaviour(Type behaviourType)
    {
        _serviceCollection.Decorate(typeof(IQueryHandler<,>), behaviourType);
            
        return this;
    }
}