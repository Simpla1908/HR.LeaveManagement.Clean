
using HR.LeaveManagement.Application.Models.Identity;

namespace HR.LeaveManagement.Application.Contracts.Identity;

public interface IUserService
{
    string? UserId { get; }
    bool IsAdministrator { get; }
    Task<List<Employee>> GetEmployees();
    Task<Employee?> GetEmployee(string userId);
}
