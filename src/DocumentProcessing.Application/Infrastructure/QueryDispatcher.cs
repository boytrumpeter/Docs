using Microsoft.Extensions.DependencyInjection;
using DocumentProcessing.Application.Abstractions;

namespace DocumentProcessing.Application.Infrastructure;

public class QueryDispatcher : IQueryDispatcher
{
    private readonly IServiceProvider _serviceProvider;

    public QueryDispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<TResult> DispatchAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default)
    {
        var handlerType = typeof(IQueryHandler<,>).MakeGenericType(query.GetType(), typeof(TResult));
        var handler = _serviceProvider.GetRequiredService(handlerType);
        
        var method = handlerType.GetMethod("HandleAsync");
        if (method == null)
            throw new InvalidOperationException($"HandleAsync method not found for {handlerType.Name}");

        var result = method.Invoke(handler, new object[] { query, cancellationToken });
        
        if (result is Task<TResult> task)
            return await task;
            
        throw new InvalidOperationException($"Handler for {query.GetType().Name} did not return Task<{typeof(TResult).Name}>");
    }
}