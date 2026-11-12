using System.Net.Http.Headers;
using System.Net.Http.Json;
using HR.LeaveManagement.Api;
using HR.LeaveManagement.Api.Controllers;
using HR.LeaveManagement.Api.Middleware;
using HR.LeaveManagement.Application;
using HR.LeaveManagement.Application.Models.Identity;
using HR.LeaveManagement.Domain;
using HR.LeaveManagement.Identity;
using HR.LeaveManagement.Identity.DbContext;
using HR.LeaveManagement.Identity.Models;
using HR.LeaveManagement.Identity.Services;
using HR.LeaveManagement.Infrastructure;
using HR.LeaveManagement.Persistence;
using HR.LeaveManagement.Persistence.DatabaseContext;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HR.LeaveManagement.Persistence.IntegrationTests;

public sealed class ApiFixture : IAsyncLifetime
{
    private WebApplication? app;
    private readonly string databaseName = "HRLeaveTests_" + Guid.NewGuid().ToString("N");
    public IServiceProvider Services => app!.Services;
    public string EmployeeId { get; private set; } = "";
    public string OtherEmployeeId { get; private set; } = "";
    public string EmployeeToken { get; private set; } = "";
    public string OtherEmployeeToken { get; private set; } = "";
    public string AdminToken { get; private set; } = "";
    private Uri? baseAddress;

    public async ValueTask InitializeAsync()
    {
        var connection = new SqlConnectionStringBuilder(
            Environment.GetEnvironmentVariable("HR_TEST_CONNECTION")
            ?? @"Server=(localdb)\MSSQLLocalDB;Integrated Security=true;TrustServerCertificate=true")
        { InitialCatalog = databaseName };
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        { EnvironmentName = "Development", ApplicationName = typeof(AuthController).Assembly.FullName });
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:HrDatabaseConnectionString"] = connection.ConnectionString,
            ["JwtSettings:Key"] = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)),
            ["JwtSettings:Issuer"] = "LeaveTests",
            ["JwtSettings:Audience"] = "LeaveTests",
            ["JwtSettings:DurationInMinutes"] = "30",
            ["EmailSettings:Enabled"] = "false"
        });
        builder.Services.AddControllers().AddApplicationPart(typeof(AuthController).Assembly);
        builder.Services.AddApplicationServices();
        builder.Services.AddClientCors(builder.Configuration);
        builder.Services.AddPersistenceServices(builder.Configuration);
        builder.Services.AddIdentityServices(builder.Configuration);
        builder.Services.AddInfrastructureServices(builder.Configuration);
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
        builder.Services.AddProblemDetails();
        app = builder.Build();
        app.UseExceptionHandler();
        app.UseCors("BlazorClient");
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        using (var scope = Services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<HrDatabaseContext>().Database.MigrateAsync();
            await scope.ServiceProvider.GetRequiredService<HrLeaveManagementIdentityDbContext>().Database.MigrateAsync();
        }
        await IdentityRoleSeeder.SeedRolesAsync(Services);
        using (var scope = Services.CreateScope())
        {
            var manager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<ApplicationUser>>();
            EmployeeId = await AddUser(manager, "employee@example.test", "Employee");
            OtherEmployeeId = await AddUser(manager, "other@example.test", "Employee");
            await AddUser(manager, "admin@example.test", "Administrator");
        }
        await app.StartAsync();
        baseAddress = new Uri(Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single());
        EmployeeToken = await Login("employee@example.test");
        OtherEmployeeToken = await Login("other@example.test");
        AdminToken = await Login("admin@example.test");
    }

    private static async Task<string> AddUser(Microsoft.AspNetCore.Identity.UserManager<ApplicationUser> manager, string email, string role)
    {
        var user = new ApplicationUser { UserName = email, Email = email, FirstName = "Test", LastName = role };
        var created = await manager.CreateAsync(user, "Test!Pass123");
        Assert.True(created.Succeeded, string.Join("; ", created.Errors.Select(e => e.Description)));
        Assert.True((await manager.AddToRoleAsync(user, role)).Succeeded);
        return user.Id;
    }
    private async Task<string> Login(string email)
    {
        using var client = CreateClient();
        using var response = await client.PostAsJsonAsync("api/auth/login", new { email, password = "Test!Pass123" });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthResponse>())!.Token;
    }
    public HttpClient CreateClient(string? token = null)
    {
        var client = new HttpClient { BaseAddress = baseAddress, Timeout = TimeSpan.FromSeconds(60) };
        if (token is not null) client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
    public async Task ResetAsync(int days = 10)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HrDatabaseContext>();
        await db.LeaveRequests.ExecuteDeleteAsync();
        await db.LeaveAllocations.ExecuteDeleteAsync();
        db.LeaveAllocations.AddRange(
            new LeaveAllocation { EmployeeId = EmployeeId, LeaveTypeId = 1, Period = 2030, NumberOfDays = days },
            new LeaveAllocation { EmployeeId = OtherEmployeeId, LeaveTypeId = 1, Period = 2030, NumberOfDays = 10 },
            new LeaveAllocation { EmployeeId = EmployeeId, LeaveTypeId = 1, Period = 2031, NumberOfDays = 30 });
        await db.SaveChangesAsync();
    }
    public async Task<int> SeedRequestAsync(string? employeeId = null, int start = 1, int end = 3)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HrDatabaseContext>();
        var entity = new LeaveRequest { RequestingEmployeeId = employeeId ?? EmployeeId, LeaveTypeId = 1,
            StartDate = new DateTime(2030, 1, start), EndDate = new DateTime(2030, 1, end), DateRequested = DateTime.UtcNow };
        db.LeaveRequests.Add(entity);
        await db.SaveChangesAsync();
        return entity.Id;
    }
    public async Task<int> BalanceAsync(int year = 2030)
    {
        using var scope = Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<HrDatabaseContext>().LeaveAllocations
            .Where(x => x.EmployeeId == EmployeeId && x.Period == year).Select(x => x.NumberOfDays).SingleAsync();
    }
    public async ValueTask DisposeAsync()
    {
        if (app is null) return;
        try
        {
            await app.StopAsync();
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<HrDatabaseContext>();
            // Only this fixture's generated database is eligible for cleanup.
            Assert.Equal(databaseName, db.Database.GetDbConnection().Database);
            Assert.StartsWith("HRLeaveTests_", databaseName);
            await db.Database.EnsureDeletedAsync();
        }
        finally { await app.DisposeAsync(); }
    }
}
