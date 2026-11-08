using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Farm_Management.Migrations
{
    /// <inheritdoc />
    public partial class AddCultivationRecordsAndTraceability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Seeds",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InputLotId",
                table: "Seeds",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Seeds",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Revision",
                table: "Seeds",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "PlantTypes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "PlantTypes",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Revision",
                table: "PlantTypes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Plants",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Plants",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Revision",
                table: "Plants",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "CropJournalEntries",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InputLotId",
                table: "CropJournalEntries",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "CropJournalEntries",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Revision",
                table: "CropJournalEntries",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WaterResourceId",
                table: "CropJournalEntries",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "CropCareSteps",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "CropCareSteps",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Revision",
                table: "CropCareSteps",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Areas",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Areas",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Revision",
                table: "Areas",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CultivationAudits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RecordType = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    RecordId = table.Column<int>(type: "int", nullable: true),
                    PlantId = table.Column<int>(type: "int", nullable: true),
                    Action = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Actor = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    BeforeJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AfterJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CultivationAudits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FieldResources",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Revision = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AreaId = table.Column<int>(type: "int", nullable: true),
                    Kind = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Location = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    RiskAssessment = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Treatment = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FieldResources", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HarvestTraces",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Revision = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PlantId = table.Column<int>(type: "int", nullable: true),
                    JournalEntryId = table.Column<int>(type: "int", nullable: true),
                    LegacyHarvestId = table.Column<int>(type: "int", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    HarvestDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    WeightKg = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Quality = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SourceHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    IsVoided = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HarvestTraces", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InputLots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Revision = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Kind = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ProductName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ManufacturerBatch = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Supplier = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    InvoiceNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ReceivedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Unit = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    RegistrationNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InputLots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InspectionReports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Revision = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TargetType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    TargetId = table.Column<int>(type: "int", nullable: true),
                    ReportNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Laboratory = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SampleDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReportDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Indicator = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Result = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    LimitReference = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Conclusion = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    EvidencePath = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InspectionReports", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Seeds_InputLotId",
                table: "Seeds",
                column: "InputLotId");

            migrationBuilder.CreateIndex(
                name: "IX_CropJournalEntries_InputLotId",
                table: "CropJournalEntries",
                column: "InputLotId");

            migrationBuilder.CreateIndex(
                name: "IX_CropJournalEntries_WaterResourceId",
                table: "CropJournalEntries",
                column: "WaterResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_CultivationAudits_PlantId_OccurredAt",
                table: "CultivationAudits",
                columns: new[] { "PlantId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CultivationAudits_RecordType_RecordId",
                table: "CultivationAudits",
                columns: new[] { "RecordType", "RecordId" });

            migrationBuilder.CreateIndex(
                name: "IX_FieldResources_AreaId",
                table: "FieldResources",
                column: "AreaId");

            migrationBuilder.CreateIndex(
                name: "IX_HarvestTraces_Code",
                table: "HarvestTraces",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_HarvestTraces_JournalEntryId",
                table: "HarvestTraces",
                column: "JournalEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_HarvestTraces_LegacyHarvestId",
                table: "HarvestTraces",
                column: "LegacyHarvestId");

            migrationBuilder.CreateIndex(
                name: "IX_InputLots_Code",
                table: "InputLots",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_InspectionReports_TargetType_TargetId",
                table: "InspectionReports",
                columns: new[] { "TargetType", "TargetId" });
            // Baseline only: older changes cannot be reconstructed or attributed.
            foreach (var pair in new[] { ("Areas", "Area"), ("PlantTypes", "PlantType"), ("Seeds", "Seed"), ("Plants", "Plant"), ("CropCareSteps", "CropCareStep"), ("CropJournalEntries", "CropJournalEntry") })
            {
                var plant = pair.Item2 == "Plant" ? "e.Id" : pair.Item2 is "CropCareStep" or "CropJournalEntry" ? "e.PlantId" : "NULL";
                migrationBuilder.Sql($"INSERT INTO CultivationAudits (RecordType, RecordId, PlantId, Action, OccurredAt, Reason, AfterJson) SELECT N'{pair.Item2}', e.Id, {plant}, N'Baseline', GETUTCDATE(), N'Hồ sơ có trước tính năng lịch sử; chưa biết các lần sửa trước đó.', (SELECT e.* FOR JSON PATH, INCLUDE_NULL_VALUES, WITHOUT_ARRAY_WRAPPER) FROM [{pair.Item1}] e;");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM CultivationAudits) OR EXISTS (SELECT 1 FROM HarvestTraces)
                   OR EXISTS (SELECT 1 FROM InputLots) OR EXISTS (SELECT 1 FROM FieldResources) OR EXISTS (SELECT 1 FROM InspectionReports)
                    THROW 50006, 'Cannot remove retained cultivation history or provenance data.', 1;
                """);
            migrationBuilder.DropTable(
                name: "CultivationAudits");

            migrationBuilder.DropTable(
                name: "FieldResources");

            migrationBuilder.DropTable(
                name: "HarvestTraces");

            migrationBuilder.DropTable(
                name: "InputLots");

            migrationBuilder.DropTable(
                name: "InspectionReports");

            migrationBuilder.DropIndex(
                name: "IX_Seeds_InputLotId",
                table: "Seeds");

            migrationBuilder.DropIndex(
                name: "IX_CropJournalEntries_InputLotId",
                table: "CropJournalEntries");

            migrationBuilder.DropIndex(
                name: "IX_CropJournalEntries_WaterResourceId",
                table: "CropJournalEntries");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Seeds");

            migrationBuilder.DropColumn(
                name: "InputLotId",
                table: "Seeds");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Seeds");

            migrationBuilder.DropColumn(
                name: "Revision",
                table: "Seeds");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "PlantTypes");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "PlantTypes");

            migrationBuilder.DropColumn(
                name: "Revision",
                table: "PlantTypes");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Plants");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Plants");

            migrationBuilder.DropColumn(
                name: "Revision",
                table: "Plants");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "CropJournalEntries");

            migrationBuilder.DropColumn(
                name: "InputLotId",
                table: "CropJournalEntries");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "CropJournalEntries");

            migrationBuilder.DropColumn(
                name: "Revision",
                table: "CropJournalEntries");

            migrationBuilder.DropColumn(
                name: "WaterResourceId",
                table: "CropJournalEntries");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "CropCareSteps");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "CropCareSteps");

            migrationBuilder.DropColumn(
                name: "Revision",
                table: "CropCareSteps");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Areas");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Areas");

            migrationBuilder.DropColumn(
                name: "Revision",
                table: "Areas");
        }
    }
}
