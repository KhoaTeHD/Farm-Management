using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Farm_Management.Migrations
{
    /// <inheritdoc />
    public partial class AddMonthlyPayroll : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PayrollEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PayrollRunId = table.Column<int>(type: "int", nullable: true),
                    WorkerId = table.Column<int>(type: "int", nullable: true),
                    WorkerCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    WorkerName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DailySalary = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    FullDays = table.Column<int>(type: "int", nullable: true),
                    HalfDays = table.Column<int>(type: "int", nullable: true),
                    OddHours = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: true),
                    TotalHours = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: true),
                    AttendanceCount = table.Column<int>(type: "int", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    AttendanceJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PayrollRuns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Year = table.Column<int>(type: "int", nullable: true),
                    Month = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    FinalizedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReopenedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    WorkerCount = table.Column<int>(type: "int", nullable: true),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    AttendanceHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollRuns", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollEntries_PayrollRunId_WorkerId",
                table: "PayrollEntries",
                columns: new[] { "PayrollRunId", "WorkerId" });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollEntries_WorkerId",
                table: "PayrollEntries",
                column: "WorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRuns_Year_Month_Status",
                table: "PayrollRuns",
                columns: new[] { "Year", "Month", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM [PayrollRuns]) OR EXISTS (SELECT 1 FROM [PayrollEntries])
                    THROW 50003, N'Rollback requires explicitly preserving or relocating payroll data first.', 1;
                """);
            migrationBuilder.DropTable(
                name: "PayrollEntries");

            migrationBuilder.DropTable(
                name: "PayrollRuns");
        }
    }
}
