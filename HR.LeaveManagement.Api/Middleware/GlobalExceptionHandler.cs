
using HR.LeaveManagement.Application.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace HR.LeaveManagement.Api.Middleware;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(
        ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // 1. Choisir la réponse HTTP selon le problème rencontré.
        int statusCode;
        string title;
        string? detail = null;

        if (exception is BadRequestException)
        {
            statusCode = StatusCodes.Status400BadRequest;
            title = exception.Message;
        }
        else if (exception is ForbiddenException)
        {
            statusCode = StatusCodes.Status403Forbidden;
            title = "Access denied";
        }
        else if (exception is NotFoundException)
        {
            statusCode = StatusCodes.Status404NotFound;
            title = "Resource not found";
            detail = exception.Message;
        }
        else
        {
            // Une erreur inattendue est journalisée puis confiée à ASP.NET Core.
            _logger.LogError(exception, "Unhandled application exception");
            return false;
        }

        // 2. Construire un seul format de réponse pour les erreurs connues.
        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Type = $"https://httpstatuses.com/{statusCode}"
        };

        if (exception is BadRequestException badRequest)
        {
            // Extensions ajoute le champ "errors" dans le JSON envoyé au client.
            problem.Extensions["errors"] = badRequest.ValidationErrors;
        }

        // Journaliser les erreurs métier attendues.
        _logger.LogWarning(
            "Request failed: {ErrorMessage}",
            exception.Message);

        // 3. Envoyer le code HTTP et les informations d'erreur au client.
        httpContext.Response.StatusCode = statusCode;

        await httpContext.Response.WriteAsJsonAsync(
            problem,
            options: null,
            contentType: "application/problem+json",
            cancellationToken: cancellationToken);

        return true;
    }
}
