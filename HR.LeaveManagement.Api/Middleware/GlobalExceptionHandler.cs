
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
        // Transformer les exceptions connues en réponses HTTP.
        ProblemDetails problem;

        switch (exception)
        {
            case BadRequestException badRequest:
                problem = new ValidationProblemDetails(
                    badRequest.ValidationErrors)
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = badRequest.Message,
                    Type = "https://httpstatuses.com/400"
                };
                break;

            case ForbiddenException:
                problem = new ProblemDetails
                {
                    Status = StatusCodes.Status403Forbidden,
                    Title = "Access denied"
                };
                break;

            case NotFoundException notFound:
                problem = new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Resource not found",
                    Detail = notFound.Message,
                    Type = "https://httpstatuses.com/404"
                };
                break;

            default:
                // Laisser ASP.NET Core gérer les erreurs inconnues.
                _logger.LogError(
                    exception,
                    "Unhandled application exception");

                return false;
        }

        // Journaliser les erreurs métier attendues.
        _logger.LogWarning(
            "Request failed: {ErrorMessage}",
            exception.Message);

        // Définir le code HTTP et le format JSON.
        httpContext.Response.StatusCode = problem.Status!.Value;

        httpContext.Response.ContentType =
            "application/problem+json";

        // Envoyer la réponse au client.
        await httpContext.Response.WriteAsJsonAsync(
            problem,
            cancellationToken);

        return true;
    }
}
