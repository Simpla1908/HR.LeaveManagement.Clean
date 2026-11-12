using System.Net;
using System.Net.Http.Json;
using HR.LeaveManagement.Application.DTOs.LeaveRequest;
using HR.LeaveManagement.Application.Features.LeaveAllocation.Queries.GetLeaveAllocations;
using HR.LeaveManagement.Domain;
using HR.LeaveManagement.Persistence.DatabaseContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace HR.LeaveManagement.Persistence.IntegrationTests;

public sealed class LeaveApiTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    [Fact]
    public async Task Employee_can_create_list_read_and_edit_a_single_day_request()
    {
        await fixture.ResetAsync();
        using var client = fixture.CreateClient(fixture.EmployeeToken);
        var command = new { startDate = "2030-01-02", endDate = "2030-01-02", leaveTypeId = 1, requestComments = "Appointment" };
        using var created = await client.PostAsJsonAsync("api/LeaveRequests", command, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, created.StatusCode);
        var requests = await client.GetFromJsonAsync<List<LeaveRequestListDto>>("api/LeaveRequests", cancellationToken: TestContext.Current.CancellationToken);
        var request = Assert.Single(requests!);
        using var details = await client.GetAsync($"api/LeaveRequests/{request.Id}", cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, details.StatusCode);
        using var updated = await client.PutAsJsonAsync($"api/LeaveRequests/{request.Id}", new
        { id = request.Id, startDate = "2030-01-02", endDate = "2030-01-03", leaveTypeId = 1, requestComments = "Updated" }, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, updated.StatusCode);
        Assert.Equal(10, await fixture.BalanceAsync());
    }

    [Fact]
    public async Task Employees_only_see_their_own_requests_and_allocations()
    {
        await fixture.ResetAsync();
        await fixture.SeedRequestAsync();
        await fixture.SeedRequestAsync(fixture.OtherEmployeeId);
        using var client = fixture.CreateClient(fixture.EmployeeToken);
        var requests = await client.GetFromJsonAsync<List<LeaveRequestListDto>>("api/LeaveRequests", cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(fixture.EmployeeId, Assert.Single(requests!).RequestingEmployeeId);
        var allocations = await client.GetFromJsonAsync<List<LeaveAllocationDto>>("api/LeaveAllocations/mine", cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(2, allocations!.Count);
        Assert.All(allocations, x => Assert.Equal(fixture.EmployeeId, x.EmployeeId));
    }

    [Fact]
    public async Task Another_employee_cannot_read_edit_or_cancel_a_request()
    {
        await fixture.ResetAsync();
        var id = await fixture.SeedRequestAsync();
        using var client = fixture.CreateClient(fixture.OtherEmployeeToken);
        using var read = await client.GetAsync($"api/LeaveRequests/{id}", cancellationToken: TestContext.Current.CancellationToken);
        using var cancel = await client.PutAsync($"api/LeaveRequests/{id}/cancel", null, cancellationToken: TestContext.Current.CancellationToken);
        using var edit = await client.PutAsJsonAsync($"api/LeaveRequests/{id}", new
        { id, startDate = "2030-01-01", endDate = "2030-01-02", leaveTypeId = 1 }, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Forbidden, read.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, cancel.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, edit.StatusCode);
        Assert.Equal(10, await fixture.BalanceAsync());
    }

    [Fact]
    public async Task Approval_and_cancellation_are_idempotent_and_use_the_request_year()
    {
        await fixture.ResetAsync();
        var id = await fixture.SeedRequestAsync();
        using var admin = fixture.CreateClient(fixture.AdminToken);
        using var employee = fixture.CreateClient(fixture.EmployeeToken);
        for (var i = 0; i < 2; i++)
        {
            using var approved = await admin.PutAsJsonAsync($"api/LeaveRequests/{id}/approval", new { id, approved = true }, cancellationToken: TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.NoContent, approved.StatusCode);
        }
        Assert.Equal(7, await fixture.BalanceAsync());
        Assert.Equal(30, await fixture.BalanceAsync(2031));
        for (var i = 0; i < 2; i++)
        {
            using var cancelled = await employee.PutAsync($"api/LeaveRequests/{id}/cancel", null, cancellationToken: TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.NoContent, cancelled.StatusCode);
        }
        Assert.Equal(10, await fixture.BalanceAsync());
        using var reapproved = await admin.PutAsJsonAsync($"api/LeaveRequests/{id}/approval", new { id, approved = true }, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, reapproved.StatusCode);
    }

    [Fact]
    public async Task Reversing_approval_restores_the_balance_only_once()
    {
        await fixture.ResetAsync();
        var id = await fixture.SeedRequestAsync();
        using var admin = fixture.CreateClient(fixture.AdminToken);
        (await admin.PutAsJsonAsync($"api/LeaveRequests/{id}/approval", new { id, approved = true }, cancellationToken: TestContext.Current.CancellationToken)).EnsureSuccessStatusCode();
        for (var i = 0; i < 2; i++)
            (await admin.PutAsJsonAsync($"api/LeaveRequests/{id}/approval", new { id, approved = false }, cancellationToken: TestContext.Current.CancellationToken)).EnsureSuccessStatusCode();
        Assert.Equal(10, await fixture.BalanceAsync());
    }

    [Fact]
    public async Task Employee_cannot_approve_delete_or_allocate()
    {
        await fixture.ResetAsync();
        var id = await fixture.SeedRequestAsync();
        using var client = fixture.CreateClient(fixture.EmployeeToken);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PutAsJsonAsync($"api/LeaveRequests/{id}/approval", new { id, approved = true }, cancellationToken: TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.DeleteAsync($"api/LeaveRequests/{id}", cancellationToken: TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("api/LeaveAllocations", new { leaveTypeId = 1 }, cancellationToken: TestContext.Current.CancellationToken)).StatusCode);
    }

    [Fact]
    public async Task Insufficient_balance_does_not_change_request_status()
    {
        await fixture.ResetAsync(days: 2);
        var id = await fixture.SeedRequestAsync();
        using var admin = fixture.CreateClient(fixture.AdminToken);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PutAsJsonAsync($"api/LeaveRequests/{id}/approval", new { id, approved = true }, cancellationToken: TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(2, await fixture.BalanceAsync());
        using var scope = fixture.Services.CreateScope();
        Assert.Null((await scope.ServiceProvider.GetRequiredService<HrDatabaseContext>().LeaveRequests.SingleAsync(x => x.Id == id, cancellationToken: TestContext.Current.CancellationToken)).Approved);
    }

    [Fact]
    public async Task Concurrent_approvals_cannot_overdraw_the_same_allocation()
    {
        await fixture.ResetAsync(days: 4);
        var first = await fixture.SeedRequestAsync();
        var second = await fixture.SeedRequestAsync(start: 4, end: 6);
        using var admin = fixture.CreateClient(fixture.AdminToken);
        var results = await Task.WhenAll(
            admin.PutAsJsonAsync($"api/LeaveRequests/{first}/approval", new { id = first, approved = true }, cancellationToken: TestContext.Current.CancellationToken),
            admin.PutAsJsonAsync($"api/LeaveRequests/{second}/approval", new { id = second, approved = true }, cancellationToken: TestContext.Current.CancellationToken));
        Assert.Single(results, x => x.StatusCode == HttpStatusCode.NoContent);
        Assert.Single(results, x => x.StatusCode == HttpStatusCode.BadRequest);
        Assert.Equal(1, await fixture.BalanceAsync());
        foreach (var result in results) result.Dispose();
    }

    [Fact]
    public async Task Concurrent_duplicate_approval_and_cancel_only_adjust_the_balance_once()
    {
        await fixture.ResetAsync();
        var id = await fixture.SeedRequestAsync();
        using var admin = fixture.CreateClient(fixture.AdminToken);
        var approvals = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ =>
            admin.PutAsJsonAsync($"api/LeaveRequests/{id}/approval", new { id, approved = true }, cancellationToken: TestContext.Current.CancellationToken)));
        Assert.All(approvals, x => Assert.Equal(HttpStatusCode.NoContent, x.StatusCode));
        Assert.Equal(7, await fixture.BalanceAsync());
        var cancellations = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ =>
            admin.PutAsync($"api/LeaveRequests/{id}/cancel", null, cancellationToken: TestContext.Current.CancellationToken)));
        Assert.All(cancellations, x => Assert.Equal(HttpStatusCode.NoContent, x.StatusCode));
        Assert.Equal(10, await fixture.BalanceAsync());
        foreach (var response in approvals.Concat(cancellations)) response.Dispose();
    }

    [Fact]
    public async Task Approved_requests_cannot_be_edited_or_deleted_without_cancellation()
    {
        await fixture.ResetAsync();
        var id = await fixture.SeedRequestAsync();
        using var admin = fixture.CreateClient(fixture.AdminToken);
        (await admin.PutAsJsonAsync($"api/LeaveRequests/{id}/approval", new { id, approved = true }, cancellationToken: TestContext.Current.CancellationToken)).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.DeleteAsync($"api/LeaveRequests/{id}", cancellationToken: TestContext.Current.CancellationToken)).StatusCode);
        using var edit = await admin.PutAsJsonAsync($"api/LeaveRequests/{id}", new { id, startDate = "2030-01-01", endDate = "2030-01-02", leaveTypeId = 1 }, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, edit.StatusCode);
        Assert.Equal(7, await fixture.BalanceAsync());
    }

    [Theory]
    [InlineData("2030-01-03", "2030-01-01", 1)]
    [InlineData("2030-12-31", "2031-01-01", 1)]
    [InlineData("2030-01-01", "2030-01-02", 999)]
    [InlineData("2032-01-01", "2032-01-02", 1)]
    [InlineData("2030-01-01", "2030-01-20", 1)]
    public async Task Invalid_requests_return_validation_errors(string start, string end, int typeId)
    {
        await fixture.ResetAsync();
        using var client = fixture.CreateClient(fixture.EmployeeToken);
        using var response = await client.PostAsJsonAsync("api/LeaveRequests", new { startDate = start, endDate = end, leaveTypeId = typeId }, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Annual_allocations_are_not_created_twice()
    {
        await fixture.ResetAsync();
        using var admin = fixture.CreateClient(fixture.AdminToken);
        for (var i = 0; i < 2; i++)
            (await admin.PostAsJsonAsync("api/LeaveAllocations", new { leaveTypeId = 1 }, cancellationToken: TestContext.Current.CancellationToken)).EnsureSuccessStatusCode();
        using var scope = fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HrDatabaseContext>();
        Assert.Equal(2, await db.LeaveAllocations.CountAsync(x => x.Period == DateTime.Now.Year, cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Database_enforces_one_allocation_per_employee_type_and_year()
    {
        await fixture.ResetAsync();
        using var scope = fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HrDatabaseContext>();
        db.LeaveAllocations.Add(new LeaveAllocation { EmployeeId = fixture.EmployeeId, LeaveTypeId = 1, Period = 2030, NumberOfDays = 10 });
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync(cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Transaction_rolls_back_a_balance_change_when_the_request_save_fails()
    {
        await fixture.ResetAsync();
        using var scope = fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HrDatabaseContext>();
        var unit = scope.ServiceProvider.GetRequiredService<HR.LeaveManagement.Application.Contracts.Persistence.IUnitOfWork>();
        await Assert.ThrowsAsync<InvalidOperationException>(() => unit.ExecuteAsync<bool>(async () =>
        {
            var allocation = await db.LeaveAllocations.SingleAsync(x => x.EmployeeId == fixture.EmployeeId && x.Period == 2030, cancellationToken: TestContext.Current.CancellationToken);
            allocation.NumberOfDays -= 3;
            await db.SaveChangesAsync(cancellationToken: TestContext.Current.CancellationToken);
            throw new InvalidOperationException("Simulated request save failure");
        }, cancellationToken: TestContext.Current.CancellationToken));
        Assert.Equal(10, await fixture.BalanceAsync());
    }

    [Fact]
    public async Task Anonymous_requests_require_authentication()
    {
        using var client = fixture.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("api/LeaveRequests", cancellationToken: TestContext.Current.CancellationToken)).StatusCode);
    }

    [Fact]
    public async Task Registration_creates_an_employee_without_administrator_permissions()
    {
        using var client = fixture.CreateClient();
        var suffix = Guid.NewGuid().ToString("N");
        var email = $"registered-{suffix}@example.test";
        using var registered = await client.PostAsJsonAsync("api/auth/register", new
        {
            firstName = "New", lastName = "Employee", userName = "new-" + suffix,
            email, password = "Test!Pass123"
        }, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, registered.StatusCode);
        using var login = await client.PostAsJsonAsync("api/auth/login", new { email, password = "Test!Pass123" },
            cancellationToken: TestContext.Current.CancellationToken);
        login.EnsureSuccessStatusCode();
        var auth = await login.Content.ReadFromJsonAsync<HR.LeaveManagement.Application.Models.Identity.AuthResponse>(cancellationToken: TestContext.Current.CancellationToken);
        using var employee = fixture.CreateClient(auth!.Token);
        using var me = await employee.GetAsync("api/auth/me", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        Assert.Contains("Employee", await me.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.Equal(HttpStatusCode.Forbidden, (await employee.GetAsync("api/auth/admin", TestContext.Current.CancellationToken)).StatusCode);
        // Keep this shared fixture's employee count stable for allocation tests.
        using var scope = fixture.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<HR.LeaveManagement.Identity.Models.ApplicationUser>>();
        var user = await users.FindByEmailAsync(email);
        Assert.True((await users.DeleteAsync(user!)).Succeeded);
    }

    [Fact]
    public async Task Cors_allows_the_configured_Blazor_origin_and_rejects_other_origins()
    {
        using var client = fixture.CreateClient();
        foreach (var origin in new[] { "https://localhost:7040", "https://untrusted.example.test" })
        {
            using var preflight = new HttpRequestMessage(HttpMethod.Options, "api/LeaveRequests");
            preflight.Headers.Add("Origin", origin);
            preflight.Headers.Add("Access-Control-Request-Method", "POST");
            preflight.Headers.Add("Access-Control-Request-Headers", "authorization,content-type");
            using var result = await client.SendAsync(preflight, TestContext.Current.CancellationToken);
            if (origin == "https://localhost:7040")
                Assert.Equal(origin, Assert.Single(result.Headers.GetValues("Access-Control-Allow-Origin")));
            else Assert.False(result.Headers.Contains("Access-Control-Allow-Origin"));
        }
    }

    [Fact]
    public async Task Pending_requests_can_be_rejected_without_an_allocation()
    {
        await fixture.ResetAsync();
        var id = await fixture.SeedRequestAsync();
        using (var scope = fixture.Services.CreateScope())
            await scope.ServiceProvider.GetRequiredService<HrDatabaseContext>().LeaveAllocations
                .Where(x => x.EmployeeId == fixture.EmployeeId).ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        using var admin = fixture.CreateClient(fixture.AdminToken);
        using var response = await admin.PutAsJsonAsync($"api/LeaveRequests/{id}/approval", new { id, approved = false },
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Allocation_of_approved_requests_cannot_be_deleted()
    {
        await fixture.ResetAsync();
        var id = await fixture.SeedRequestAsync();
        using var admin = fixture.CreateClient(fixture.AdminToken);
        (await admin.PutAsJsonAsync($"api/LeaveRequests/{id}/approval", new { id, approved = true },
            cancellationToken: TestContext.Current.CancellationToken)).EnsureSuccessStatusCode();
        using var scope = fixture.Services.CreateScope();
        var allocationId = await scope.ServiceProvider.GetRequiredService<HrDatabaseContext>().LeaveAllocations
            .Where(x => x.EmployeeId == fixture.EmployeeId && x.Period == 2030).Select(x => x.Id)
            .SingleAsync(TestContext.Current.CancellationToken);
        using var response = await admin.DeleteAsync($"api/LeaveAllocations/{allocationId}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(7, await fixture.BalanceAsync());
    }

    [Fact]
    public async Task Migration_preserves_the_original_debit_when_cancelling_legacy_requests()
    {
        await fixture.ResetAsync();
        using (var scope = fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HrDatabaseContext>();
            var migrator = db.GetService<Microsoft.EntityFrameworkCore.Migrations.IMigrator>();
            await migrator.MigrateAsync("20261107162148_InitialCreate", TestContext.Current.CancellationToken);
            try
            {
                // This represents a request approved under the previous two-day counting rule.
                await db.Database.ExecuteSqlInterpolatedAsync($@"INSERT INTO LeaveRequests
                    (StartDate, EndDate, LeaveTypeId, DateRequested, Approved, Cancelled, RequestingEmployeeId)
                    VALUES ({new DateTime(2030, 1, 1)}, {new DateTime(2030, 1, 3)}, {1}, {DateTime.UtcNow}, {true}, {false}, {fixture.EmployeeId});
                    UPDATE LeaveAllocations SET NumberOfDays = {8} WHERE EmployeeId = {fixture.EmployeeId} AND Period = {2030}",
                    TestContext.Current.CancellationToken);
            }
            finally { await migrator.MigrateAsync(cancellationToken: TestContext.Current.CancellationToken); }
            var request = await db.LeaveRequests.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken);
            Assert.Equal(2, request.DeductedDays);
            using var employee = fixture.CreateClient(fixture.EmployeeToken);
            (await employee.PutAsync($"api/LeaveRequests/{request.Id}/cancel", null, TestContext.Current.CancellationToken)).EnsureSuccessStatusCode();
        }
        Assert.Equal(10, await fixture.BalanceAsync());
    }
}
