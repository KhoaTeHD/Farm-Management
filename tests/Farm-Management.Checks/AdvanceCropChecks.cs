using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text.RegularExpressions;
using Farm_Management.Data;
using Farm_Management.Models;
using Farm_Management.Models.ViewModels;
using Farm_Management.Services;
using Microsoft.EntityFrameworkCore;

internal static class AdvanceCropChecks
{
    public static void Unit(Action<bool, string> check)
    {
        var workers = new Dictionary<int, Worker> { [1] = new() { Id = 1, Code = "A", FullName = "Advance fixture", DailySalary = 200000 } };
        var logs = new[] { new AttendanceLog { Id = 1, WorkerId = 1, Date = new(2024, 5, 1), HoursWorked = 8 } };
        WorkerAdvance Receipt(int id, decimal? amount) => new() { Id = id, WorkerId = 1, Reference = "TU" + id, Amount = amount, PaidDate = new(2024, 4, 30), PayrollYear = 2024, PayrollMonth = 5 };
        PayrollMonthViewModel Calculate(params WorkerAdvance[] receipts) => PayrollService.Calculate(2024, 5, logs, workers, advances: receipts);
        var month = Calculate(Receipt(1, 30000), Receipt(2, 20000));
        check(month.CanFinalize && month.Total == 200000 && month.TotalAdvances == 50000 && month.NetTotal == 150000, "Multiple receipts deduct once from gross pay in the selected month");
        var other = Receipt(3, 10000); other.PayrollMonth = 4;
        check(Calculate(other).NetTotal == 200000, "Payment date and deduction month have distinct meanings");
        check(!Calculate(Receipt(1, null)).CanFinalize && Calculate(Receipt(1, null)).Rows.Single().AdvanceAmount == null, "Unknown advances are never converted to zero");
        check(!Calculate(Receipt(1, 200001)).CanFinalize, "Advance exceeding wages blocks finalization instead of silently carrying debt");
        check(Calculate(Receipt(1, 200000)).CanFinalize && Calculate(Receipt(1, 200000)).NetTotal == 0, "Exact full deduction permits zero take-home pay");
        var duplicate = Receipt(2, 100); duplicate.Reference = " tu1 ";
        check(!Calculate(Receipt(1, 100), duplicate).CanFinalize, "Duplicate receipt references are detected despite case and whitespace");
        var orphan = Receipt(1, 100); orphan.WorkerId = 99999;
        check(!Calculate(orphan).CanFinalize, "Orphan advances block payroll");
        check(!PayrollService.Calculate(2024, 5, Array.Empty<AttendanceLog>(), workers, advances: new[] { Receipt(1, 100) }).CanFinalize, "Advance-only workers cannot silently disappear from payroll");
        check(!PayrollService.ValidAdvanceAmount(1.5m) && !PayrollService.ValidAdvanceAmount(0), "Advance amounts must be positive whole dong");
        var entry = PayrollService.Snapshot(month.Rows.Single(), 1);
        check(PayrollService.ReadEntry(entry, 2024, 5)?.NetAmount == 150000, "Advance snapshots round-trip with receipt details");
        entry.AdvancesJson = "[{}]";
        check(PayrollService.ReadEntry(entry, 2024, 5) == null, "Incomplete receipt JSON invalidates snapshots");
        entry = PayrollService.Snapshot(month.Rows.Single(), 1); entry.NetAmount = null;
        check(PayrollService.ReadEntry(entry, 2024, 5) == null && PayrollService.ReadEntry(entry, 2024, 5, -1) == null, "Missing net amounts and unknown calculation versions are rejected");
        var form = new AdvanceForm { WorkerId = 1, Reference = "A", PaidDate = DateTime.Today.AddDays(1), PayrollYear = 2024, PayrollMonth = 5, Amount = 100 };
        check(!Validator.TryValidateObject(form, new(form), new List<ValidationResult>(), true), "Future advance payment dates fail form validation");
        check(CropRules.Expected(new DateTime(2024, 2, 1), 30) == new DateTime(2024, 3, 2) && CropRules.Expected(new DateTime(9998, 12, 31), 36500) == null, "Crop harvest estimates handle leap years and overflow");
        check(CropRules.Safety(new[] { new PesticideApplicationLog { ApplicationDate = DateTime.Today, PHI_Days = 7 } }).SafeDate == DateTime.Today.AddDays(7), "Harvest safety derives from application date and withdrawal interval");
        check(CropRules.Safety(new[] { new PesticideApplicationLog() }).Incomplete, "Unknown withdrawal information remains incomplete");
    }

