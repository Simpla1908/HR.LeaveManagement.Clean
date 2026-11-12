
using System.Text;
using HR.LeaveManagement.Application.Contracts.Identity;
using HR.LeaveManagement.Application.Models.Identity;
using HR.LeaveManagement.Identity.DbContext;
using HR.LeaveManagement.Identity.Models;
using HR.LeaveManagement.Identity.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace HR.LeaveManagement.Identity;

public static class IdentityServicesRegistration
{
    public static IServiceCollection AddIdentityServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 1. Charger les paramètres JWT.
        services.Configure<JwtSettings>(
            configuration.GetSection("JwtSettings"));

        // 2. Configurer SQL Server pour Identity.
        services.AddDbContext<HrLeaveManagementIdentityDbContext>(
            options =>
                options.UseSqlServer(
                    configuration.GetConnectionString(
                        "HrDatabaseConnectionString")));

        // 3. Configurer les utilisateurs et les rôles Identity.
        services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.Password.RequiredLength = 8;
            options.Password.RequireDigit = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireNonAlphanumeric = true;

            options.User.RequireUniqueEmail = true;

            options.Lockout.DefaultLockoutTimeSpan =
                TimeSpan.FromMinutes(15);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;
        })
        .AddRoles<IdentityRole>()
        .AddSignInManager()
        .AddEntityFrameworkStores<HrLeaveManagementIdentityDbContext>()
        .AddDefaultTokenProviders();

        // 4. Enregistrer les services applicatifs.
        services.AddHttpContextAccessor();

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();

        // 5. Récupérer et valider la clé de signature.
        var encodedKey = configuration["JwtSettings:Key"]
            ?? throw new InvalidOperationException(
                "JwtSettings:Key is not configured.");

        var keyBytes = Convert.FromBase64String(encodedKey);

        if (keyBytes.Length < 32)
        {
            throw new InvalidOperationException(
                "JWT signing key must contain at least 32 bytes.");
        }

        var issuer = configuration["JwtSettings:Issuer"]
            ?? throw new InvalidOperationException(
                "JWT issuer is missing.");

        var audience = configuration["JwtSettings:Audience"]
            ?? throw new InvalidOperationException(
                "JWT audience is missing.");

        // 6. Activer l'authentification JWT dans l'API.
        services.AddAuthentication(
            JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.MapInboundClaims = false;

            options.TokenValidationParameters =
                new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey =
                        new SymmetricSecurityKey(keyBytes),

                    ValidateIssuer = true,
                    ValidIssuer = issuer,

                    ValidateAudience = true,
                    ValidAudience = audience,

                    ValidateLifetime = true,
                    RequireExpirationTime = true,
                    ClockSkew = TimeSpan.Zero,

                    NameClaimType = "name",
                    RoleClaimType =
                        System.Security.Claims.ClaimTypes.Role
                };
        });

        // 7. Enregistrer les services d'autorisation.
        services.AddAuthorization();

        return services;
    }
}
