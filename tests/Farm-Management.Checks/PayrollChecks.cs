using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Farm_Management.Data;
using Farm_Management.Models;
using Farm_Management.Services;
using Microsoft.EntityFrameworkCore;

internal static class PayrollChecks
{
    public static void Unit(Action<bool, string> check)
    {
        var worker = new Worker { Id = 1, Code = "PAY", FullName = "Payroll fixture", DailySalary = 200000, IsActive = false };
        var workers = new Dictionary<int, Worker> { [1] = worker };
        var logs = new[] { Log(1, 1, 8), Log(2, 2, 4), Log(3, 3, 6), Log(4, 29, 10) };
        var month = PayrollService.Calculate(2024, 2, logs, workers);
        check(month.CanFinalize && month.Total == 650000 && month.Rows.Single().TotalHours == 28, "Payroll handles full days, half days, odd hours, over-8-hour cap and leap day");
        check(month.Rows.Single().FullDays == 2 && month.Rows.Single().HalfDays == 1 && month.Rows.Single().OddHours == 6, "Payroll derives classification from hours even when WorkType is stale");
        check(month.Rows.Count == 1, "Former workers with attendance remain payable");
        check(!PayrollService.Calculate(2024, 2, logs.Concat(new[] { Log(5, 1, 4) }), workers).CanFinalize, "Duplicate same-day attendance blocks payroll finalization");
        check(!PayrollService.Calculate(2024, 2, logs.Concat(new[] { new AttendanceLog { Id = 5, WorkerId = 1, Date = new DateTime(2024, 2, 1, 15, 0, 0), HoursWorked = 8 } }), workers).CanFinalize, "Duplicate dates with different time components are still duplicates");
        check(PayrollService.Calculate(2024, 2, new[] { new AttendanceLog { Id = 1, Date = new(2024, 2, 1), WorkerId = 999, HoursWorked = 8 } }, workers).Issues.Count > 0, "Soft orphan references block payroll");
        check(!PayrollService.Calculate(2024, 2, logs, workers, 1).CanFinalize, "Unassigned attendance dates block finalization");
        check(!PayrollService.Calculate(2024, 2, Array.Empty<AttendanceLog>(), workers).CanFinalize, "An empty month cannot be finalized");
        var invalidHours = PayrollService.Calculate(2024, 2, logs.Concat(new[] { new AttendanceLog { Id = 5, WorkerId = 1, Date = new(2024, 2, 4), HoursWorked = null } }), workers);
        check(invalidHours.Rows.Single().Amount == null && !invalidHours.CanFinalize, "Invalid worker data never produces a partial monthly salary");
        var duplicateCodes = new Dictionary<int, Worker>(workers) { [2] = new Worker { Id = 2, Code = "pay", FullName = "Other", DailySalary = 200000 } };
        check(!PayrollService.Calculate(2024, 2, logs, duplicateCodes).CanFinalize, "Duplicate worker codes are rejected before payroll finalization");
        worker.DailySalary = null;
        check(PayrollService.Calculate(2024, 2, logs, workers).Rows.Single().Amount == null, "A missing daily salary is not treated as zero");
        worker.DailySalary = 101;
        check(PayrollService.Calculate(2024, 2, new[] { Log(1, 1, .5m), Log(2, 2, .5m) }, workers).Total == 13, "Money rounds once per worker, not once per day");
        check(PayrollService.Money(100.5m) == 101, "Half a dong rounds away from zero");
        var multipleWorkers = new Dictionary<int, Worker> { [1] = worker, [2] = new Worker { Id = 2, Code = "OTHER", FullName = "Other", DailySalary = 101 } };
        var second = Log(2, 1, .5m); second.WorkerId = 2;
        check(PayrollService.Calculate(2024, 2, new[] { Log(1, 1, .5m), second }, multipleWorkers).Total == 12, "Grand total sums each worker's rounded amount");
        worker.DailySalary = 200000;
        var entry = PayrollService.Snapshot(month.Rows.Single(), 1);
        check(PayrollService.ReadEntry(entry, 2024, 2)?.Amount == 650000, "Payroll snapshots round-trip with immutable daily source");
        entry.AttendanceJson = "[{\"Hours\":8}]";
        check(PayrollService.ReadEntry(entry, 2024, 2) == null, "Incomplete JSON does not fabricate missing dates or attendance IDs");
        entry = PayrollService.Snapshot(month.Rows.Single(), 1); entry.Amount = null;
        check(PayrollService.ReadEntry(entry, 2024, 2) == null, "Missing snapshot amount is detected");
    }

