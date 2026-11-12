
using FluentValidation.Results;

namespace HR.LeaveManagement.Application.Exceptions;

public class BadRequestException : Exception
{
    // Dictionnaire : nom de propriété → messages d'erreur.
    public IDictionary<string, string[]> ValidationErrors
    { get; }

    // Constructeur pour une erreur métier générale.
    public BadRequestException(string message)
        : base(message)
    {
        ValidationErrors = new Dictionary<string, string[]>();
    }

    // Constructeur pour les erreurs FluentValidation.
    public BadRequestException(
        string message,
        IEnumerable<ValidationFailure> errors)
        : base(message)
    {
        ValidationErrors = errors
            .GroupBy(error => error.PropertyName)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(error => error.ErrorMessage)
                    .ToArray());
    }
}
