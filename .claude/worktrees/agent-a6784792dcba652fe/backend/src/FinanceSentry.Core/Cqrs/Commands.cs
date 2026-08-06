namespace FinanceSentry.Core.Cqrs;

public interface ICommand<out TResponse>;

public interface ICommandHandler<in TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    Task<TResponse> Handle(TCommand command, CancellationToken cancellationToken);
}

public readonly record struct Unit
{
    public static readonly Unit Value = default;
}
