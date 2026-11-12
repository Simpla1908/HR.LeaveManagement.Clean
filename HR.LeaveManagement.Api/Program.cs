
using HR.LeaveManagement.Api;
using HR.LeaveManagement.Api.Middleware;
using HR.LeaveManagement.Application;
using HR.LeaveManagement.Identity;
using HR.LeaveManagement.Infrastructure;
using HR.LeaveManagement.Identity.Services;
using HR.LeaveManagement.Persistence;
using NSwag;
using NSwag.Generation.Processors.Security;

var builder = WebApplication.CreateBuilder(args);

// ============================================
// 1. CONFIGURATION DES SERVICES
// ============================================

// Contrôleurs ASP.NET Core.
builder.Services.AddControllers();

// Couche Application : MediatR, AutoMapper, FluentValidation.
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// Couche Persistence : SQL Server, DbContext, repositories.
builder.Services.AddPersistenceServices(
    builder.Configuration);

// Couche Identity : utilisateurs, rôles, JWT.
builder.Services.AddIdentityServices(
    builder.Configuration);

// Gestion centralisée des erreurs.
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Documentation Swagger/OpenAPI.
builder.Services.AddOpenApiDocument(config =>
{
    config.Title = "HR Leave Management API";
    config.Version = "v1";

    config.AddSecurity(
        "JWT",
        Array.Empty<string>(),
        new OpenApiSecurityScheme
        {
            Type = OpenApiSecuritySchemeType.ApiKey,
            Name = "Authorization",
            In = OpenApiSecurityApiKeyLocation.Header,
            Description = "Enter: Bearer {your JWT token}"
        });

    config.OperationProcessors.Add(
        new AspNetCoreOperationSecurityScopeProcessor("JWT"));
});

// Préparer une politique CORS pour le futur client Blazor.
// Ne pas activer une origine arbitraire.
builder.Services.AddClientCors(builder.Configuration);

// ============================================
// 2. CONSTRUCTION DE L'APPLICATION
// ============================================

var app = builder.Build();

// ============================================
// 3. PIPELINE HTTP
// ============================================

// Intercepter les exceptions non traitées.
app.UseExceptionHandler();

// Swagger seulement en développement.
if (app.Environment.IsDevelopment())
{
    app.UseOpenApi();
    app.UseSwaggerUi();
}

// Rediriger HTTP vers HTTPS.
app.UseHttpsRedirection();

// Appliquer CORS aux clients autorisés.
app.UseCors("BlazorClient");

// Identifier l'utilisateur.
app.UseAuthentication();

// Vérifier les rôles et les autorisations.
app.UseAuthorization();

// Associer les routes aux contrôleurs.
app.MapControllers();

// ============================================
// 4. INITIALISATION ET DÉMARRAGE
// ============================================

// À exécuter après application des migrations Identity.
await IdentityRoleSeeder.SeedRolesAsync(app.Services);

app.Run();
