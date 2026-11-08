using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using Farm_Management.Data;
using Farm_Management.Models;
using Farm_Management.Models.ViewModels;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

var root = FindRoot();
var options = new DbContextOptionsBuilder<ApplicationDbContext>()
    .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=Unused;Integrated Security=true;TrustServerCertificate=true").Options;
using (var context = new ApplicationDbContext(options))
{
    var model = context.GetService<IDesignTimeModel>().Model;
    Check(model.GetEntityTypes().Count() == 24, "Exactly 24 persisted entities");
    foreach (var entity in model.GetEntityTypes())
    {
        Check(!entity.GetForeignKeys().Any(), $"No hard foreign keys: {entity.DisplayName()}");
        Check(!entity.GetIndexes().Any(i => i.IsUnique), $"No unique indexes: {entity.DisplayName()}");
        Check(entity.GetKeys().Count() == 1, $"Only the primary key remains: {entity.DisplayName()}");
        Check(entity.GetProperties().Where(p => !p.IsPrimaryKey()).All(p => p.IsNullable),
            $"All business columns nullable: {entity.DisplayName()}");
    }
    var migrations = context.GetService<IMigrationsAssembly>();
    var migration = migrations.CreateMigration(migrations.Migrations.Single(m => m.Key.EndsWith("_SoftenDatabaseConstraints")).Value, context.Database.ProviderName!);
    Check(migration.UpOperations.OfType<DropForeignKeyOperation>().Count() == 17, "Migration drops 17 foreign keys");
    Check(migration.UpOperations.OfType<AlterColumnOperation>().All(c => c.IsNullable), "Migration only relaxes column nullability");
    Check(!migration.UpOperations.Any(o => o is DropTableOperation or DropColumnOperation or DeleteDataOperation), "Migration preserves tables, columns and data");
    Check(!migration.UpOperations.OfType<CreateIndexOperation>().Any(i => i.IsUnique), "Replacement indexes are non-unique");
    var differ = context.GetService<IMigrationsModelDiffer>();
    Check(!differ.HasDifferences(context.GetService<IModelRuntimeInitializer>().Initialize(migrations.ModelSnapshot!.Model).GetRelationalModel(),
        model.GetRelationalModel()), "Snapshot matches the current model");
}

Check(!Valid(new WorkerForm { Code = " ", FullName = "A", DailySalary = 100, IsActive = true }), "Worker code is required");
Check(!Valid(new WorkerForm { Code = "A", FullName = "A", DailySalary = null }), "Salary is required at the form boundary");
Check(!Valid(new AttendanceLogForm { WorkerId = 1, Date = DateTime.Today, HoursWorked = 25 }), "Reject hours above 24");
Check(!Valid(new AttendanceLogForm { WorkerId = 1, Date = DateTime.Today }), "Reject missing hours");
Check(!Valid(new AttendanceLogForm { WorkerId = 1, Date = DateTime.Today, HoursWorked = 7.99m }), "Reject fractional hours outside the half-hour step");
Check(!Valid(new AreaForm { Name = "A", Acreage = -1 }), "Reject negative acreage");
Check(!Valid(new AreaForm { Name = "A", GpsCoordinates = "NaN, 108" }), "Reject invalid GPS coordinates");
Check(Valid(new AreaForm { Name = "A", GpsCoordinates = "10.5, 106.5" }), "Accept valid coordinates");
Check(AttendanceRules.Classify(null) == null && AttendanceRules.Classify(25) == null, "Unknown/invalid hours are not work days");
Check(AttendanceRules.Classify(4) == "HalfDay" && AttendanceRules.Classify(8) == "FullDay"
    && AttendanceRules.Classify(6) == "OddHours", "Classify full days, half days and odd hours");
