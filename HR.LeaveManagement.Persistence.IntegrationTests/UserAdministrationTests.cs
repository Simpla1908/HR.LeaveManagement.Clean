using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HR.LeaveManagement.Application.Models.Identity;
using HR.LeaveManagement.Identity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace HR.LeaveManagement.Persistence.IntegrationTests;

public class UserAdministrationTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    [Fact]
    public async Task Administrator_can_create_edit_change_role_and_delete_an_account()
    {
        using var admin = fixture.CreateClient(fixture.AdminToken);
        var suffix = Guid.NewGuid().ToString("N");
        var email = $"person-{suffix}@example.test";
        var create = new { firstName = "Alice", lastName = "Martin", userName = "person-" + suffix, email, role = "Employee", password = "Test!Pass123" };
        using var created = await admin.PostAsJsonAsync("api/users", create, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken)).GetProperty("id").GetString()!;
        var users = await admin.GetFromJsonAsync<List<UserSummary>>("api/users", TestContext.Current.CancellationToken);
        Assert.Contains(users!, x => x.Id == id && x.Role == "Employee" && x.FirstName == "Alice");

        using var originalLogin = await admin.PostAsJsonAsync("api/auth/login", new { email, password = "Test!Pass123" }, TestContext.Current.CancellationToken);
        var oldToken = (await originalLogin.Content.ReadFromJsonAsync<AuthResponse>(TestContext.Current.CancellationToken))!.Token;
        using var oldSession = fixture.CreateClient(oldToken);
        using var updated = await admin.PutAsJsonAsync($"api/users/{id}", new
        { firstName = "Alicia", lastName = "Martin", userName = create.userName, email, role = "Administrator" }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, updated.StatusCode);
        using var invalidated = await oldSession.GetAsync("api/auth/me", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, invalidated.StatusCode);

        // Reproduire un compte de laboratoire qui possède les deux rôles.
        using (var scope = fixture.Services.CreateScope())
        {
            var manager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var account = (await manager.FindByIdAsync(id))!;
            Assert.True((await manager.AddToRoleAsync(account, "Employee")).Succeeded);
        }

        using var newLogin = await admin.PostAsJsonAsync("api/auth/login", new { email, password = "Test!Pass123" }, TestContext.Current.CancellationToken);
        var newToken = (await newLogin.Content.ReadFromJsonAsync<AuthResponse>(TestContext.Current.CancellationToken))!.Token;
        using var newSession = fixture.CreateClient(newToken);
        using var authorized = await newSession.GetAsync("api/users", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, authorized.StatusCode);
        var accounts = await authorized.Content.ReadFromJsonAsync<List<UserSummary>>(TestContext.Current.CancellationToken);
        Assert.Contains(accounts!, x => x.Id == id && x.FirstName == "Alicia" && x.Role == "Administrator");

        using var demoted = await admin.PutAsJsonAsync($"api/users/{id}", new
        { firstName = "Alicia", lastName = "Martin", userName = create.userName, email, role = "Employee" }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, demoted.StatusCode);
        using var revoked = await newSession.GetAsync("api/users", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, revoked.StatusCode);
        using var employeeLogin = await admin.PostAsJsonAsync("api/auth/login", new { email, password = "Test!Pass123" }, TestContext.Current.CancellationToken);
        var employeeToken = (await employeeLogin.Content.ReadFromJsonAsync<AuthResponse>(TestContext.Current.CancellationToken))!.Token;
        using var employeeSession = fixture.CreateClient(employeeToken);
        using var forbidden = await employeeSession.GetAsync("api/users", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        using var deleted = await admin.DeleteAsync($"api/users/{id}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        using var deletedSession = await employeeSession.GetAsync("api/auth/me", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, deletedSession.StatusCode);
    }

    [Fact]
    public async Task Employees_and_anonymous_callers_cannot_manage_accounts()
    {
        using var anonymous = fixture.CreateClient();
        using var unauthenticated = await anonymous.GetAsync("api/users", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, unauthenticated.StatusCode);
        using var employee = fixture.CreateClient(fixture.EmployeeToken);
        using var list = await employee.GetAsync("api/users", TestContext.Current.CancellationToken);
        using var create = await employee.PostAsJsonAsync("api/users", new { }, TestContext.Current.CancellationToken);
        using var edit = await employee.PutAsJsonAsync($"api/users/{fixture.OtherEmployeeId}", new { }, TestContext.Current.CancellationToken);
        using var delete = await employee.DeleteAsync($"api/users/{fixture.OtherEmployeeId}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Forbidden, list.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, create.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, edit.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
    }

    [Fact]
    public async Task Administrator_cannot_delete_or_demote_their_own_account()
    {
        using var admin = fixture.CreateClient(fixture.AdminToken);
        var accounts = await admin.GetFromJsonAsync<List<UserSummary>>("api/users", TestContext.Current.CancellationToken);
        var account = accounts!.Single(x => x.Email == "admin@example.test");
        using var demoted = await admin.PutAsJsonAsync($"api/users/{account.Id}", new
        { account.FirstName, account.LastName, account.UserName, account.Email, role = "Employee" }, TestContext.Current.CancellationToken);
        using var deleted = await admin.DeleteAsync($"api/users/{account.Id}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, demoted.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, deleted.StatusCode);
    }

    [Fact]
    public async Task Referenced_accounts_and_leave_types_cannot_be_deleted()
    {
        await fixture.ResetAsync();
        using var admin = fixture.CreateClient(fixture.AdminToken);
        using var account = await admin.DeleteAsync($"api/users/{fixture.EmployeeId}", TestContext.Current.CancellationToken);
        using var type = await admin.DeleteAsync("api/LeaveTypes/1", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, account.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, type.StatusCode);
    }

    [Fact]
    public async Task Invalid_role_and_duplicate_email_are_rejected_without_creating_another_account()
    {
        using var admin = fixture.CreateClient(fixture.AdminToken);
        var before = await admin.GetFromJsonAsync<List<UserSummary>>("api/users", TestContext.Current.CancellationToken);
        using var invalid = await admin.PostAsJsonAsync("api/users", new
        { firstName = "Test", lastName = "User", userName = "invalid-user", email = "invalid@example.test", password = "Test!Pass123", role = "SuperAdmin" }, TestContext.Current.CancellationToken);
        using var duplicate = await admin.PostAsJsonAsync("api/users", new
        { firstName = "Test", lastName = "User", userName = "duplicate-user", email = "employee@example.test", password = "Test!Pass123", role = "Employee" }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, duplicate.StatusCode);
        var after = await admin.GetFromJsonAsync<List<UserSummary>>("api/users", TestContext.Current.CancellationToken);
        Assert.Equal(before!.Count, after!.Count);
    }

    [Fact]
    public async Task Administrator_can_create_edit_and_delete_an_unused_leave_type()
    {
        using var admin = fixture.CreateClient(fixture.AdminToken);
        var name = "Congé " + Guid.NewGuid().ToString("N");
        using var created = await admin.PostAsJsonAsync("api/LeaveTypes", new { name, defaultDays = 5 }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken)).GetProperty("id").GetInt32();
        using var edited = await admin.PutAsJsonAsync($"api/LeaveTypes/{id}", new { id, name, defaultDays = 8 }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, edited.StatusCode);
        using var deleted = await admin.DeleteAsync($"api/LeaveTypes/{id}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
    }
}
