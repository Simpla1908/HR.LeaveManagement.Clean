using HR.LeaveManagement.Application.Models.Identity;

namespace HR.LeaveManagement.Application.Contracts.Identity;

public interface IUserAdministrationService
{
    Task<List<UserSummary>> GetAllAsync(CancellationToken cancellationToken);
    Task<string> CreateAsync(CreateUserRequest request);
    Task UpdateAsync(string id, UserDetailsRequest request);
    Task DeleteAsync(string id);
}
