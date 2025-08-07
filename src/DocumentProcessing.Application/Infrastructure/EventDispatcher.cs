using Microsoft.Extensions.DependencyInjection;
using DocumentProcessing.Application.Abstractions;

namespace DocumentProcessing.Application.Infrastructure;

public class EventDispatcher : IEventDispatcher
{
    private readonly IServiceProvider _serviceProvider;

    public EventDispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task DispatchAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : IEvent
    {
        var handlerType = typeof(IEventHandler<>).MakeGenericType(typeof(TEvent));
        var handlers = _serviceProvider.GetServices(handlerType);

        var tasks = new List<Task>();
        
        foreach (var handler in handlers)
        {
            var method = handlerType.GetMethod("HandleAsync");
            if (method == null)
                continue;

            var result = method.Invoke(handler, new object[] { @event, cancellationToken });
            if (result is Task task)
                tasks.Add(task);
        }

        if (tasks.Any())
            await Task.WhenAll(tasks);
    }
}