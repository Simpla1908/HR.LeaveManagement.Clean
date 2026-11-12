using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace HR.LeaveManagement.Persistence.Migrations;

public partial class UniqueAnnualAllocations : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(name: "DeductedDays", table: "LeaveRequests", nullable: false, defaultValue: 0);
        // Existing approvals used a truncated date difference, excluding the final day.
        // Preserve their exact debit so future cancellation cannot invent an extra day.
        migrationBuilder.Sql(@"UPDATE LeaveRequests
            SET DeductedDays = CAST(DATEDIFF_BIG(SECOND, StartDate, EndDate) / 86400 AS int)
            WHERE Approved = 1 AND Cancelled = 0");
        // Fail rather than discard existing allocations if duplicates need manual reconciliation.
        migrationBuilder.DropIndex(name: "IX_LeaveAllocations_EmployeeId_LeaveTypeId_Period", table: "LeaveAllocations");
        migrationBuilder.CreateIndex(name: "IX_LeaveAllocations_EmployeeId_LeaveTypeId_Period", table: "LeaveAllocations",
            columns: new[] { "EmployeeId", "LeaveTypeId", "Period" }, unique: true);
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "DeductedDays", table: "LeaveRequests");
        migrationBuilder.DropIndex(name: "IX_LeaveAllocations_EmployeeId_LeaveTypeId_Period", table: "LeaveAllocations");
        migrationBuilder.CreateIndex(name: "IX_LeaveAllocations_EmployeeId_LeaveTypeId_Period", table: "LeaveAllocations",
            columns: new[] { "EmployeeId", "LeaveTypeId", "Period" });
    }
}
