namespace DocumentProcessing.Application.Abstractions;

public interface ICommand<out TResult>
{
}

public interface ICommand : ICommand<Unit>
{
}

public readonly struct Unit
{
    public static readonly Unit Value = new();
}