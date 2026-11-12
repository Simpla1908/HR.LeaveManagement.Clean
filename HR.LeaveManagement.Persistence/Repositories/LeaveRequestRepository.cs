
using HR.LeaveManagement.Application.Contracts.Persistence;
using HR.LeaveManagement.Domain;
using HR.LeaveManagement.Persistence.DatabaseContext;
using Microsoft.EntityFrameworkCore;

namespace HR.LeaveManagement.Persistence.Repositories;

public class LeaveRequestRepository
    : GenericRepository<LeaveRequest>,
      ILeaveRequestRepository
{
    public LeaveRequestRepository(
        HrDatabaseContext context)
        : base(context)
    {
    }

    public async Task<List<LeaveRequest>>
        GetLeaveRequestsWithDetails()
    {
        return await _context.LeaveRequests
            .Where(x => !string.IsNullOrEmpty(
                x.RequestingEmployeeId))
            .Include(x => x.LeaveType)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<LeaveRequest>>
        GetLeaveRequestsWithDetails(string userId)
    {
        return await _context.LeaveRequests
            .Where(x => x.RequestingEmployeeId == userId)
            .Include(x => x.LeaveType)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<LeaveRequest?>
        GetLeaveRequestWithDetails(int id)
    {
        return await _context.LeaveRequests
            .Include(x => x.LeaveType)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);
    }
}
