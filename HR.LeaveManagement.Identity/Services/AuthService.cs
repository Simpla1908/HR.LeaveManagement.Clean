
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using HR.LeaveManagement.Application.Contracts.Identity;
using HR.LeaveManagement.Application.Exceptions;
using HR.LeaveManagement.Application.Models.Identity;
using HR.LeaveManagement.Identity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace HR.LeaveManagement.Identity.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly JwtSettings _jwtSettings;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IOptions<JwtSettings> jwtSettings)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtSettings = jwtSettings.Value;
    }

    public async Task<AuthResponse> Login(AuthRequest request)
    {
        // Rechercher l'utilisateur par son adresse email.
        var user = await _userManager.FindByEmailAsync(
            request.Email);

        if (user is null)
        {
            throw new BadRequestException(
                "Invalid email or password.");
        }

        // Vérifier le mot de passe et prendre en compte le verrouillage.
        var result = await _signInManager
            .CheckPasswordSignInAsync(
                user,
                request.Password,
                lockoutOnFailure: true);

        if (!result.Succeeded)
        {
            throw new BadRequestException(
                "Invalid email or password.");
        }

        // Générer un JWT après authentification réussie.
        var token = await GenerateTokenAsync(user);

        return new AuthResponse
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            UserName = user.UserName ?? string.Empty,
            Token = token
        };
    }

    public async Task<RegistrationResponse> Register(
        RegistrationRequest request)
    {
        // Créer un utilisateur Identity.
        var user = new ApplicationUser
        {
            Email = request.Email,
            UserName = request.UserName,
            FirstName = request.FirstName,
            LastName = request.LastName,

            // Une confirmation d'email sera ajoutée ultérieurement.
            EmailConfirmed = false
        };

        var result = await _userManager.CreateAsync(
            user,
            request.Password);

        if (!result.Succeeded)
        {
            var errors = string.Join(
                "; ",
                result.Errors.Select(x => x.Description));

            throw new BadRequestException(errors);
        }

        // Un compte créé ici est toujours un employé.
        var roleResult = await _userManager.AddToRoleAsync(
            user,
            "Employee");

        if (!roleResult.Succeeded)
        {
            throw new InvalidOperationException(
                "Could not assign the Employee role.");
        }

        return new RegistrationResponse
        {
            UserId = user.Id
        };
    }

    private async Task<string> GenerateTokenAsync(
        ApplicationUser user)
    {
        // Charger les rôles et claims de l'utilisateur.
        var roles = await _userManager.GetRolesAsync(user);
        var userClaims = await _userManager.GetClaimsAsync(user);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Jti,
                Guid.NewGuid().ToString()),
            new("uid", user.Id),
            new("security_stamp", await _userManager.GetSecurityStampAsync(user)),
            new("name", user.UserName ?? string.Empty),
            new(JwtRegisteredClaimNames.Email,
                user.Email ?? string.Empty)
        };

        claims.AddRange(userClaims);

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        // Décoder la clé de signature.
        var key = new SymmetricSecurityKey(
            Convert.FromBase64String(_jwtSettings.Key));

        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256);

        // Construire le jeton.
        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(
                _jwtSettings.DurationInMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler()
            .WriteToken(token);
    }
}
