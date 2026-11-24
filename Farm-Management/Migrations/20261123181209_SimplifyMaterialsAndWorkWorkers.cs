using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Farm_Management.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyMaterialsAndWorkWorkers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Unit",
                table: "CropJournalEntries",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(10)",
                oldMaxLength: 10,
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaterialItemId",
                table: "CropJournalEntries",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CropJournalWorkers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CropJournalEntryId = table.Column<int>(type: "int", nullable: true),
                    WorkerId = table.Column<int>(type: "int", nullable: true),
                    WorkerName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CropJournalWorkers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MaterialItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Revision = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Category = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    DefaultUnit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialItems", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CropJournalEntries_MaterialItemId",
                table: "CropJournalEntries",
                column: "MaterialItemId");

            migrationBuilder.CreateIndex(
                name: "IX_CropJournalWorkers_CropJournalEntryId",
                table: "CropJournalWorkers",
                column: "CropJournalEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_CropJournalWorkers_CropJournalEntryId_WorkerId",
                table: "CropJournalWorkers",
                columns: new[] { "CropJournalEntryId", "WorkerId" });

            migrationBuilder.CreateIndex(
                name: "IX_CropJournalWorkers_WorkerId",
                table: "CropJournalWorkers",
                column: "WorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialItems_Category",
                table: "MaterialItems",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialItems_Name",
                table: "MaterialItems",
                column: "Name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM [MaterialItems])
                    OR EXISTS (SELECT 1 FROM [CropJournalWorkers])
                    OR EXISTS (SELECT 1 FROM [CropJournalEntries] WHERE [MaterialItemId] IS NOT NULL OR LEN([Unit]) > 10)
                    THROW 50008, N'Không thể rollback khi đã có dữ liệu vật liệu hoặc phân công công nhân. Sao lưu và xử lý dữ liệu trước.', 1;
                """);

            migrationBuilder.DropTable(
                name: "CropJournalWorkers");

            migrationBuilder.DropTable(
                name: "MaterialItems");

            migrationBuilder.DropIndex(
                name: "IX_CropJournalEntries_MaterialItemId",
                table: "CropJournalEntries");

            migrationBuilder.DropColumn(
                name: "MaterialItemId",
                table: "CropJournalEntries");

            migrationBuilder.AlterColumn<string>(
                name: "Unit",
                table: "CropJournalEntries",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);
        }
    }
}
