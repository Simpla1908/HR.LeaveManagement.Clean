using HR.LeaveManagement.Application.Contracts.Identity;
using HR.LeaveManagement.Application.Exceptions;

namespace HR.LeaveManagement.Application.Services;

public static class LeaveRequestAccess
{
    public static void EnsureAllowed(IUserService user, Domain.LeaveRequest request)
    {
        if (string.IsNullOrEmpty(user.UserId) ||
            (!user.IsAdministrator && request.RequestingEmployeeId != user.UserId))
            throw new ForbiddenException();
    }
}
