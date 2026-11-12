
using HR.LeaveManagement.Application.Contracts.Identity;
using HR.LeaveManagement.Application.Models.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HR.LeaveManagement.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IWebHostEnvironment _environment;

    public AuthController(
        IAuthService authService,
        IWebHostEnvironment environment)
    {
        _authService = authService;
        _environment = environment;
    }

    // Authentification de l'utilisateur.
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(
        AuthRequest request)
    {
        var response = await _authService.Login(request);

        return Ok(response);
    }

    // Inscription temporaire pour notre laboratoire local.
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<RegistrationResponse>> Register(
        RegistrationRequest request)
    {
        if (!_environment.IsDevelopment())
        {
            return NotFound();
        }

        var response = await _authService.Register(request);

        return Ok(response);
    }

    // Récupérer les informations de l'utilisateur connecté.
    [Authorize]
    [HttpGet("me")]
    public IActionResult Me()
    {
        return Ok(new
        {
            UserId = User.FindFirstValue("uid"),
            UserName = User.FindFirstValue("name"),
            Email = User.FindFirstValue("email"),
            Roles = User.FindAll(ClaimTypes.Role)
                .Select(x => x.Value)
                .ToList()
        });
    }

    // Endpoint réservé à l'administrateur.
    [Authorize(Roles = "Administrator")]
    [HttpGet("admin")]
    public IActionResult AdminOnly()
    {
        return Ok(new
        {
            Message = "Administrator access granted."
        });
    }
}
