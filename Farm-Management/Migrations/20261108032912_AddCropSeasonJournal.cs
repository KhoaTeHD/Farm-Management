using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Farm_Management.Migrations
{
    /// <inheritdoc />
    public partial class AddCropSeasonJournal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CareProtocol",
                table: "Plants",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NextSeasonChanges",
                table: "Plants",
                type: "nvarchar(3000)",
                maxLength: 3000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SeasonName",
                table: "Plants",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SeasonReview",
                table: "Plants",
                type: "nvarchar(3000)",
                maxLength: 3000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CropCareSteps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlantId = table.Column<int>(type: "int", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Kind = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    PlannedDay = table.Column<int>(type: "int", nullable: true),
                    Instructions = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CropCareSteps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CropJournalEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlantId = table.Column<int>(type: "int", nullable: true),
                    CareStepId = table.Column<int>(type: "int", nullable: true),
                    RecordCode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Kind = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Recorder = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Summary = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Material = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Unit = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Method = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Weather = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    WithdrawalDays = table.Column<int>(type: "int", nullable: true),
                    GrowthStage = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Condition = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    HeightCm = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    SurvivalPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    AffectedPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    Quality = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PhotoPath = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CropJournalEntries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CropCareSteps_PlantId_PlannedDay",
                table: "CropCareSteps",
                columns: new[] { "PlantId", "PlannedDay" });

            migrationBuilder.CreateIndex(
                name: "IX_CropJournalEntries_CareStepId",
                table: "CropJournalEntries",
                column: "CareStepId");

            migrationBuilder.CreateIndex(
                name: "IX_CropJournalEntries_PlantId_Date",
                table: "CropJournalEntries",
                columns: new[] { "PlantId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_CropJournalEntries_RecordCode",
                table: "CropJournalEntries",
                column: "RecordCode");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM CropJournalEntries)
                   OR EXISTS (SELECT 1 FROM CropCareSteps)
                   OR EXISTS (SELECT 1 FROM Plants WHERE SeasonName IS NOT NULL OR CareProtocol IS NOT NULL OR SeasonReview IS NOT NULL OR NextSeasonChanges IS NOT NULL)
                    THROW 50005, 'Cannot remove season journals while their data exists.', 1;
                """);
            migrationBuilder.DropTable(
                name: "CropCareSteps");

            migrationBuilder.DropTable(
                name: "CropJournalEntries");

            migrationBuilder.DropColumn(
                name: "CareProtocol",
                table: "Plants");

            migrationBuilder.DropColumn(
                name: "NextSeasonChanges",
                table: "Plants");

            migrationBuilder.DropColumn(
                name: "SeasonName",
                table: "Plants");

            migrationBuilder.DropColumn(
                name: "SeasonReview",
                table: "Plants");
        }
    }
}
