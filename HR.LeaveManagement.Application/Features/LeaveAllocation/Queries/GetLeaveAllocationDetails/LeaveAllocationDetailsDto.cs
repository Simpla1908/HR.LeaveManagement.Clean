using HR.LeaveManagement.Application.Features.LeaveType.Queries.GetAllLeaveTypes;

namespace HR.LeaveManagement.Application.Features.LeaveAllocation.Queries.GetLeaveAllocationDetails;

public class LeaveAllocationDetailsDto
{
    public int Id { get; set; }
    public string EmployeeId { get; set; } = string.Empty;
    public int NumberOfDays { get; set; }
    public LeaveTypeDto LeaveType { get; set; } = new();
    public int LeaveTypeId { get; set; }
    public int Period { get; set; }
}