    public static async Task Integration(HttpClient client, DbContextOptions<ApplicationDbContext> options, Action<bool, string> check)
    {
        int workerId;
        int firstLogId;
        await using (var db = new ApplicationDbContext(options))
        {
            var worker = new Worker { Code = "PAY01", FullName = "Công nhân kiểm thử lương", DailySalary = 200000, IsActive = false };
            db.Workers.Add(worker); await db.SaveChangesAsync(); workerId = worker.Id;
            var logs = new[] { Log(0, 1, 8), Log(0, 2, 4), Log(0, 3, 6), Log(0, 29, 10) };
            foreach (var log in logs) log.WorkerId = workerId;
            db.AttendanceLogs.AddRange(logs);
            db.AttendanceLogs.Add(new AttendanceLog { WorkerId = workerId, Date = new(2024, 3, 1), HoursWorked = 8 });
            await db.SaveChangesAsync(); firstLogId = logs[0].Id;
        }
        foreach (var route in new[] { "/Payroll?year=2024&month=2", "/Payroll/Review?year=2024&month=2", "/Payroll?year=2024&month=1" })
            check((await client.GetAsync(route)).StatusCode == HttpStatusCode.OK, "Render " + route);
        check((await client.GetAsync("/Payroll?year=2024&month=13")).StatusCode == HttpStatusCode.BadRequest, "Reject invalid payroll periods");
        var preview = await client.GetStringAsync("/Payroll?year=2024&month=2");
        check(preview.Contains("650.000"), "Monthly preview includes leap day and excludes the next month");
        var initialToken = await ReviewToken();
        await Salary(210000);
        await Post("/Payroll/FinalizeMonth", new() { ["Year"] = "2024", ["Month"] = "2", ["ReviewToken"] = initialToken }, "/Payroll/Review?year=2024&month=2");
        await using (var db = new ApplicationDbContext(options))
            check(!await db.PayrollRuns.AnyAsync(r => r.Year == 2024 && r.Month == 2), "A stale salary review cannot be finalized");
        await Salary(200000);
        var token = await ReviewToken();
        var responses = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ => Post("/Payroll/FinalizeMonth",
            new() { ["Year"] = "2024", ["Month"] = "2", ["ReviewToken"] = token }, "/Payroll/Review?year=2024&month=2")));
        check(responses.All(r => r.StatusCode == HttpStatusCode.Redirect), "Concurrent finalization requests return safely");
        int runId;
        await using (var db = new ApplicationDbContext(options))
        {
            var runs = await db.PayrollRuns.Where(r => r.Year == 2024 && r.Month == 2 && r.Status == PayrollService.Finalized).ToListAsync();
            check(runs.Count == 1, "Exactly one finalized run exists without a database unique constraint");
            runId = runs.Single().Id;
            var model = await new PayrollService(db).GetMonthAsync(2024, 2);
            check(!model.SnapshotInvalid && model.Total == 650000 && model.Rows.Single().DailySalary == 200000, "Finalization stores complete payroll snapshots");
        }
        await Salary(400000);
        await using (var db = new ApplicationDbContext(options))
        {
            var model = await new PayrollService(db).GetMonthAsync(2024, 2);
            check(model.Total == 650000 && model.Rows.Single().DailySalary == 200000 && !model.SourceChanged, "Later salary changes do not alter or falsely invalidate finalized payroll");
        }
        var save = await Post("/Attendance/SaveCell", new() { ["workerId"] = workerId.ToString(), ["date"] = "2024-02-01", ["hours"] = "4" }, "/Attendance/Calendar?year=2024&month=2");
        check((await save.Content.ReadAsStringAsync()).Contains("\"success\":false"), "Calendar edits reject finalized months");
        var create = await Post("/Attendance/Create", new() { ["WorkerId"] = "3", ["Date"] = "2024-02-05", ["HoursWorked"] = "8" });
        check(create.StatusCode == HttpStatusCode.OK, "Single-create form rejects finalized months");
        await Post("/Attendance/MarkAttendance", new() { ["date"] = "2024-02-05", ["workerIds"] = "3", ["hours"] = "8", ["notes"] = "" }, "/Attendance/Create?date=2024-02-05");
        var edit = await Post($"/Attendance/Edit/{firstLogId}", new() { ["Id"] = firstLogId.ToString(), ["WorkerId"] = workerId.ToString(), ["Date"] = "2024-03-05", ["HoursWorked"] = "8" });
        check(edit.StatusCode == HttpStatusCode.OK, "Moving attendance out of a finalized month is blocked");
        await Post($"/Attendance/Delete/{firstLogId}", new() { ["Id"] = firstLogId.ToString() });
        await using (var db = new ApplicationDbContext(options))
        {
            var original = await db.AttendanceLogs.FindAsync(firstLogId);
            check(original?.Date == new DateTime(2024, 2, 1) && original.HoursWorked == 8, "Finalized attendance survives forged edit and delete requests");
            check(!await db.AttendanceLogs.AnyAsync(a => a.WorkerId == 3 && a.Date == new DateTime(2024, 2, 5)), "Both create paths respect payroll locks");
        }
        check((await client.GetAsync($"/Payroll/Archive/{runId}")).StatusCode == HttpStatusCode.OK, "Render an archived payroll snapshot");
        var csv = await client.GetAsync("/Payroll/Export?year=2024&month=2");
        check(csv.StatusCode == HttpStatusCode.OK && (await csv.Content.ReadAsStringAsync()).Contains("650000"), "Export uses saved payroll values");
        var noCsrf = await client.PostAsync($"/Payroll/Reopen/{runId}", new FormUrlEncodedContent(new Dictionary<string, string> { ["id"] = runId.ToString() }));
        check(noCsrf.StatusCode == HttpStatusCode.BadRequest, "Payroll lifecycle endpoints require antiforgery protection");
        await Post($"/Payroll/Reopen/{runId}", new() { ["id"] = runId.ToString() });
        var reopenedSave = await Post("/Attendance/SaveCell", new() { ["workerId"] = workerId.ToString(), ["date"] = "2024-02-01", ["hours"] = "4" }, "/Attendance/Calendar?year=2024&month=2");
        check((await reopenedSave.Content.ReadAsStringAsync()).Contains("\"success\":true"), "Reopening unlocks existing attendance, including former workers");
        var newToken = await ReviewToken();
        await Post("/Payroll/FinalizeMonth", new() { ["Year"] = "2024", ["Month"] = "2", ["ReviewToken"] = newToken }, "/Payroll/Review?year=2024&month=2");
        int newRunId;
        await using (var db = new ApplicationDbContext(options))
        {
            var service = new PayrollService(db);
            var model = await service.GetMonthAsync(2024, 2);
            newRunId = model.Finalized!.Id;
            check(model.Total == 1100000 && model.History.Count == 2, "Re-finalization uses revised attendance and the current salary while retaining history");
            var archive = await service.GetArchiveAsync(runId);
            check(archive!.Total == 650000 && archive.Rows.Single().Days.Single(d => d.AttendanceId == firstLogId).Hours == 8, "Old snapshots retain their original salary and daily attendance");
            var log = await db.AttendanceLogs.FindAsync(firstLogId); log!.HoursWorked = 2; await db.SaveChangesAsync();
            check((await service.GetMonthAsync(2024, 2)).SourceChanged, "Direct changes to soft data are detected after finalization");
            log.HoursWorked = 4; await db.SaveChangesAsync();
            var entry = await db.PayrollEntries.SingleAsync(e => e.PayrollRunId == newRunId);
            entry.Amount = null; await db.SaveChangesAsync();
            check((await service.GetMonthAsync(2024, 2)).SnapshotInvalid, "Incomplete stored payroll cannot be silently accepted");
        }
        check((await client.GetAsync("/Payroll/Export?year=2024&month=2")).StatusCode == HttpStatusCode.BadRequest, "Export refuses an incomplete stored payroll");
        await using (var db = new ApplicationDbContext(options))
        {
            var entry = await db.PayrollEntries.SingleAsync(e => e.PayrollRunId == newRunId); entry.Amount = 1100000;
            var extra = new PayrollRun { Year = 2024, Month = 2, Status = PayrollService.Finalized };
            db.PayrollRuns.Add(extra); await db.SaveChangesAsync();
            check((await new PayrollService(db).GetMonthAsync(2024, 2)).ConflictingRuns, "Soft duplicate payroll runs are detected");
            extra.Status = PayrollService.Reopened; await db.SaveChangesAsync();
            var formula = new Worker { Code = "=PAY", FullName = "+SUM(A1)", DailySalary = 200000, IsActive = true };
            db.Workers.Add(formula); await db.SaveChangesAsync();
            db.AttendanceLogs.Add(new AttendanceLog { WorkerId = formula.Id, Date = new(2024, 4, 1), HoursWorked = 8 });
            await db.SaveChangesAsync();
        }
        var formulaCsv = await client.GetStringAsync("/Payroll/Export?year=2024&month=4");
        check(formulaCsv.Contains("'=PAY") && formulaCsv.Contains("'+SUM(A1)"), "CSV keeps formula-like names and codes as text");
        check((await client.GetAsync("/Payroll/Export?year=2026&month=11")).StatusCode == HttpStatusCode.BadRequest, "Export rejects an invalid draft month");

        async Task Salary(decimal salary) => await Post($"/Workers/Edit/{workerId}", new()
        {
            ["Id"] = workerId.ToString(), ["Code"] = "PAY01", ["FullName"] = "Công nhân kiểm thử lương",
            ["DailySalary"] = salary.ToString(System.Globalization.CultureInfo.InvariantCulture), ["IsActive"] = "false"
        });
        async Task<string> ReviewToken()
        {
            var html = await client.GetStringAsync("/Payroll/Review?year=2024&month=2");
            return Regex.Match(html, "name=\"ReviewToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;
        }
        async Task<HttpResponseMessage> Post(string route, Dictionary<string, string> fields, string? tokenRoute = null)
        {
            var html = await client.GetStringAsync(tokenRoute ?? route);
            fields["__RequestVerificationToken"] = WebUtility.HtmlDecode(Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
            if (fields["__RequestVerificationToken"].Length == 0) throw new Exception("Missing antiforgery token for " + route);
            TestChangeFields.Apply(fields,html);
            return await client.PostAsync(route, new FormUrlEncodedContent(fields));
        }
    }

    private static AttendanceLog Log(int id, int day, decimal hours) => new()
    {
        Id = id, WorkerId = 1, Date = new DateTime(2024, 2, day), HoursWorked = hours, WorkType = "stale"
    };
}
