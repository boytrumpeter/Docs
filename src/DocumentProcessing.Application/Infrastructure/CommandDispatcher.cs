using Microsoft.Extensions.DependencyInjection;
using DocumentProcessing.Application.Abstractions;

namespace DocumentProcessing.Application.Infrastructure;

public class CommandDispatcher : ICommandDispatcher
{
    private readonly IServiceProvider _serviceProvider;

    public CommandDispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<TResult> DispatchAsync<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default)
    {
        var handlerType = typeof(ICommandHandler<,>).MakeGenericType(command.GetType(), typeof(TResult));
        var handler = _serviceProvider.GetRequiredService(handlerType);
        
        var method = handlerType.GetMethod("HandleAsync");
        if (method == null)
            throw new InvalidOperationException($"HandleAsync method not found for {handlerType.Name}");

        var result = method.Invoke(handler, new object[] { command, cancellationToken });
        
        if (result is Task<TResult> task)
            return await task;
            
        throw new InvalidOperationException($"Handler for {command.GetType().Name} did not return Task<{typeof(TResult).Name}>");
    }

    public async Task DispatchAsync(ICommand command, CancellationToken cancellationToken = default)
    {
        await DispatchAsync<Unit>(command, cancellationToken);
    }
}