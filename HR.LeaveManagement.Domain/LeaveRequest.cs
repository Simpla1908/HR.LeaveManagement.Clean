using System;
using System.Collections.Generic;
using HR.LeaveManagement.Domain.Common;


namespace HR.LeaveManagement.Domain;
public class LeaveRequest : BaseEntity
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public LeaveType? LeaveType { get; set; }
    public int LeaveTypeId { get; set; }
    public DateTime DateRequested { get; set; }
    public string? RequestComments { get; set; }
    public bool? Approved { get; set; }
    public bool Cancelled { get; set; }
    // Exact number of days currently debited, including requests made under older counting rules.
    public int DeductedDays { get; set; }
    public string RequestingEmployeeId { get; set; } = string.Empty;

}

