using FluentValidation;
using FluentValidation.Results;

namespace FinanceSentry.Core.Cqrs;

public sealed class CommandValidationDecorator<TCommand, TResponse>(
    ICommandHandler<TCommand, TResponse> inner,
    IEnumerable<IValidator<TCommand>> validators)
    : ICommandHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    public async Task<TResponse> Handle(TCommand command, CancellationToken cancellationToken)
    {
        await ValidationRunner.RunAsync(validators, command, cancellationToken);
        return await inner.Handle(command, cancellationToken);
    }
}

public sealed class QueryValidationDecorator<TQuery, TResponse>(
    IQueryHandler<TQuery, TResponse> inner,
    IEnumerable<IValidator<TQuery>> validators)
    : IQueryHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    public async Task<TResponse> Handle(TQuery query, CancellationToken cancellationToken)
    {
        await ValidationRunner.RunAsync(validators, query, cancellationToken);
        return await inner.Handle(query, cancellationToken);
    }
}

internal static class ValidationRunner
{
    public static async Task RunAsync<T>(
        IEnumerable<IValidator<T>> validators,
        T input,
        CancellationToken cancellationToken)
    {
        var failures = new List<ValidationFailure>();
        foreach (var validator in validators)
        {
            var result = await validator.ValidateAsync(input, cancellationToken);
            if (!result.IsValid)
            {
                failures.AddRange(result.Errors);
            }
        }

        if (failures.Count > 0)
        {
            throw new ValidationException(failures);
        }
    }
}
