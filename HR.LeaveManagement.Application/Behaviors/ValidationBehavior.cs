
//Pipeline Behavior
using FluentValidation;
using FluentValidation.Results;
using HR.LeaveManagement.Application.Exceptions;
using MediatR;

namespace HR.LeaveManagement.Application.Behaviors;

public class ValidationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(
        IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Aucun validateur : continuer directement.
        if (!_validators.Any())
        {
            return await next(cancellationToken);
        }

        // Créer le contexte de validation.
        var context = new ValidationContext<TRequest>(request);

        // Exécuter tous les validateurs associés.
        var failures = new List<ValidationFailure>();

        foreach (var validator in _validators)
        {
            var result = await validator.ValidateAsync(
                context,
                cancellationToken);

            failures.AddRange(result.Errors);
        }

        // Si des erreurs existent, arrêter le traitement.
        if (failures.Count > 0)
        {
            throw new BadRequestException(
                "Validation failed",
                failures);
        }

        // Sinon, continuer vers le prochain composant.
        return await next(cancellationToken);
    }
}
