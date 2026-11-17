namespace HR.LeaveManagement.Domain;

// Instantané des volumes de l'application ; indépendant d'un utilisateur connecté.
public sealed class ApplicationStat
{
    public int Id { get; set; }
    public DateTime CapturedAtUtc { get; set; }
    public int LeaveAllocationCount { get; set; }
    public int LeaveRequestCount { get; set; }
    public int LeaveTypeCount { get; set; }

    // Null pour les anciens instantanés, où la répartition n'était pas mesurée.
    public int? PendingLeaveRequestCount { get; set; }
    public int? ApprovedLeaveRequestCount { get; set; }
    public int? CancelledLeaveRequestCount { get; set; }

    public decimal? PendingLeaveRequestAverageDays { get; set; }
    public decimal? ApprovedLeaveRequestAverageDays { get; set; }
    public decimal? CancelledLeaveRequestAverageDays { get; set; }

    public decimal? PendingLeaveRequestPercentage { get; set; }
    public decimal? ApprovedLeaveRequestPercentage { get; set; }
    public decimal? CancelledLeaveRequestPercentage { get; set; }
}
