using HR.LeaveManagement.Application;
using HR.LeaveManagement.Application.Contracts.Identity;
using HR.LeaveManagement.Application.Exceptions;
using HR.LeaveManagement.Application.Models.Identity;
using HR.LeaveManagement.Application.Services;
using HR.LeaveManagement.Domain;
using Microsoft.Extensions.DependencyInjection;
using AutoMapper;

namespace HR.LeaveManagement.Application.UnitTests;

public class LeaveRulesTests
{
    [Theory]
    [InlineData(1, 1, 1)]
    [InlineData(1, 5, 5)]
    [InlineData(1, 31, 31)]
    public void Calendar_days_include_both_dates(int start, int end, int expected) =>
        Assert.Equal(expected, LeaveDuration.GetDays(new(2030, 1, start), new(2030, 1, end)));

    [Fact]
    public void Time_of_day_does_not_change_duration() =>
        Assert.Equal(2, LeaveDuration.GetDays(new(2030, 1, 1, 18, 0, 0), new(2030, 1, 2, 8, 0, 0)));

    [Fact]
    public void Reversed_dates_are_rejected() =>
        Assert.Throws<BadRequestException>(() => LeaveDuration.GetDays(new(2030, 1, 2), new(2030, 1, 1)));

    [Fact]
    public void Requests_spanning_years_are_rejected() =>
        Assert.Throws<BadRequestException>(() => LeaveDuration.GetDays(new(2030, 12, 31), new(2031, 1, 1)));

    [Theory]
    [InlineData("employee", false, true)]
    [InlineData("other", false, false)]
    [InlineData("admin", true, true)]
    [InlineData(null, true, false)]
    public void Access_requires_owner_or_authenticated_admin(string? userId, bool admin, bool allowed)
    {
        var user = new TestUser(userId, admin);
        var request = new LeaveRequest { RequestingEmployeeId = "employee" };
        if (allowed) LeaveRequestAccess.EnsureAllowed(user, request);
        else Assert.Throws<ForbiddenException>(() => LeaveRequestAccess.EnsureAllowed(user, request));
    }

    [Fact]
    public void All_registered_mapping_profiles_are_valid()
    {
        var services = new ServiceCollection().AddLogging().AddApplicationServices();
        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<IMapper>().ConfigurationProvider.AssertConfigurationIsValid();
    }

    private sealed class TestUser(string? id, bool admin) : IUserService
    {
        public string? UserId => id;
        public bool IsAdministrator => admin;
        public Task<Employee?> GetEmployee(string userId) => Task.FromResult<Employee?>(null);
        public Task<List<Employee>> GetEmployees() => Task.FromResult(new List<Employee>());
    }
}
