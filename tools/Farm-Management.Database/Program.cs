using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Farm_Management.Data;

var root = new DirectoryInfo(AppContext.BaseDirectory);
while (root != null && !File.Exists(Path.Combine(root.FullName, "Farm-Management.sln"))) root = root.Parent;
if (root == null) throw new InvalidOperationException("Repository root not found.");
var config = new ConfigurationBuilder().SetBasePath(Path.Combine(root.FullName, "Farm-Management"))
    .AddJsonFile("appsettings.json").AddJsonFile("appsettings.Local.json", optional: true)
    .AddEnvironmentVariables().Build();
var connectionString = config.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("DefaultConnection must be configured locally or via environment.");
var settings = new SqlConnectionStringBuilder(connectionString);
if (settings.InitialCatalog != "FarmManagement") throw new InvalidOperationException("This utility only manages FarmManagement.");
var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connectionString).Options;
await using var db = new ApplicationDbContext(options);
var masterSettings = new SqlConnectionStringBuilder(connectionString) { InitialCatalog = "master" };
await using var master = new SqlConnection(masterSettings.ConnectionString);
await master.OpenAsync();
await using (var command = new SqlCommand("SELECT COUNT(*) FROM sys.databases WHERE name=@name", master))
{
    command.Parameters.AddWithValue("@name", settings.InitialCatalog);
    Console.WriteLine("Target database exists: " + ((int)(await command.ExecuteScalarAsync())! == 1));
}
if (args.Contains("--migrate"))
{
    Console.WriteLine("Applying pending migrations to FarmManagement...");
    await db.Database.MigrateAsync();
}
if (!await db.Database.CanConnectAsync()) { Console.WriteLine("Database not yet created."); return; }
Console.WriteLine("Applied migrations:");
foreach (var migration in await db.Database.GetAppliedMigrationsAsync()) Console.WriteLine("  " + migration);
Console.WriteLine("Pending migrations: " + (await db.Database.GetPendingMigrationsAsync()).Count());
await using var connection = new SqlConnection(connectionString);
await connection.OpenAsync();
var tables = db.Model.GetEntityTypes().Select(e => e.GetTableName()!).ToArray();
var counts = new Dictionary<string, long>();
foreach (var table in tables)
{
    await using var exists = new SqlCommand("SELECT OBJECT_ID(@table, 'U')", connection);
    exists.Parameters.AddWithValue("@table", table);
    if (await exists.ExecuteScalarAsync() is DBNull) { Console.WriteLine("Missing table: " + table); continue; }
    await using var command = new SqlCommand("SELECT COUNT_BIG(*) FROM " + "[" + table.Replace("]", "]]") + "]", connection);
    counts[table] = (long)(await command.ExecuteScalarAsync())!;
}
Console.WriteLine("Row counts: " + JsonSerializer.Serialize(counts));
await using (var command = new SqlCommand("""
    SELECT
      (SELECT COUNT(*) FROM sys.foreign_keys WHERE parent_object_id IN (SELECT object_id FROM sys.tables WHERE name <> '__EFMigrationsHistory')) AS ForeignKeys,
      (SELECT COUNT(*) FROM sys.indexes WHERE is_unique=1 AND is_primary_key=0 AND object_id IN (SELECT object_id FROM sys.tables WHERE name <> '__EFMigrationsHistory')) AS UniqueIndexes,
      (SELECT COUNT(*) FROM sys.columns WHERE is_nullable=0 AND name <> 'Id' AND object_id IN (SELECT object_id FROM sys.tables WHERE name <> '__EFMigrationsHistory')) AS RequiredBusinessColumns,
      (SELECT COUNT(*) FROM sys.check_constraints WHERE parent_object_id IN (SELECT object_id FROM sys.tables WHERE name <> '__EFMigrationsHistory')) AS CheckConstraints,
      (SELECT COUNT(*) FROM sys.triggers WHERE parent_id IN (SELECT object_id FROM sys.tables WHERE name <> '__EFMigrationsHistory')) AS TableTriggers;
    """, connection))
{
    await using var reader = await command.ExecuteReaderAsync();
    await reader.ReadAsync();
    Console.WriteLine($"Schema: foreign keys={reader.GetInt32(0)}, unique indexes={reader.GetInt32(1)}, required business columns={reader.GetInt32(2)}, checks={reader.GetInt32(3)}, triggers={reader.GetInt32(4)}");
    if (args.Contains("--verify") && Enumerable.Range(0, 5).Any(i => reader.GetInt32(i) != 0))
        throw new InvalidOperationException("Soft database rules are not satisfied.");
}
