using HR.LeaveManagement.Application.Exceptions;

namespace HR.LeaveManagement.Application.Services;

public static class LeaveDuration
{
    public static int GetDays(DateTime start, DateTime end)
    {
        if (end.Date < start.Date)
            throw new BadRequestException("End date must be on or after start date.");
        if (start.Year != end.Year)
            throw new BadRequestException("A leave request must stay within one calendar year.");
        return (end.Date - start.Date).Days + 1;
    }
}
