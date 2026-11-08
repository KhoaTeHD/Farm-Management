using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Farm_Management.Migrations
{
    /// <inheritdoc />
    public partial class SoftenDatabaseConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AttendanceLogs_Workers_WorkerId",
                table: "AttendanceLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_FertilizationLogs_Fertilizers_FertilizerId",
                table: "FertilizationLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_FertilizationLogs_Plants_PlantId",
                table: "FertilizationLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_FertilizationLogs_Workers_WorkerId",
                table: "FertilizationLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_HarvestBatches_Plants_PlantId",
                table: "HarvestBatches");

            migrationBuilder.DropForeignKey(
                name: "FK_HarvestBatches_Workers_WorkerId",
                table: "HarvestBatches");

            migrationBuilder.DropForeignKey(
                name: "FK_PesticideApplicationLogs_Pesticides_PesticideId",
                table: "PesticideApplicationLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_PesticideApplicationLogs_Plants_PlantId",
                table: "PesticideApplicationLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_PesticideApplicationLogs_Workers_WorkerId",
                table: "PesticideApplicationLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_Pesticides_PesticideRegistries_PesticideRegistryId",
                table: "Pesticides");

            migrationBuilder.DropForeignKey(
                name: "FK_Plants_Areas_AreaId",
                table: "Plants");

            migrationBuilder.DropForeignKey(
                name: "FK_Plants_PlantTypes_PlantTypeId",
                table: "Plants");

            migrationBuilder.DropForeignKey(
                name: "FK_Plants_Seeds_SeedId",
                table: "Plants");

            migrationBuilder.DropForeignKey(
                name: "FK_Seeds_PlantTypes_PlantTypeId",
                table: "Seeds");

            migrationBuilder.DropForeignKey(
                name: "FK_WaterIrrigationLogs_Plants_PlantId",
                table: "WaterIrrigationLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_WaterIrrigationLogs_WaterSources_WaterSourceId",
                table: "WaterIrrigationLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_WaterIrrigationLogs_Workers_WorkerId",
                table: "WaterIrrigationLogs");

            migrationBuilder.DropIndex(
                name: "IX_Workers_Code",
                table: "Workers");

            migrationBuilder.DropIndex(
                name: "IX_Plants_BatchCode",
                table: "Plants");

            migrationBuilder.DropIndex(
                name: "IX_HarvestBatches_TraceabilityCode",
                table: "HarvestBatches");

            migrationBuilder.DropIndex(
                name: "IX_AttendanceLogs_WorkerId_Date",
                table: "AttendanceLogs");

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "Workers",
                type: "bit",
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<string>(
                name: "FullName",
                table: "Workers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<decimal>(
                name: "DailySalary",
                table: "Workers",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Workers",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "Workers",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "WaterSources",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<bool>(
                name: "IsQualified",
                table: "WaterSources",
                type: "bit",
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "WaterSources",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<int>(
                name: "WorkerId",
                table: "WaterIrrigationLogs",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "WaterSourceId",
                table: "WaterIrrigationLogs",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "PlantId",
                table: "WaterIrrigationLogs",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<DateTime>(
                name: "IrrigationDate",
                table: "WaterIrrigationLogs",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "WaterIrrigationLogs",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<int>(
                name: "PlantTypeId",
                table: "Seeds",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "Origin",
                table: "Seeds",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Seeds",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<bool>(
                name: "IsCertified",
                table: "Seeds",
                type: "bit",
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Seeds",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "PlantTypes",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "PlantTypes",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Plants",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<int>(
                name: "SeedId",
                table: "Plants",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<DateTime>(
                name: "PlantingDate",
                table: "Plants",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<int>(
                name: "PlantTypeId",
                table: "Plants",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Plants",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<string>(
                name: "BatchCode",
                table: "Plants",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<int>(
                name: "AreaId",
                table: "Plants",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "Supplier",
                table: "Pesticides",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<decimal>(
                name: "StockQuantity",
                table: "Pesticides",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<DateTime>(
                name: "PurchaseDate",
                table: "Pesticides",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<int>(
                name: "PesticideRegistryId",
                table: "Pesticides",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ExpiryDate",
                table: "Pesticides",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Pesticides",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<string>(
                name: "BatchNumber",
                table: "Pesticides",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "TradeName",
                table: "PesticideRegistries",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "PesticideRegistries",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<int>(
                name: "PHI_Days",
                table: "PesticideRegistries",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "PesticideRegistries",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<string>(
                name: "ActiveIngredient",
                table: "PesticideRegistries",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<int>(
                name: "WorkerId",
                table: "PesticideApplicationLogs",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<DateTime>(
                name: "SafeHarvestDate",
                table: "PesticideApplicationLogs",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<int>(
                name: "PlantId",
                table: "PesticideApplicationLogs",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "PesticideId",
                table: "PesticideApplicationLogs",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "PHI_Days",
                table: "PesticideApplicationLogs",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "Dosage",
                table: "PesticideApplicationLogs",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "PesticideApplicationLogs",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ApplicationDate",
                table: "PesticideApplicationLogs",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<int>(
                name: "WorkerId",
                table: "HarvestBatches",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "TraceabilityCode",
                table: "HarvestBatches",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<int>(
                name: "PlantId",
                table: "HarvestBatches",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<bool>(
                name: "PHI_Compliant",
                table: "HarvestBatches",
                type: "bit",
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<DateTime>(
                name: "HarvestDate",
                table: "HarvestBatches",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "HarvestBatches",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<string>(
                name: "Supplier",
                table: "Fertilizers",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<decimal>(
                name: "StockQuantity",
                table: "Fertilizers",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Fertilizers",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Fertilizers",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<int>(
                name: "WorkerId",
                table: "FertilizationLogs",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "PlantId",
                table: "FertilizationLogs",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "FertilizerId",
                table: "FertilizationLogs",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "Dosage",
                table: "FertilizationLogs",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "FertilizationLogs",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ApplicationDate",
                table: "FertilizationLogs",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<int>(
                name: "WorkerId",
                table: "AttendanceLogs",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "WorkType",
                table: "AttendanceLogs",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<decimal>(
                name: "HoursWorked",
                table: "AttendanceLogs",
                type: "decimal(4,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(4,2)");

            migrationBuilder.AlterColumn<DateTime>(
                name: "Date",
                table: "AttendanceLogs",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "AttendanceLogs",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Areas",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Areas",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.CreateIndex(
                name: "IX_Workers_Code",
                table: "Workers",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_Plants_BatchCode",
                table: "Plants",
                column: "BatchCode");

            migrationBuilder.CreateIndex(
                name: "IX_HarvestBatches_TraceabilityCode",
                table: "HarvestBatches",
                column: "TraceabilityCode");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceLogs_WorkerId_Date",
                table: "AttendanceLogs",
                columns: new[] { "WorkerId", "Date" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Refuse an incompatible rollback rather than inventing values or deleting data.
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM [Workers] WHERE [IsActive] IS NULL OR [FullName] IS NULL OR [DailySalary] IS NULL OR [CreatedAt] IS NULL OR [Code] IS NULL)
                    THROW 50002, N'Rollback requires resolving NULL values in Workers first.', 1;
                IF EXISTS (SELECT 1 FROM [WaterSources] WHERE [Name] IS NULL OR [IsQualified] IS NULL OR [CreatedAt] IS NULL)
                    THROW 50002, N'Rollback requires resolving NULL values in WaterSources first.', 1;
                IF EXISTS (SELECT 1 FROM [WaterIrrigationLogs] WHERE [WorkerId] IS NULL OR [WaterSourceId] IS NULL OR [PlantId] IS NULL OR [IrrigationDate] IS NULL OR [CreatedAt] IS NULL)
                    THROW 50002, N'Rollback requires resolving NULL values in WaterIrrigationLogs first.', 1;
                IF EXISTS (SELECT 1 FROM [Seeds] WHERE [PlantTypeId] IS NULL OR [Origin] IS NULL OR [Name] IS NULL OR [IsCertified] IS NULL OR [CreatedAt] IS NULL)
                    THROW 50002, N'Rollback requires resolving NULL values in Seeds first.', 1;
                IF EXISTS (SELECT 1 FROM [PlantTypes] WHERE [Name] IS NULL OR [CreatedAt] IS NULL)
                    THROW 50002, N'Rollback requires resolving NULL values in PlantTypes first.', 1;
                IF EXISTS (SELECT 1 FROM [Plants] WHERE [Status] IS NULL OR [SeedId] IS NULL OR [PlantingDate] IS NULL OR [PlantTypeId] IS NULL OR [CreatedAt] IS NULL OR [BatchCode] IS NULL OR [AreaId] IS NULL)
                    THROW 50002, N'Rollback requires resolving NULL values in Plants first.', 1;
                IF EXISTS (SELECT 1 FROM [Pesticides] WHERE [Supplier] IS NULL OR [StockQuantity] IS NULL OR [PurchaseDate] IS NULL OR [PesticideRegistryId] IS NULL OR [ExpiryDate] IS NULL OR [CreatedAt] IS NULL OR [BatchNumber] IS NULL)
                    THROW 50002, N'Rollback requires resolving NULL values in Pesticides first.', 1;
                IF EXISTS (SELECT 1 FROM [PesticideRegistries] WHERE [TradeName] IS NULL OR [Status] IS NULL OR [PHI_Days] IS NULL OR [CreatedAt] IS NULL OR [ActiveIngredient] IS NULL)
                    THROW 50002, N'Rollback requires resolving NULL values in PesticideRegistries first.', 1;
                IF EXISTS (SELECT 1 FROM [PesticideApplicationLogs] WHERE [WorkerId] IS NULL OR [SafeHarvestDate] IS NULL OR [PlantId] IS NULL OR [PesticideId] IS NULL OR [PHI_Days] IS NULL OR [Dosage] IS NULL OR [CreatedAt] IS NULL OR [ApplicationDate] IS NULL)
                    THROW 50002, N'Rollback requires resolving NULL values in PesticideApplicationLogs first.', 1;
                IF EXISTS (SELECT 1 FROM [HarvestBatches] WHERE [WorkerId] IS NULL OR [TraceabilityCode] IS NULL OR [PlantId] IS NULL OR [PHI_Compliant] IS NULL OR [HarvestDate] IS NULL OR [CreatedAt] IS NULL)
                    THROW 50002, N'Rollback requires resolving NULL values in HarvestBatches first.', 1;
                IF EXISTS (SELECT 1 FROM [Fertilizers] WHERE [Supplier] IS NULL OR [StockQuantity] IS NULL OR [Name] IS NULL OR [CreatedAt] IS NULL)
                    THROW 50002, N'Rollback requires resolving NULL values in Fertilizers first.', 1;
                IF EXISTS (SELECT 1 FROM [FertilizationLogs] WHERE [WorkerId] IS NULL OR [PlantId] IS NULL OR [FertilizerId] IS NULL OR [Dosage] IS NULL OR [CreatedAt] IS NULL OR [ApplicationDate] IS NULL)
                    THROW 50002, N'Rollback requires resolving NULL values in FertilizationLogs first.', 1;
                IF EXISTS (SELECT 1 FROM [AttendanceLogs] WHERE [WorkerId] IS NULL OR [WorkType] IS NULL OR [HoursWorked] IS NULL OR [Date] IS NULL OR [CreatedAt] IS NULL)
                    THROW 50002, N'Rollback requires resolving NULL values in AttendanceLogs first.', 1;
                IF EXISTS (SELECT 1 FROM [Areas] WHERE [Name] IS NULL OR [CreatedAt] IS NULL)
                    THROW 50002, N'Rollback requires resolving NULL values in Areas first.', 1;
                IF EXISTS (SELECT 1 FROM [Workers] GROUP BY [Code] HAVING COUNT(*) > 1)
                    THROW 50002, N'Rollback requires resolving duplicates in Workers first.', 1;
                IF EXISTS (SELECT 1 FROM [Plants] GROUP BY [BatchCode] HAVING COUNT(*) > 1)
                    THROW 50002, N'Rollback requires resolving duplicates in Plants first.', 1;
                IF EXISTS (SELECT 1 FROM [HarvestBatches] GROUP BY [TraceabilityCode] HAVING COUNT(*) > 1)
                    THROW 50002, N'Rollback requires resolving duplicates in HarvestBatches first.', 1;
                IF EXISTS (SELECT 1 FROM [AttendanceLogs] GROUP BY [WorkerId], [Date] HAVING COUNT(*) > 1)
                    THROW 50002, N'Rollback requires resolving duplicates in AttendanceLogs first.', 1;
                IF EXISTS (SELECT 1 FROM [AttendanceLogs] child LEFT JOIN [Workers] parent ON child.[WorkerId] = parent.[Id] WHERE parent.[Id] IS NULL)
                    THROW 50002, N'Rollback requires resolving orphan references in AttendanceLogs.WorkerId first.', 1;
                IF EXISTS (SELECT 1 FROM [FertilizationLogs] child LEFT JOIN [Fertilizers] parent ON child.[FertilizerId] = parent.[Id] WHERE parent.[Id] IS NULL)
                    THROW 50002, N'Rollback requires resolving orphan references in FertilizationLogs.FertilizerId first.', 1;
                IF EXISTS (SELECT 1 FROM [FertilizationLogs] child LEFT JOIN [Plants] parent ON child.[PlantId] = parent.[Id] WHERE parent.[Id] IS NULL)
                    THROW 50002, N'Rollback requires resolving orphan references in FertilizationLogs.PlantId first.', 1;
                IF EXISTS (SELECT 1 FROM [FertilizationLogs] child LEFT JOIN [Workers] parent ON child.[WorkerId] = parent.[Id] WHERE parent.[Id] IS NULL)
                    THROW 50002, N'Rollback requires resolving orphan references in FertilizationLogs.WorkerId first.', 1;
                IF EXISTS (SELECT 1 FROM [HarvestBatches] child LEFT JOIN [Plants] parent ON child.[PlantId] = parent.[Id] WHERE parent.[Id] IS NULL)
                    THROW 50002, N'Rollback requires resolving orphan references in HarvestBatches.PlantId first.', 1;
                IF EXISTS (SELECT 1 FROM [HarvestBatches] child LEFT JOIN [Workers] parent ON child.[WorkerId] = parent.[Id] WHERE parent.[Id] IS NULL)
                    THROW 50002, N'Rollback requires resolving orphan references in HarvestBatches.WorkerId first.', 1;
                IF EXISTS (SELECT 1 FROM [PesticideApplicationLogs] child LEFT JOIN [Pesticides] parent ON child.[PesticideId] = parent.[Id] WHERE parent.[Id] IS NULL)
                    THROW 50002, N'Rollback requires resolving orphan references in PesticideApplicationLogs.PesticideId first.', 1;
                IF EXISTS (SELECT 1 FROM [PesticideApplicationLogs] child LEFT JOIN [Plants] parent ON child.[PlantId] = parent.[Id] WHERE parent.[Id] IS NULL)
                    THROW 50002, N'Rollback requires resolving orphan references in PesticideApplicationLogs.PlantId first.', 1;
                IF EXISTS (SELECT 1 FROM [PesticideApplicationLogs] child LEFT JOIN [Workers] parent ON child.[WorkerId] = parent.[Id] WHERE parent.[Id] IS NULL)
                    THROW 50002, N'Rollback requires resolving orphan references in PesticideApplicationLogs.WorkerId first.', 1;
                IF EXISTS (SELECT 1 FROM [Pesticides] child LEFT JOIN [PesticideRegistries] parent ON child.[PesticideRegistryId] = parent.[Id] WHERE parent.[Id] IS NULL)
                    THROW 50002, N'Rollback requires resolving orphan references in Pesticides.PesticideRegistryId first.', 1;
                IF EXISTS (SELECT 1 FROM [Plants] child LEFT JOIN [Areas] parent ON child.[AreaId] = parent.[Id] WHERE parent.[Id] IS NULL)
                    THROW 50002, N'Rollback requires resolving orphan references in Plants.AreaId first.', 1;
                IF EXISTS (SELECT 1 FROM [Plants] child LEFT JOIN [PlantTypes] parent ON child.[PlantTypeId] = parent.[Id] WHERE parent.[Id] IS NULL)
                    THROW 50002, N'Rollback requires resolving orphan references in Plants.PlantTypeId first.', 1;
                IF EXISTS (SELECT 1 FROM [Plants] child LEFT JOIN [Seeds] parent ON child.[SeedId] = parent.[Id] WHERE parent.[Id] IS NULL)
                    THROW 50002, N'Rollback requires resolving orphan references in Plants.SeedId first.', 1;
                IF EXISTS (SELECT 1 FROM [Seeds] child LEFT JOIN [PlantTypes] parent ON child.[PlantTypeId] = parent.[Id] WHERE parent.[Id] IS NULL)
                    THROW 50002, N'Rollback requires resolving orphan references in Seeds.PlantTypeId first.', 1;
                IF EXISTS (SELECT 1 FROM [WaterIrrigationLogs] child LEFT JOIN [Plants] parent ON child.[PlantId] = parent.[Id] WHERE parent.[Id] IS NULL)
                    THROW 50002, N'Rollback requires resolving orphan references in WaterIrrigationLogs.PlantId first.', 1;
                IF EXISTS (SELECT 1 FROM [WaterIrrigationLogs] child LEFT JOIN [WaterSources] parent ON child.[WaterSourceId] = parent.[Id] WHERE parent.[Id] IS NULL)
                    THROW 50002, N'Rollback requires resolving orphan references in WaterIrrigationLogs.WaterSourceId first.', 1;
                IF EXISTS (SELECT 1 FROM [WaterIrrigationLogs] child LEFT JOIN [Workers] parent ON child.[WorkerId] = parent.[Id] WHERE parent.[Id] IS NULL)
                    THROW 50002, N'Rollback requires resolving orphan references in WaterIrrigationLogs.WorkerId first.', 1;
                """);

            migrationBuilder.DropIndex(
                name: "IX_Workers_Code",
                table: "Workers");

            migrationBuilder.DropIndex(
                name: "IX_Plants_BatchCode",
                table: "Plants");

            migrationBuilder.DropIndex(
                name: "IX_HarvestBatches_TraceabilityCode",
                table: "HarvestBatches");

            migrationBuilder.DropIndex(
                name: "IX_AttendanceLogs_WorkerId_Date",
                table: "AttendanceLogs");

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "Workers",
                type: "bit",
                nullable: false,

                oldClrType: typeof(bool),
                oldType: "bit",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FullName",
                table: "Workers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,

                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "DailySalary",
                table: "Workers",
                type: "decimal(18,2)",
                nullable: false,

                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Workers",
                type: "datetime2",
                nullable: false,

                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "Workers",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,

                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "WaterSources",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,

                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "IsQualified",
                table: "WaterSources",
                type: "bit",
                nullable: false,

                oldClrType: typeof(bool),
                oldType: "bit",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "WaterSources",
                type: "datetime2",
                nullable: false,

                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "WorkerId",
                table: "WaterIrrigationLogs",
                type: "int",
                nullable: false,

                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "WaterSourceId",
                table: "WaterIrrigationLogs",
                type: "int",
                nullable: false,

                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "PlantId",
                table: "WaterIrrigationLogs",
                type: "int",
                nullable: false,

                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "IrrigationDate",
                table: "WaterIrrigationLogs",
                type: "datetime2",
                nullable: false,

                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "WaterIrrigationLogs",
                type: "datetime2",
                nullable: false,

                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "PlantTypeId",
                table: "Seeds",
                type: "int",
                nullable: false,

                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Origin",
                table: "Seeds",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,

                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Seeds",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,

                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "IsCertified",
                table: "Seeds",
                type: "bit",
                nullable: false,

                oldClrType: typeof(bool),
                oldType: "bit",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Seeds",
                type: "datetime2",
                nullable: false,

                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "PlantTypes",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,

                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "PlantTypes",
                type: "datetime2",
                nullable: false,

                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Plants",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,

                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "SeedId",
                table: "Plants",
                type: "int",
                nullable: false,

                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "PlantingDate",
                table: "Plants",
                type: "datetime2",
                nullable: false,

                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "PlantTypeId",
                table: "Plants",
                type: "int",
                nullable: false,

                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Plants",
                type: "datetime2",
                nullable: false,

                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "BatchCode",
                table: "Plants",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,

                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "AreaId",
                table: "Plants",
                type: "int",
                nullable: false,

                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Supplier",
                table: "Pesticides",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,

                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "StockQuantity",
                table: "Pesticides",
                type: "decimal(18,2)",
                nullable: false,

                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "PurchaseDate",
                table: "Pesticides",
                type: "datetime2",
                nullable: false,

                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "PesticideRegistryId",
                table: "Pesticides",
                type: "int",
                nullable: false,

                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "ExpiryDate",
                table: "Pesticides",
                type: "datetime2",
                nullable: false,

                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Pesticides",
                type: "datetime2",
                nullable: false,

                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "BatchNumber",
                table: "Pesticides",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,

                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TradeName",
                table: "PesticideRegistries",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,

                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "PesticideRegistries",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,

                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "PHI_Days",
                table: "PesticideRegistries",
                type: "int",
                nullable: false,

                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "PesticideRegistries",
                type: "datetime2",
                nullable: false,

                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ActiveIngredient",
                table: "PesticideRegistries",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,

                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "WorkerId",
                table: "PesticideApplicationLogs",
                type: "int",
                nullable: false,

                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "SafeHarvestDate",
                table: "PesticideApplicationLogs",
                type: "datetime2",
                nullable: false,

                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "PlantId",
                table: "PesticideApplicationLogs",
                type: "int",
                nullable: false,

                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "PesticideId",
                table: "PesticideApplicationLogs",
                type: "int",
                nullable: false,

                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "PHI_Days",
                table: "PesticideApplicationLogs",
                type: "int",
                nullable: false,

                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Dosage",
                table: "PesticideApplicationLogs",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,

                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "PesticideApplicationLogs",
                type: "datetime2",
                nullable: false,

                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "ApplicationDate",
                table: "PesticideApplicationLogs",
                type: "datetime2",
                nullable: false,

                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "WorkerId",
                table: "HarvestBatches",
                type: "int",
                nullable: false,

                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TraceabilityCode",
                table: "HarvestBatches",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,

                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "PlantId",
                table: "HarvestBatches",
                type: "int",
                nullable: false,

                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "PHI_Compliant",
                table: "HarvestBatches",
                type: "bit",
                nullable: false,

                oldClrType: typeof(bool),
                oldType: "bit",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "HarvestDate",
                table: "HarvestBatches",
                type: "datetime2",
                nullable: false,

                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "HarvestBatches",
                type: "datetime2",
                nullable: false,

                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Supplier",
                table: "Fertilizers",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,

                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "StockQuantity",
                table: "Fertilizers",
                type: "decimal(18,2)",
                nullable: false,

                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Fertilizers",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,

                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Fertilizers",
                type: "datetime2",
                nullable: false,

                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "WorkerId",
                table: "FertilizationLogs",
                type: "int",
                nullable: false,

                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "PlantId",
                table: "FertilizationLogs",
                type: "int",
                nullable: false,

                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "FertilizerId",
                table: "FertilizationLogs",
                type: "int",
                nullable: false,

                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Dosage",
                table: "FertilizationLogs",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,

                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "FertilizationLogs",
                type: "datetime2",
                nullable: false,

                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "ApplicationDate",
                table: "FertilizationLogs",
                type: "datetime2",
                nullable: false,

                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "WorkerId",
                table: "AttendanceLogs",
                type: "int",
                nullable: false,

                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "WorkType",
                table: "AttendanceLogs",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,

                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "HoursWorked",
                table: "AttendanceLogs",
                type: "decimal(4,2)",
                nullable: false,

                oldClrType: typeof(decimal),
                oldType: "decimal(4,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "Date",
                table: "AttendanceLogs",
                type: "datetime2",
                nullable: false,

                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "AttendanceLogs",
                type: "datetime2",
                nullable: false,

                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Areas",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,

                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Areas",
                type: "datetime2",
                nullable: false,

                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Workers_Code",
                table: "Workers",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Plants_BatchCode",
                table: "Plants",
                column: "BatchCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HarvestBatches_TraceabilityCode",
                table: "HarvestBatches",
                column: "TraceabilityCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceLogs_WorkerId_Date",
                table: "AttendanceLogs",
                columns: new[] { "WorkerId", "Date" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceLogs_Workers_WorkerId",
                table: "AttendanceLogs",
                column: "WorkerId",
                principalTable: "Workers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FertilizationLogs_Fertilizers_FertilizerId",
                table: "FertilizationLogs",
                column: "FertilizerId",
                principalTable: "Fertilizers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FertilizationLogs_Plants_PlantId",
                table: "FertilizationLogs",
                column: "PlantId",
                principalTable: "Plants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FertilizationLogs_Workers_WorkerId",
                table: "FertilizationLogs",
                column: "WorkerId",
                principalTable: "Workers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_HarvestBatches_Plants_PlantId",
                table: "HarvestBatches",
                column: "PlantId",
                principalTable: "Plants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_HarvestBatches_Workers_WorkerId",
                table: "HarvestBatches",
                column: "WorkerId",
                principalTable: "Workers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PesticideApplicationLogs_Pesticides_PesticideId",
                table: "PesticideApplicationLogs",
                column: "PesticideId",
                principalTable: "Pesticides",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PesticideApplicationLogs_Plants_PlantId",
                table: "PesticideApplicationLogs",
                column: "PlantId",
                principalTable: "Plants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PesticideApplicationLogs_Workers_WorkerId",
                table: "PesticideApplicationLogs",
                column: "WorkerId",
                principalTable: "Workers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Pesticides_PesticideRegistries_PesticideRegistryId",
                table: "Pesticides",
                column: "PesticideRegistryId",
                principalTable: "PesticideRegistries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Plants_Areas_AreaId",
                table: "Plants",
                column: "AreaId",
                principalTable: "Areas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Plants_PlantTypes_PlantTypeId",
                table: "Plants",
                column: "PlantTypeId",
                principalTable: "PlantTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Plants_Seeds_SeedId",
                table: "Plants",
                column: "SeedId",
                principalTable: "Seeds",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Seeds_PlantTypes_PlantTypeId",
                table: "Seeds",
                column: "PlantTypeId",
                principalTable: "PlantTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WaterIrrigationLogs_Plants_PlantId",
                table: "WaterIrrigationLogs",
                column: "PlantId",
                principalTable: "Plants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WaterIrrigationLogs_WaterSources_WaterSourceId",
                table: "WaterIrrigationLogs",
                column: "WaterSourceId",
                principalTable: "WaterSources",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WaterIrrigationLogs_Workers_WorkerId",
                table: "WaterIrrigationLogs",
                column: "WorkerId",
                principalTable: "Workers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
