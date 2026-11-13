
using FluentValidation.Results;

namespace HR.LeaveManagement.Application.Exceptions;

// Signale des données invalides ou une opération refusée par une règle métier.
public class BadRequestException : Exception
{
    // Pour chaque champ, conserver tous ses messages d'erreur.
    public Dictionary<string, List<string>> ValidationErrors { get; }
        = new Dictionary<string, List<string>>();

    // Constructeur pour une erreur métier générale.
    public BadRequestException(string message)
        : base(message)
    {
    }

    // Constructeur pour les erreurs FluentValidation.
    public BadRequestException(
        string message,
        IEnumerable<ValidationFailure> errors)
        : base(message)
    {
        foreach (var error in errors)
        {
            // Créer la liste lors de la première erreur de ce champ.
            if (!ValidationErrors.ContainsKey(error.PropertyName))
            {
                ValidationErrors[error.PropertyName] = new List<string>();
            }

            ValidationErrors[error.PropertyName].Add(error.ErrorMessage);
        }
    }
}
