using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HR.LeaveManagement.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddApplicationStats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "stats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CapturedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LeaveAllocationCount = table.Column<int>(type: "int", nullable: false),
                    LeaveRequestCount = table.Column<int>(type: "int", nullable: false),
                    LeaveTypeCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stats", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_stats_CapturedAtUtc",
                table: "stats",
                column: "CapturedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "stats");
        }
    }
}
