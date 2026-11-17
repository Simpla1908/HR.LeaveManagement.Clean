using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HR.LeaveManagement.Api.Controllers;

[ApiController]
[Route("health")]
[AllowAnonymous]
public class HealthController(HealthCheckService healthChecks) : ControllerBase
{
    // Vérifier la disponibilité de l'API sans tester les dépendances externes.
    [HttpGet]
    [Produces("text/plain")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status503ServiceUnavailable)]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var report = await healthChecks.CheckHealthAsync(cancellationToken);

        return new ContentResult
        {
            Content = report.Status.ToString(),
            ContentType = "text/plain",
            StatusCode = report.Status == HealthStatus.Unhealthy
                ? StatusCodes.Status503ServiceUnavailable
                : StatusCodes.Status200OK
        };
    }
}