    public static async Task Integration(HttpClient client, DbContextOptions<ApplicationDbContext> options, Action<bool, string> check)
    {
        int workerId;
        await using (var db = new ApplicationDbContext(options))
        {
            var worker = new Worker { Code = "ADV01", FullName = "Công nhân tạm ứng", DailySalary = 300000, IsActive = true };
            db.Workers.Add(worker); await db.SaveChangesAsync(); workerId = worker.Id;
            db.AttendanceLogs.Add(new() { WorkerId = workerId, Date = new(2024, 5, 1), HoursWorked = 8 }); await db.SaveChangesAsync();
        }
        foreach (var path in new[] { "/Advances", "/Advances?incomplete=true", "/Advances/Create", "/Plants", "/Plants/Create", "/Plants/Edit/1", "/Plants/Details/1", "/Plants/Delete/1", "/Seeds", "/Seeds/Create", "/Seeds/Edit/1", "/Seeds/Delete/1", "/PlantTypes", "/PlantTypes/Create", "/PlantTypes/Edit/1", "/PlantTypes/Delete/1" })
            check((await client.GetAsync(path)).StatusCode == HttpStatusCode.OK, "Nullable crop/advance screen renders " + path);
        Dictionary<string, string> Advance(string reference, string amount = "50000", string month = "5") => new() { ["WorkerId"] = workerId.ToString(), ["Reference"] = reference, ["PaidDate"] = "2024-04-30", ["PayrollYear"] = "2024", ["PayrollMonth"] = month, ["Amount"] = amount };
        var oldToken = await Token();
        var races = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ => Post("/Advances/Create", Advance("ADV-RACE"))));
        check(races.Count(r => r.StatusCode == HttpStatusCode.Redirect) == 1 && races.Count(r => r.StatusCode == HttpStatusCode.OK) == 1, "Concurrent receipt duplicates serialize without a unique constraint");
        await Post("/Payroll/FinalizeMonth", new() { ["Year"] = "2024", ["Month"] = "5", ["ReviewToken"] = oldToken }, "/Payroll/Review?year=2024&month=5");
        await using (var db = new ApplicationDbContext(options)) check(!await db.PayrollRuns.AnyAsync(r => r.Year == 2024 && r.Month == 5), "New advances invalidate stale payroll review tokens");
        check((await Post("/Advances/Create", Advance("ADV-SECOND", "25000"))).StatusCode == HttpStatusCode.Redirect, "One worker may receive several distinct advances");
        check((await Post("/Advances/Create", Advance("INVALID", "-1"))).StatusCode == HttpStatusCode.OK, "Backend rejects negative advance requests");
        var forged = Advance("ORPHAN"); forged["WorkerId"] = "999999";
        check((await Post("/Advances/Create", forged)).StatusCode == HttpStatusCode.OK, "Backend rejects missing worker references");
        var csrf = await client.PostAsync("/Advances/Create", new FormUrlEncodedContent(Advance("NO-CSRF")));
        check(csrf.StatusCode == HttpStatusCode.BadRequest, "Advances require antiforgery protection");
        await Post("/Payroll/FinalizeMonth", new() { ["Year"] = "2024", ["Month"] = "5", ["ReviewToken"] = await Token() }, "/Payroll/Review?year=2024&month=5");
        int receiptId, runId;
        await using (var db = new ApplicationDbContext(options))
        {
            var model = await new PayrollService(db).GetMonthAsync(2024, 5);
            check(!model.SnapshotInvalid && model.Total == 300000 && model.TotalAdvances == 75000 && model.NetTotal == 225000 && model.Finalized!.CalculationVersion == 2, "Finalization persists gross, advances, net and immutable receipt snapshots");
            runId = model.Finalized!.Id;
            receiptId = (await db.WorkerAdvances.SingleAsync(a => a.Reference == "ADV-RACE")).Id;
        }
        check((await Post("/Advances/Create", Advance("LOCKED"))).StatusCode == HttpStatusCode.OK, "Closed payroll blocks new advances");
        var edited = Advance("ADV-RACE", "40000", "6"); edited["Id"] = receiptId.ToString();
        check((await Post($"/Advances/Edit/{receiptId}", edited)).StatusCode == HttpStatusCode.OK, "Closed payroll blocks moving advances to another month");
        await Post($"/Advances/Delete/{receiptId}", new() { ["id"] = receiptId.ToString() });
        await using (var db = new ApplicationDbContext(options)) check(await db.WorkerAdvances.AnyAsync(a => a.Id == receiptId), "Closed-month advances survive forged deletes");
        await Post($"/Payroll/Reopen/{runId}", new() { ["id"] = runId.ToString() });
        edited["PayrollMonth"] = "5";
        check((await Post($"/Advances/Edit/{receiptId}", edited)).StatusCode == HttpStatusCode.Redirect, "Reopening unlocks advance corrections");
        await using (var db = new ApplicationDbContext(options))
        {
            var service = new PayrollService(db);
            check((await service.GetArchiveAsync(runId))!.NetTotal == 225000, "Reopened historical snapshots retain their original deductions");
            check((await service.GetMonthAsync(2024, 5)).NetTotal == 235000, "Draft recalculates corrected advance deductions");
        }
        await Post("/Payroll/FinalizeMonth", new() { ["Year"] = "2024", ["Month"] = "5", ["ReviewToken"] = await Token() }, "/Payroll/Review?year=2024&month=5");
        await using (var db = new ApplicationDbContext(options))
        {
            var receipt = await db.WorkerAdvances.FindAsync(receiptId); receipt!.Amount = 41000; await db.SaveChangesAsync();
            check((await new PayrollService(db).GetMonthAsync(2024, 5)).SourceChanged, "Direct advance data changes are detected after finalization");
            receipt.Amount = 40000; await db.SaveChangesAsync();
            var active = await db.PayrollRuns.SingleAsync(r => r.Year == 2024 && r.Month == 5 && r.Status == PayrollService.Finalized);
            active.CalculationVersion = null; await db.SaveChangesAsync();
            check((await new PayrollService(db).GetMonthAsync(2024, 5)).SnapshotInvalid, "NULL snapshot version is not mistaken for a legacy payroll");
            active.CalculationVersion = 2; await db.SaveChangesAsync();
        }
        var csv = await client.GetStringAsync("/Payroll/Export?year=2024&month=5");
        check(csv.Contains("235000") && csv.Contains("65000") && csv.Contains("300000"), "CSV exports gross, deductions and net pay");

        check((await Post("/PlantTypes/Create", new() { ["Name"] = "Cải kiểm thử", ["GrowthCycleDays"] = "30" })).StatusCode == HttpStatusCode.Redirect, "Create crop type through MVC");
        int typeId, seedId, areaId, plantId;
        await using (var db = new ApplicationDbContext(options))
        {
            typeId = (await db.PlantTypes.SingleAsync(t => t.Name == "Cải kiểm thử")).Id;
            var area = new Area { Name = "Vùng lô kiểm thử", Acreage = 100 }; db.Areas.Add(area); await db.SaveChangesAsync(); areaId = area.Id;
        }
        check((await Post("/Seeds/Create", new() { ["Name"] = "Giống cải QA", ["Origin"] = "Nhà cung cấp QA", ["PlantTypeId"] = typeId.ToString() })).StatusCode == HttpStatusCode.Redirect, "Create seed with valid crop type");
        await using (var db = new ApplicationDbContext(options)) seedId = (await db.Seeds.SingleAsync(s => s.Name == "Giống cải QA")).Id;
        Dictionary<string, string> Plant(string code) => new() { ["BatchCode"] = code, ["AreaId"] = areaId.ToString(), ["PlantTypeId"] = typeId.ToString(), ["SeedId"] = seedId.ToString(), ["PlantingDate"] = "2024-02-01", ["Status"] = "Growing", ["Quantity"] = "100" };
        var wrong = Plant("WRONG"); wrong["PlantTypeId"] = "1";
        check((await Post("/Plants/Create", wrong)).StatusCode == HttpStatusCode.OK, "Backend rejects seed belonging to another crop type");
        var lots = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ => Post("/Plants/Create", Plant("LOT-QA"))));
        check(lots.Count(r => r.StatusCode == HttpStatusCode.Redirect) == 1 && lots.Count(r => r.StatusCode == HttpStatusCode.OK) == 1, "Concurrent duplicate lot codes serialize safely");
        await using (var db = new ApplicationDbContext(options))
        {
            var plant = await db.Plants.SingleAsync(p => p.BatchCode == "LOT-QA"); plantId = plant.Id;
            check(plant.ExpectedHarvestDate == new DateTime(2024, 3, 2), "Backend derives harvest date when omitted");
            db.PesticideApplicationLogs.Add(new() { PlantId = plantId, ApplicationDate = DateTime.Today, PHI_Days = 7 }); await db.SaveChangesAsync();
        }
        var update = Plant("LOT-QA"); update["Id"] = plantId.ToString(); update["Status"] = "Harvested";
        check((await Post($"/Plants/Edit/{plantId}", update)).StatusCode == HttpStatusCode.OK, "Backend blocks harvested status before withdrawal interval ends");
        await using (var db = new ApplicationDbContext(options))
        {
            var log = await db.PesticideApplicationLogs.SingleAsync(l => l.PlantId == plantId); log.PHI_Days = null; await db.SaveChangesAsync();
        }
        check((await Post($"/Plants/Edit/{plantId}", update)).StatusCode == HttpStatusCode.OK, "Missing withdrawal data blocks harvested status");
        await using (var db = new ApplicationDbContext(options))
        {
            var otherType = new PlantType { Name = "Loại khác" }; db.PlantTypes.Add(otherType); await db.SaveChangesAsync();
            check((await Post($"/Seeds/Edit/{seedId}", new() { ["Id"] = seedId.ToString(), ["Name"] = "Giống cải QA", ["Origin"] = "QA", ["PlantTypeId"] = otherType.Id.ToString() })).StatusCode == HttpStatusCode.OK, "Seed cannot change type while used by another crop type");
        }
        update["Status"] = "Growing"; update["ExpectedHarvestDate"] = "2024-03-10";
        check((await Post($"/Plants/Edit/{plantId}", update)).StatusCode == HttpStatusCode.Redirect, "Manual harvest estimates can override cycle estimates");
        await Post($"/PlantTypes/Delete/{typeId}", new() { ["id"] = typeId.ToString() });
        await Post($"/Seeds/Delete/{seedId}", new() { ["id"] = seedId.ToString() });
        await Post($"/Areas/Delete/{areaId}", new() { ["id"] = areaId.ToString() });
        await Post($"/Plants/Delete/{plantId}", new() { ["id"] = plantId.ToString() });
        await using (var db = new ApplicationDbContext(options))
            check(await db.PlantTypes.AnyAsync(t => t.Id == typeId) && await db.Seeds.AnyAsync(s => s.Id == seedId) && await db.Areas.AnyAsync(a => a.Id == areaId) && await db.Plants.AnyAsync(p => p.Id == plantId), "Backend preserves referenced types, seeds, areas and lots with journals without hard foreign keys");
        check((await client.GetAsync($"/Plants/Details/{plantId}")).StatusCode == HttpStatusCode.OK, "Crop tracking displays recorded farming journals");

        async Task<string> Token() => Regex.Match(await client.GetStringAsync("/Payroll/Review?year=2024&month=5"), "name=\"ReviewToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;
        async Task<HttpResponseMessage> Post(string route, Dictionary<string, string> fields, string? source = null)
        {
            var tokenRoute = source ?? (route == "/Payroll/FinalizeMonth" ? "/Payroll/Review?year=2024&month=5" : route);
            var html = await client.GetStringAsync(tokenRoute);
            fields["__RequestVerificationToken"] = WebUtility.HtmlDecode(Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
            if (fields["__RequestVerificationToken"].Length == 0) throw new Exception("Missing token: " + route);
            TestChangeFields.Apply(fields,html);
            return await client.PostAsync(route, new FormUrlEncodedContent(fields));
        }
    }
}