var calendar = new AttendanceCalendarViewModel
{
    Logs = new()
    {
        [(1, 1)] = new() { HoursWorked = 8, WorkType = null },
        [(1, 2)] = new() { HoursWorked = 4, WorkType = "FullDay" },
        [(1, 3)] = new() { HoursWorked = 6 },
        [(1, 4)] = new() { HoursWorked = null, WorkType = "FullDay" },
        [(1, 5)] = new() { HoursWorked = 25, WorkType = "FullDay" }
    }
};
Check(calendar.GetWorkerSummary(1) == (1.5m, 6m), "Totals use valid hours, not stale classifications");
Check(calendar.GetHours(2, 1) == null, "Missing attendance remains unknown");
PayrollChecks.Unit(Check);
AdvanceCropChecks.Unit(Check);
SeasonJournalChecks.Unit(Check);
MaterialUsageChecks.Unit(Check);

if (!args.Contains("--integration") && !args.Contains("--preview"))
{
    Console.WriteLine("PASS: schema and business checks. Add --integration for disposable SQL Server and HTTP checks.");
    return;
}

// This suite creates and deletes ONLY its own uniquely named database. Never use the app database here.
var database = "FarmManagement_Checks_" + Guid.NewGuid().ToString("N");
var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("FARM_TEST_SQLSERVER")
    ?? "Server=(localdb)\\MSSQLLocalDB;Integrated Security=true;TrustServerCertificate=true;Connect Timeout=15")
{ InitialCatalog = "master", Pooling = false };
await using var master = new SqlConnection(connection.ConnectionString);
await master.OpenAsync();
var folder = Path.Combine(root, "tests", "Farm-Management.Checks", "bin", database);
Directory.CreateDirectory(folder);
var dataPath = Path.Combine(folder, database + ".mdf").Replace("'", "''");
var logPath = Path.Combine(folder, database + ".ldf").Replace("'", "''");
await using (var create = new SqlCommand($"CREATE DATABASE [{database}] ON (NAME=N'{database}', FILENAME=N'{dataPath}') LOG ON (NAME=N'{database}_log', FILENAME=N'{logPath}')", master))
    await create.ExecuteNonQueryAsync();
