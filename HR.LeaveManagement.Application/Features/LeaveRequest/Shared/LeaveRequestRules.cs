using HR.LeaveManagement.Application.Contracts.Identity;
using HR.LeaveManagement.Application.Exceptions;

namespace HR.LeaveManagement.Application.Features.LeaveRequest.Shared;

public static class LeaveRequestRules
{
    public static int GetDays(DateTime start, DateTime end)
    {
        if (end.Date < start.Date)
        {
            throw new BadRequestException("End date must be on or after start date.");
        }

        if (start.Year != end.Year)
        {
            throw new BadRequestException("A leave request must stay within one calendar year.");
        }

        // Les deux dates sont incluses ; les heures ne changent pas le résultat.
        return (end.Date - start.Date).Days + 1;
    }

    public static void EnsureAllowed(IUserService user, Domain.LeaveRequest request)
    {
        if (string.IsNullOrEmpty(user.UserId))
        {
            throw new ForbiddenException();
        }

        if (!user.IsAdministrator && request.RequestingEmployeeId != user.UserId)
        {
            throw new ForbiddenException();
        }
    }
}
