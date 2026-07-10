namespace Erdmier.ZooTycoonLauncher.Application.Common.Behaviours;

/// <summary>
///     Mediator pipeline behaviour that runs every registered <see cref="IValidator{T}" /> for the incoming message before invoking the next handler. When any validator produces
///     failures, the pipeline short-circuits with an <see cref="ErrorOr{T}" /> value carrying one validation <see cref="Error" /> per failure.
/// </summary>
/// <typeparam name="TMessage">The Mediator message type (command or query).</typeparam>
/// <typeparam name="TResponse">The handler's response type; must be <see cref="IErrorOr" /> so we can short-circuit cleanly.</typeparam>
public sealed class ValidationBehaviour<TMessage, TResponse> : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
    where TResponse : IErrorOr
{
    private readonly IEnumerable<IValidator<TMessage>> _validators;

    /// <summary>Initialises a new instance with the validators resolved from DI.</summary>
    /// <param name="validators">Every registered validator for <typeparamref name="TMessage" />. Empty when no validation rules exist for this message.</param>
    public ValidationBehaviour(IEnumerable<IValidator<TMessage>> validators) => _validators = validators;

    /// <inheritdoc />
    public async ValueTask<TResponse> Handle(TMessage message, MessageHandlerDelegate<TMessage, TResponse> next, CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next(message, cancellationToken);
        }

        ValidationContext<TMessage> context = new(message);

        ValidationResult[] results = await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        List<Error> errors = results.SelectMany(r => r.Errors)
                                    .Where(f => f is not null)
                                    .Select(f => Error.Validation(f.PropertyName, f.ErrorMessage))
                                    .ToList();

        if (errors.Count == 0)
        {
            return await next(message, cancellationToken);
        }

        // TResponse is a closed ErrorOr<T> at every call site; we need a runtime constructor because there is no compile-time
        // path to construct the closed generic from a List<Error>. ErrorOr<T> exposes a public ctor (List<Error>).
        Type responseType = typeof(TResponse);

        return (TResponse)Activator.CreateInstance(responseType, errors)!;
    }
}
