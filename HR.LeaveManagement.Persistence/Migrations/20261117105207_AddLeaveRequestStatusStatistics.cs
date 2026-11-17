using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HR.LeaveManagement.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLeaveRequestStatusStatistics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ApprovedLeaveRequestAverageDays",
                table: "stats",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ApprovedLeaveRequestCount",
                table: "stats",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ApprovedLeaveRequestPercentage",
                table: "stats",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CancelledLeaveRequestAverageDays",
                table: "stats",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CancelledLeaveRequestCount",
                table: "stats",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CancelledLeaveRequestPercentage",
                table: "stats",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PendingLeaveRequestAverageDays",
                table: "stats",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PendingLeaveRequestCount",
                table: "stats",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PendingLeaveRequestPercentage",
                table: "stats",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApprovedLeaveRequestAverageDays",
                table: "stats");

            migrationBuilder.DropColumn(
                name: "ApprovedLeaveRequestCount",
                table: "stats");

            migrationBuilder.DropColumn(
                name: "ApprovedLeaveRequestPercentage",
                table: "stats");

            migrationBuilder.DropColumn(
                name: "CancelledLeaveRequestAverageDays",
                table: "stats");

            migrationBuilder.DropColumn(
                name: "CancelledLeaveRequestCount",
                table: "stats");

            migrationBuilder.DropColumn(
                name: "CancelledLeaveRequestPercentage",
                table: "stats");

            migrationBuilder.DropColumn(
                name: "PendingLeaveRequestAverageDays",
                table: "stats");

            migrationBuilder.DropColumn(
                name: "PendingLeaveRequestCount",
                table: "stats");

            migrationBuilder.DropColumn(
                name: "PendingLeaveRequestPercentage",
                table: "stats");
        }
    }
}
