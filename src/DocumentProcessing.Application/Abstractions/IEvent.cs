namespace DocumentProcessing.Application.Abstractions;

public interface IEvent
{
    DateTime OccurredAt { get; }
}