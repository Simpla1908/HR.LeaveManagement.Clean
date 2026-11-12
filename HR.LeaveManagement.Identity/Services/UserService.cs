
using System.Security.Claims;
using HR.LeaveManagement.Application.Contracts.Identity;
using HR.LeaveManagement.Application.Models.Identity;
using HR.LeaveManagement.Identity.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace HR.LeaveManagement.Identity.Services;

public class UserService : IUserService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IHttpContextAccessor _contextAccessor;

    public UserService(
        UserManager<ApplicationUser> userManager,
        IHttpContextAccessor contextAccessor)
    {
        _userManager = userManager;
        _contextAccessor = contextAccessor;
    }

    // Lire l'identifiant depuis les claims du JWT.
    public string? UserId =>
        _contextAccessor.HttpContext?.User
            .FindFirstValue("uid");

    public bool IsAdministrator =>
        _contextAccessor.HttpContext?.User.IsInRole("Administrator") == true;

    public async Task<Employee?> GetEmployee(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user is null)
            return null;

        return new Employee
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            Firstname = user.FirstName,
            Lastname = user.LastName
        };
    }

    public async Task<List<Employee>> GetEmployees()
    {
        var users = await _userManager.GetUsersInRoleAsync(
            "Employee");

        return users.Select(user => new Employee
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            Firstname = user.FirstName,
            Lastname = user.LastName
        }).ToList();
    }
}
