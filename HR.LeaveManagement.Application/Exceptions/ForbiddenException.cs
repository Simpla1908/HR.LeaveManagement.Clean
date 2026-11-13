namespace HR.LeaveManagement.Application.Exceptions;

// Signale que l'utilisateur n'a pas le droit d'effectuer l'opération.
public sealed class ForbiddenException : Exception
{
    public ForbiddenException()
        : base("You cannot access this resource.")
    {
    }
}
