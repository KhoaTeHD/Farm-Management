using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Farm_Management.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkerAdvances : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdvanceHash",
                table: "PayrollRuns",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CalculationVersion",
                table: "PayrollRuns",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "NetTotal",
                table: "PayrollRuns",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalAdvances",
                table: "PayrollRuns",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AdvanceAmount",
                table: "PayrollEntries",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdvancesJson",
                table: "PayrollEntries",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "NetAmount",
                table: "PayrollEntries",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "WorkerAdvances",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkerId = table.Column<int>(type: "int", nullable: true),
                    Reference = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PaidDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PayrollYear = table.Column<int>(type: "int", nullable: true),
                    PayrollMonth = table.Column<int>(type: "int", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkerAdvances", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Seeds_PlantTypeId_Name",
                table: "Seeds",
                columns: new[] { "PlantTypeId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_PlantTypes_Name",
                table: "PlantTypes",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_WorkerAdvances_PayrollYear_PayrollMonth_WorkerId",
                table: "WorkerAdvances",
                columns: new[] { "PayrollYear", "PayrollMonth", "WorkerId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkerAdvances_Reference",
                table: "WorkerAdvances",
                column: "Reference");

            migrationBuilder.CreateIndex(
                name: "IX_WorkerAdvances_WorkerId",
                table: "WorkerAdvances",
                column: "WorkerId");

            // Explicitly identify snapshots produced before advance deductions existed.
            // Leave their monetary columns and original JSON untouched.
            migrationBuilder.Sql("UPDATE PayrollRuns SET CalculationVersion = 1;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM WorkerAdvances)
                   OR EXISTS (SELECT 1 FROM PayrollRuns WHERE CalculationVersion = 2)
                   OR EXISTS (SELECT 1 FROM PayrollEntries WHERE AdvancesJson IS NOT NULL OR AdvanceAmount IS NOT NULL OR NetAmount IS NOT NULL)
                    THROW 50004, 'Cannot remove advance deductions while their data exists.', 1;
                """);
            migrationBuilder.DropTable(
                name: "WorkerAdvances");

            migrationBuilder.DropIndex(
                name: "IX_Seeds_PlantTypeId_Name",
                table: "Seeds");

            migrationBuilder.DropIndex(
                name: "IX_PlantTypes_Name",
                table: "PlantTypes");

            migrationBuilder.DropColumn(
                name: "AdvanceHash",
                table: "PayrollRuns");

            migrationBuilder.DropColumn(
                name: "CalculationVersion",
                table: "PayrollRuns");

            migrationBuilder.DropColumn(
                name: "NetTotal",
                table: "PayrollRuns");

            migrationBuilder.DropColumn(
                name: "TotalAdvances",
                table: "PayrollRuns");

            migrationBuilder.DropColumn(
                name: "AdvanceAmount",
                table: "PayrollEntries");

            migrationBuilder.DropColumn(
                name: "AdvancesJson",
                table: "PayrollEntries");

            migrationBuilder.DropColumn(
                name: "NetAmount",
                table: "PayrollEntries");
        }
    }
}