Process? app = null;
try
{
    connection.InitialCatalog = database;
    var dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection.ConnectionString).Options;
    await using (var db = new ApplicationDbContext(dbOptions))
    {
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync("20260211071346_AddAttendanceLog");
        await db.Database.ExecuteSqlRawAsync("""
            INSERT INTO Workers (Code, FullName, DailySalary, IsActive, CreatedAt) VALUES (N'OLD', N'Existing worker', 200000, 1, '2026-01-01');
            INSERT INTO AttendanceLogs (WorkerId, Date, HoursWorked, WorkType, CreatedAt) VALUES (1, '2026-11-01', 8, N'FullDay', '2026-01-01');
            """);
        await migrator.MigrateAsync("20261108015906_AddMonthlyPayroll");
        await db.Database.ExecuteSqlRawAsync("""
            INSERT INTO PayrollRuns (Year, Month, Status, FinalizedAt, WorkerCount, TotalAmount, AttendanceHash)
              VALUES (2026,11,N'Reopened','2026-11-01',1,200000,REPLICATE('a',64));
            INSERT INTO PayrollEntries (PayrollRunId,WorkerId,WorkerCode,WorkerName,DailySalary,FullDays,HalfDays,OddHours,TotalHours,AttendanceCount,Amount,AttendanceJson)
              VALUES (1,1,N'OLD',N'Existing worker',200000,1,0,0,8,1,200000,N'[{{"AttendanceId":1,"Date":"2026-11-01T00:00:00","Hours":8}}]');
            """);
        await migrator.MigrateAsync("20261108032912_AddCropSeasonJournal");
        await db.Database.ExecuteSqlRawAsync("""
            INSERT INTO Plants (BatchCode,SeasonName,PlantingDate,Status) VALUES (N'BEFORE-HISTORY',N'Mùa vụ cũ cần giữ','2024-01-01',N'Growing');
            INSERT INTO CropJournalEntries (PlantId,Date,Kind,Summary,Recorder,RecordCode) VALUES (1,'2024-01-02',N'Care',N'Nội dung trước tính năng lịch sử',N'Người ghi sổ cũ',N'0123456789abcdef0123456789abcdef');
            """);
        await migrator.MigrateAsync();
        Check(await db.CultivationAudits.AnyAsync(a=>a.RecordType=="CropJournalEntry" && a.RecordId==1 && a.Action=="Baseline" && a.Actor==null && a.BeforeJson==null),"History upgrade preserves a baseline without inventing older edits or operator identity");
        var legacy = await new Farm_Management.Services.PayrollService(db).GetArchiveAsync(1);
        Check(legacy is { SnapshotInvalid: false, NetTotal: 200000 } && legacy.Finalized!.CalculationVersion == 1,
            "Upgrade preserves legacy payroll snapshots with an explicit version marker");
        Check(await db.Workers.AnyAsync(w => w.Code == "OLD" && w.DailySalary == 200000), "Upgrade preserves existing worker data");
        Check(await db.AttendanceLogs.AnyAsync(a => a.HoursWorked == 8), "Upgrade preserves existing attendance");
        foreach (var entity in db.Model.GetEntityTypes())
        {
            var tableName = entity.GetTableName()!;
            if (!Regex.IsMatch(tableName, "^[A-Za-z]+$")) throw new Exception("Unexpected table name");
#pragma warning disable EF1002 // Validated internal model identifier; never accepts external input.
            await db.Database.ExecuteSqlRawAsync($"INSERT INTO [{tableName}] DEFAULT VALUES");
#pragma warning restore EF1002
        }
        await db.Database.ExecuteSqlRawAsync("""
            INSERT INTO AttendanceLogs (WorkerId, Date, HoursWorked) VALUES (999999, '2026-11-02', 6);
            INSERT INTO AttendanceLogs (WorkerId, Date, HoursWorked) VALUES (1, '2026-11-03', 8), (1, '2026-11-03', 4);
            """);
        Check(await db.AttendanceLogs.AnyAsync(a => a.WorkerId == 999999), "Database accepts soft references");
        // An incompatible rollback must fail without manufacturing NULL replacements.
        try
        {
            await migrator.MigrateAsync("20260211071346_AddAttendanceLog");
            throw new Exception("Rollback unexpectedly accepted incomplete data");
        }
        catch (SqlException ex) when (ex.Number is 50002 or 50003 or 50004 or 50005 or 50006) { }
        Check(await db.Workers.AnyAsync(w => w.Code == null), "Rollback failure preserves NULL data");
    }

    var portListener = new TcpListener(System.Net.IPAddress.Loopback, 0);
    portListener.Start();
    var port = ((System.Net.IPEndPoint)portListener.LocalEndpoint).Port;
    portListener.Stop();
    var start = new ProcessStartInfo("dotnet")
    {
        WorkingDirectory = Path.Combine(root, "Farm-Management"),
        UseShellExecute = false, CreateNoWindow = true,
        RedirectStandardOutput = true, RedirectStandardError = true
    };
    var configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
    start.ArgumentList.Add(Path.Combine(root, "Farm-Management", "bin", configuration, "net8.0", "Farm-Management.dll"));
    start.ArgumentList.Add("--urls");
    start.ArgumentList.Add($"http://127.0.0.1:{port}");
    start.Environment["ConnectionStrings__DefaultConnection"] = connection.ConnectionString;
    start.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
    app = Process.Start(start)!;
    var output = app.StandardOutput.ReadToEndAsync();
    var error = app.StandardError.ReadToEndAsync();
    using var client = new HttpClient(new HttpClientHandler { CookieContainer = new(), AllowAutoRedirect = false })
    { BaseAddress = new Uri($"http://127.0.0.1:{port}") };
    for (var i = 0; ; i++)
    {
        try { await client.GetAsync("/"); break; }
        catch (HttpRequestException) when (i < 40 && !app.HasExited) { await Task.Delay(250); }
    }
    foreach (var route in new[] { "/", "/Areas", "/Areas/Create", "/Workers", "/Workers/Create", "/Workers/Edit/2",
        "/Attendance", "/Attendance?missingDate=true", "/Attendance/Calendar?year=2026&month=11",
        "/Attendance/Create", "/Attendance/Edit/2", "/Attendance/Delete/2", "/Attendance/Details/2",
        "/Areas/Edit/1", "/Areas/Details/1", "/Areas/Delete/1", "/Workers/Details/2", "/Workers/Delete/2", "/Home/Privacy", "/Home/Error" })
        Check((await client.GetAsync(route)).StatusCode == HttpStatusCode.OK, "Render " + route);
    var missing = await client.GetAsync("/missing-page-for-qa");
    Check(missing.StatusCode == HttpStatusCode.NotFound && (await missing.Content.ReadAsStringAsync()).Contains("Farm Management"), "Friendly 404 page preserves the application shell");
    Check((await client.GetAsync("/Home/Status?code=0")).StatusCode == HttpStatusCode.NotFound, "Invalid status codes fail safely");
    var workerPage = await client.GetStringAsync("/Workers/Create");
    Check(workerPage.Contains("data-val-required"), "Forms retain client-side required validation");
    var response = await Post("/Workers/Create", new() { ["Code"] = "NEW", ["FullName"] = "Test", ["DailySalary"] = "200000", ["IsActive"] = "true" });
    Check(response.StatusCode == HttpStatusCode.Redirect, "Create worker through MVC");
    response = await Post("/Workers/Create", new() { ["Code"] = "NEW", ["FullName"] = "Duplicate", ["DailySalary"] = "200000", ["IsActive"] = "true" });
    Check(response.StatusCode == HttpStatusCode.OK, "Duplicate worker returns validation form");
    response = await Post("/Attendance/Create", new() { ["WorkerId"] = "1", ["Date"] = "2026-11-04", ["HoursWorked"] = "6" });
    Check(response.StatusCode == HttpStatusCode.Redirect, "Create attendance through MVC");
    response = await Post("/Attendance/Create", new() { ["WorkerId"] = "1", ["Date"] = "2026-11-04", ["HoursWorked"] = "8" });
    Check(response.StatusCode == HttpStatusCode.OK, "Duplicate attendance returns validation form");
    response = await Post("/Attendance/Create", new() { ["WorkerId"] = "999999", ["Date"] = "2026-11-04", ["HoursWorked"] = "8" });
    Check(response.StatusCode == HttpStatusCode.OK, "Missing worker returns validation form");
    response = await Post("/Attendance/Create", new() { ["WorkerId"] = "1", ["HoursWorked"] = "8" });
    Check(response.StatusCode == HttpStatusCode.OK, "Missing date safely returns validation form");
    response = await Post("/Attendance/Edit/2", new() { ["Id"] = "2", ["WorkerId"] = "1", ["Date"] = "2026-11-05", ["HoursWorked"] = "4" });
    Check(response.StatusCode == HttpStatusCode.Redirect, "Repair incomplete attendance");
    response = await client.PostAsync("/Attendance/SaveCell", new FormUrlEncodedContent(new Dictionary<string, string> { ["workerId"] = "1", ["date"] = "2026-11-06", ["hours"] = "8" }));
    Check(response.StatusCode == HttpStatusCode.BadRequest, "AJAX rejects a missing antiforgery token");
    response = await Post("/Attendance/SaveCell", new() { ["workerId"] = "1", ["date"] = "2026-11-06", ["hours"] = "8" }, "/Attendance/Calendar");
    Check((await response.Content.ReadAsStringAsync()).Contains("\"success\":true"), "Save a calendar cell");
    response = await Post("/Attendance/SaveCell", new() { ["workerId"] = "1", ["date"] = "2026-11-03", ["hours"] = "8" }, "/Attendance/Calendar");
    Check((await response.Content.ReadAsStringAsync()).Contains("\"success\":false"), "Reject overwriting a duplicate calendar cell");
    response = await Post("/Attendance/MarkAttendance", new() { ["date"] = "2026-11-07", ["workerIds"] = "1" }, "/Attendance");
    Check(response.StatusCode == HttpStatusCode.Redirect, "Malformed batch fails gracefully");
    var concurrentWorkers = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ => Post("/Workers/Create",
        new() { ["Code"] = "RACE", ["FullName"] = "Concurrent worker", ["DailySalary"] = "200000", ["IsActive"] = "true" })));
    Check(concurrentWorkers.Count(r => r.StatusCode == HttpStatusCode.Redirect) == 1
        && concurrentWorkers.Count(r => r.StatusCode == HttpStatusCode.OK) == 1, "Concurrent duplicate worker requests serialize correctly");
    var concurrentAttendance = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ => Post("/Attendance/Create",
        new() { ["WorkerId"] = "1", ["Date"] = "2026-11-08", ["HoursWorked"] = "8" })));
    Check(concurrentAttendance.Count(r => r.StatusCode == HttpStatusCode.Redirect) == 1
        && concurrentAttendance.Count(r => r.StatusCode == HttpStatusCode.OK) == 1, "Concurrent duplicate attendance requests serialize correctly");
    var invalidHours = await Post("/Attendance/SaveCell", new() { ["workerId"] = "1", ["date"] = "2026-11-09", ["hours"] = "7.99" }, "/Attendance/Calendar");
    Check((await invalidHours.Content.ReadAsStringAsync()).Contains("\"success\":false"), "AJAX enforces the same half-hour rule as forms");
    response = await Post("/Areas/Create", new() { ["Name"] = "Test area", ["Acreage"] = "100", ["GpsCoordinates"] = "10.5, 106.5" });
    Check(response.StatusCode == HttpStatusCode.Redirect, "Create area through MVC");
    int areaId;
    await using (var db = new ApplicationDbContext(dbOptions)) areaId = (await db.Areas.SingleAsync(a => a.Name == "Test area")).Id;
    response = await Post($"/Areas/Edit/{areaId}", new() { ["Id"] = areaId.ToString(), ["Name"] = "Updated area", ["Acreage"] = "200" });
    Check(response.StatusCode == HttpStatusCode.Redirect, "Edit area through MVC");
    Check((await client.GetAsync($"/Areas/Details/{areaId}")).StatusCode == HttpStatusCode.OK, "Render area details with soft references");
    response = await Post($"/Areas/Delete/{areaId}", new() { ["Id"] = areaId.ToString() });
    Check(response.StatusCode == HttpStatusCode.Redirect, "Delete unused area through MVC");
    response = await Post("/Workers/Delete/1", new() { ["Id"] = "1" }, "/Workers/Delete/1");
    Check(response.StatusCode == HttpStatusCode.Redirect, "Worker delete returns to list");
    await using (var db = new ApplicationDbContext(dbOptions))
    {
        Check(!await db.Areas.AnyAsync(a => a.Id == areaId), "Unused area was deleted");
        Check(await db.Workers.CountAsync(w => w.Code == "RACE") == 1, "Concurrent writes produce one worker");
        Check(await db.AttendanceLogs.CountAsync(a => a.WorkerId == 1 && a.Date == new DateTime(2026, 11, 8)) == 1, "Concurrent writes produce one attendance entry");
        Check(await db.Workers.AnyAsync(w => w.Id == 1), "Cannot delete worker with attendance history");
        Check(await db.Workers.CountAsync(w => w.Code == "NEW") == 1, "Duplicate code rejected without database uniqueness");
        Check(await db.AttendanceLogs.CountAsync(a => a.WorkerId == 1 && a.Date == new DateTime(2026, 11, 4)) == 1, "Duplicate attendance rejected without database uniqueness");
        var old = await db.Workers.FindAsync(1);
        old!.IsActive = false;
        await db.SaveChangesAsync();
    }
    var monthHtml = await client.GetStringAsync("/Attendance/Calendar?year=2026&month=11");
    Check(monthHtml.Contains("Existing worker"), "Former workers remain visible in historical calendar");
    await using (var db = new ApplicationDbContext(dbOptions))
    {
        Check((await new Farm_Management.Services.PayrollService(db).GetMonthAsync(2024, 2)).Issues.Any(i => i.Advance), "Unassigned NULL advance blocks finalization without assuming zero");
        db.WorkerAdvances.RemoveRange(await db.WorkerAdvances.Where(a => a.Reference == null).ToListAsync());
        await db.SaveChangesAsync();
    }
    await PayrollChecks.Integration(client, dbOptions, Check);
    await AdvanceCropChecks.Integration(client, dbOptions, Check);
    await SeasonJournalChecks.Integration(client, dbOptions, Check);
    await ProvenanceChecks.Integration(client, dbOptions, Check);
    await MaterialUsageChecks.Integration(client, dbOptions, Check);
    Console.WriteLine("PASS: disposable SQL Server migration and MVC/HTTP checks including monthly payroll.");
    if (args.Contains("--preview"))
    {
        await using (var db = new ApplicationDbContext(dbOptions))
        {
            for (var i = 1; i <= 14; i++)
            {
                var worker = new Worker { Code = "QA" + i.ToString("00"), FullName = "Công nhân kiểm thử " + i.ToString("00"),
                    Phone = "090000" + i.ToString("0000"), DailySalary = 200000 + i * 10000, IsActive = i <= 11, HireDate = DateTime.Today.AddDays(-90) };
                db.Workers.Add(worker); await db.SaveChangesAsync();
                for (var d = 0; d < 7; d++)
                    if ((i + d) % 3 != 0)
                        db.AttendanceLogs.Add(new AttendanceLog { WorkerId = worker.Id, Date = DateTime.Today.AddDays(-d),
                            HoursWorked = d % 3 == 0 ? 8 : d % 3 == 1 ? 4 : 6, WorkType = null });
            }
            for (var i = 1; i <= 3; i++) db.Areas.Add(new Area { Name = "Vùng kiểm thử " + i, Acreage = i * 1500, SoilType = i == 1 ? "Đất phù sa" : "Đất thịt", GpsCoordinates = "10.7626, 106.6601" });
            await db.SaveChangesAsync();
        }
        var local = Path.Combine(root, ".local"); Directory.CreateDirectory(local);
        var stopPath = Path.Combine(local, "stop-qa");
        if (File.Exists(stopPath)) File.Delete(stopPath);
        await File.WriteAllTextAsync(Path.Combine(local, "qa-preview.json"), System.Text.Json.JsonSerializer.Serialize(new { url = client.BaseAddress.ToString(), database, processId = app.Id }));
        Console.WriteLine("QA preview ready: " + client.BaseAddress);
        Console.WriteLine("Create .local/stop-qa to stop the preview and remove its disposable database.");
        while (!File.Exists(stopPath)) await Task.Delay(500);
        File.Delete(stopPath);
    }

    async Task<HttpResponseMessage> Post(string route, Dictionary<string, string> fields, string? tokenRoute = null)
    {
        var html = await client.GetStringAsync(tokenRoute ?? route);
        fields["__RequestVerificationToken"] = WebUtility.HtmlDecode(Regex.Match(html,
            "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
        Check(fields["__RequestVerificationToken"].Length > 0, "Antiforgery token present for " + route);
        TestChangeFields.Apply(fields,html);
            return await client.PostAsync(route, new FormUrlEncodedContent(fields));
    }
}
finally
{
    if (app is { HasExited: false }) { app.Kill(entireProcessTree: true); await app.WaitForExitAsync(); }
    app?.Dispose();
    await using var drop = new SqlCommand($"ALTER DATABASE [{database}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{database}];", master);
    await drop.ExecuteNonQueryAsync();
    Directory.Delete(folder);
}

static bool Valid(object value) => Validator.TryValidateObject(value, new ValidationContext(value), new List<ValidationResult>(), true);
static void Check(bool condition, string message)
{
    if (!condition) throw new Exception("FAIL: " + message);
    Console.WriteLine("PASS: " + message);
}
static string FindRoot()
{
    var directory = new DirectoryInfo(AppContext.BaseDirectory);
    while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Farm-Management.sln"))) directory = directory.Parent;
    return directory?.FullName ?? throw new InvalidOperationException("Run checks from the repository checkout.");
}
