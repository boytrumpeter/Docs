namespace DocumentProcessing.Application.Abstractions;

public interface ICommandDispatcher
{
    Task<TResult> DispatchAsync<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default);
    Task DispatchAsync(ICommand command, CancellationToken cancellationToken = default);
}