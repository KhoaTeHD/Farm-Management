using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Farm_Management.Data;
using Farm_Management.Models;
using Farm_Management.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Farm_Management.Services;

public class PayrollService(ApplicationDbContext context)
{
    public const string Finalized = "Finalized";
    public const string Reopened = "Reopened";
    public const string LockedMessage = "Tháng này đã chốt lương hoặc có bảng lương cần kiểm tra. Hãy mở lại bảng lương trước khi sửa chấm công hoặc tạm ứng.";

    public static bool ValidPeriod(int year, int month) => year is >= 2 and <= 9998 && month is >= 1 and <= 12;
    public static decimal Money(decimal amount) => decimal.Round(amount, 0, MidpointRounding.AwayFromZero);
    public static bool ValidSalary(decimal? salary) => salary is >= 1 and <= 10000000;

    // Called only after BeginBusinessWriteAsync in write endpoints, sharing one lock with finalization.
    public static Task<bool> IsLockedAsync(ApplicationDbContext db, DateTime date) => db.PayrollRuns.AnyAsync(r =>
        r.Year == date.Year && r.Month == date.Month && r.Status != Reopened);

    public static PayrollMonthViewModel Calculate(int year, int month, IEnumerable<AttendanceLog> source,
        IReadOnlyDictionary<int, Worker> workers, int missingDates = 0, IEnumerable<WorkerAdvance>? advances = null, int missingAdvancePeriods = 0)
    {
        if (!ValidPeriod(year, month)) throw new ArgumentOutOfRangeException(nameof(month));
        var model = new PayrollMonthViewModel { Year = year, Month = month };
        var start = new DateTime(year, month, 1);
        var end = start.AddMonths(1);
        var logs = source.Where(l => l.Date >= start && l.Date < end).OrderBy(l => l.Id).ToList();
        model.AttendanceHash = Hash(JsonSerializer.Serialize(logs.Select(l => new { l.Id, l.WorkerId, l.Date, l.HoursWorked })));
        if (missingDates > 0)
            model.Issues.Add(new($"Có {missingDates} bản ghi chấm công chưa có ngày nên chưa xác định được tháng. Bổ sung ngày trước khi chốt lương."));
        foreach (var group in logs.GroupBy(l => l.WorkerId))
        {
            if (!group.Key.HasValue || !workers.TryGetValue(group.Key.Value, out var worker))
            {
                foreach (var log in group) model.Issues.Add(new($"Chấm công #{log.Id} thiếu công nhân hoặc tham chiếu không tồn tại.", Date: log.Date));
                continue;
            }
            var row = new PayrollRow
            {
                WorkerId = worker.Id,
                Code = worker.Code,
                Name = worker.FullName,
                DailySalary = worker.DailySalary,
                AttendanceCount = group.Count()
            };
            if (string.IsNullOrWhiteSpace(worker.Code) || string.IsNullOrWhiteSpace(worker.FullName))
                row.Errors.Add("Thiếu mã hoặc họ tên công nhân.");
            if (!string.IsNullOrWhiteSpace(worker.Code) && workers.Values.Count(w => string.Equals(w.Code?.Trim(), worker.Code.Trim(), StringComparison.OrdinalIgnoreCase)) > 1)
                row.Errors.Add("Mã công nhân bị trùng trong danh sách tính lương.");
            if (!ValidSalary(worker.DailySalary)) row.Errors.Add("Lương ngày phải từ 1 đến 10.000.000 đ.");
            foreach (var day in group.GroupBy(l => l.Date!.Value.Date).OrderBy(g => g.Key))
            {
                if (day.Count() != 1)
                {
                    var error = $"Trùng {day.Count()} bản ghi ngày {day.Key:dd/MM/yyyy}.";
                    row.Errors.Add(error); model.Issues.Add(new(error, worker.Id, day.Key));
                    continue;
                }
                var log = day.Single();
                if (!AttendanceRules.IsValidHours(log.HoursWorked))
                {
                    var error = $"Số giờ ngày {day.Key:dd/MM/yyyy} thiếu hoặc không hợp lệ.";
                    row.Errors.Add(error); model.Issues.Add(new(error, worker.Id, day.Key));
                    continue;
                }
                var hours = log.HoursWorked!.Value;
                row.Days.Add(new() { AttendanceId = log.Id, Date = day.Key, Hours = hours });
                row.TotalHours += hours;
                switch (AttendanceRules.Classify(hours))
                {
                    case "FullDay": row.FullDays++; break;
                    case "HalfDay": row.HalfDays++; break;
                    default: row.OddHours += hours; break;
                }
            }
            // Never publish a partially calculated worker's pay as their full monthly amount.
            if (row.Errors.Count == 0)
                row.Amount = Money(worker.DailySalary!.Value * (row.WorkDays + row.OddHours / 8m));
            else
                foreach (var error in row.Errors.Where(e => !model.Issues.Any(i => i.WorkerId == worker.Id && i.Message == e)))
                    model.Issues.Add(new(error, worker.Id));
            model.Rows.Add(row);
        }
        ApplyAdvances(model, advances ?? Array.Empty<WorkerAdvance>(), workers, missingAdvancePeriods);
        model.Rows = model.Rows.OrderBy(r => r.Code).ThenBy(r => r.WorkerId).ToList();
        model.ReviewToken = Hash(model.AttendanceHash + model.AdvanceHash + missingAdvancePeriods + JsonSerializer.Serialize(model.Rows.Select(r => new { r.WorkerId, r.Code, r.Name, r.DailySalary })) + missingDates);
        return model;
    }

    public async Task<PayrollMonthViewModel> GetMonthAsync(int year, int month)
    {
        var start = new DateTime(year, month, 1);
        var end = start.AddMonths(1);
        var logs = await context.AttendanceLogs.AsNoTracking().Where(l => l.Date >= start && l.Date < end).ToListAsync();
        var advances = await context.WorkerAdvances.AsNoTracking().Where(a => a.PayrollYear == year && a.PayrollMonth == month).ToListAsync();
        var unassigned = await context.WorkerAdvances.CountAsync(a => a.PayrollYear == null || a.PayrollYear < 2 || a.PayrollYear > 9998 || a.PayrollMonth == null || a.PayrollMonth < 1 || a.PayrollMonth > 12);
        var ids = logs.Where(l => l.WorkerId.HasValue).Select(l => l.WorkerId!.Value).Concat(advances.Where(a => a.WorkerId.HasValue).Select(a => a.WorkerId!.Value)).Distinct().ToList();
        var workers = await context.Workers.AsNoTracking().Where(w => ids.Contains(w.Id)).ToDictionaryAsync(w => w.Id);
        var model = Calculate(year, month, logs, workers, await context.AttendanceLogs.CountAsync(l => l.Date == null), advances, unassigned);
        model.History = await context.PayrollRuns.AsNoTracking().Where(r => r.Year == year && r.Month == month).OrderByDescending(r => r.Id).ToListAsync();
        var active = model.History.Where(r => r.Status != Reopened).ToList();
        model.ConflictingRuns = active.Count > 1 || active.Any(r => r.Status != Finalized);
        if (model.ConflictingRuns)
            model.Issues.Add(new("Bảng lương lưu trữ bị trùng hoặc thiếu trạng thái. Cần kiểm tra dữ liệu trước khi chốt thêm."));
        if (active.Count == 1 && active[0].Status == Finalized)
        {
            await LoadSnapshotAsync(model, active[0]);
        }
        return model;
    }

    public async Task<PayrollMonthViewModel?> GetArchiveAsync(int id)
    {
        var run = await context.PayrollRuns.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);
        if (run?.Year == null || run.Month == null || !ValidPeriod(run.Year.Value, run.Month.Value)) return null;
        var model = await GetMonthAsync(run.Year.Value, run.Month.Value);
        model.IsArchive = true;
        await LoadSnapshotAsync(model, run);
        return model;
    }

    private async Task LoadSnapshotAsync(PayrollMonthViewModel model, PayrollRun run)
    {
        model.Finalized = run;
        model.SourceChanged = model.AttendanceHash != run.AttendanceHash || (run.CalculationVersion == 2 ? model.AdvanceHash != run.AdvanceHash : model.AdvanceRecordCount > 0);
        var entries = await context.PayrollEntries.AsNoTracking().Where(e => e.PayrollRunId == run.Id).OrderBy(e => e.WorkerCode).ToListAsync();
        var snapshot = new List<PayrollRow>();
        model.SnapshotInvalid = (run.CalculationVersion != 1 && run.CalculationVersion != 2) || (run.CalculationVersion == 2 && (run.AdvanceHash?.Length != 64 || !run.TotalAdvances.HasValue || !run.NetTotal.HasValue)) || (run.Status != Finalized && run.Status != Reopened) || run.AttendanceHash?.Length != 64 || !model.Finalized.FinalizedAt.HasValue || entries.Count == 0
            || model.Finalized.WorkerCount != entries.Count || entries.GroupBy(e => e.WorkerId).Any(g => g.Count() != 1);
        foreach (var entry in entries)
        {
            var row = ReadEntry(entry, model.Year, model.Month, run.CalculationVersion ?? -1);
            if (row == null) model.SnapshotInvalid = true;
            else snapshot.Add(row);
        }
        if (snapshot.Sum(r => r.Amount!.Value) != model.Finalized.TotalAmount) model.SnapshotInvalid = true;
        if (run.CalculationVersion == 2 && (snapshot.Sum(r => r.AdvanceAmount!.Value) != run.TotalAdvances || snapshot.Sum(r => r.NetAmount!.Value) != run.NetTotal)) model.SnapshotInvalid = true;
        model.Rows = snapshot;
    }

    public static PayrollRow? ReadEntry(PayrollEntry entry, int year, int month, int version = 2)
    {
        if (!entry.WorkerId.HasValue || entry.WorkerId <= 0 || !ValidSalary(entry.DailySalary) || string.IsNullOrWhiteSpace(entry.WorkerCode)
            || string.IsNullOrWhiteSpace(entry.WorkerName) || !entry.FullDays.HasValue || !entry.HalfDays.HasValue
            || !entry.OddHours.HasValue || !entry.TotalHours.HasValue || !entry.Amount.HasValue
            || !entry.AttendanceCount.HasValue || entry.AttendanceJson == null) return null;
        List<PayrollDay>? days;
        try { days = JsonSerializer.Deserialize<List<PayrollDay>>(entry.AttendanceJson); }
        catch (JsonException) { return null; }
        if (days == null || days.Count == 0 || days.Count != entry.AttendanceCount
            || days.Any(d => d == null || d.AttendanceId <= 0 || d.Date.Year != year || d.Date.Month != month || !AttendanceRules.IsValidHours(d.Hours))
            || days.GroupBy(d => d.Date.Date).Any(g => g.Count() != 1) || days.GroupBy(d => d.AttendanceId).Any(g => g.Count() != 1)) return null;
        var full = days.Count(d => AttendanceRules.Classify(d.Hours) == "FullDay");
        var half = days.Count(d => AttendanceRules.Classify(d.Hours) == "HalfDay");
        var odd = days.Where(d => AttendanceRules.Classify(d.Hours) == "OddHours").Sum(d => d.Hours);
        if (entry.FullDays != full || entry.HalfDays != half || entry.OddHours != odd
            || entry.TotalHours != days.Sum(d => d.Hours)
            || entry.Amount != Money(entry.DailySalary!.Value * (full + half * 0.5m + odd / 8m))) return null;
        if (version != 1 && version != 2) return null;
        var deductions = new List<PayrollAdvanceSnapshot>();
        decimal advanceAmount = 0;
        if (version == 2)
        {
            if (!entry.AdvanceAmount.HasValue || !entry.NetAmount.HasValue || entry.AdvancesJson == null) return null;
            try { deductions = JsonSerializer.Deserialize<List<PayrollAdvanceSnapshot>>(entry.AdvancesJson)!; }
            catch (JsonException) { return null; }
            if (deductions == null || deductions.Any(a => a == null || a.AdvanceId <= 0 || string.IsNullOrWhiteSpace(a.Reference) || a.PaidDate.Year < 2 || !ValidAdvanceAmount(a.Amount))
                || deductions.GroupBy(a => a.AdvanceId).Any(g => g.Count() > 1) || deductions.GroupBy(a => a.Reference.Trim().ToUpperInvariant()).Any(g => g.Count() > 1)) return null;
            advanceAmount = deductions.Sum(a => a.Amount);
            if (entry.AdvanceAmount != advanceAmount || entry.NetAmount != entry.Amount - advanceAmount || entry.NetAmount < 0) return null;
        }
        return new PayrollRow
        {
            WorkerId = entry.WorkerId.Value,
            Code = entry.WorkerCode,
            Name = entry.WorkerName,
            DailySalary = entry.DailySalary,
            FullDays = full,
            HalfDays = half,
            OddHours = odd,
            TotalHours = entry.TotalHours.Value,
            Amount = entry.Amount,
            AdvanceAmount = advanceAmount,
            NetAmount = entry.Amount - advanceAmount,
            Advances = deductions,
            AttendanceCount = days.Count,
            Days = days
        };
    }

    public static PayrollEntry Snapshot(PayrollRow row, int runId) => new()
    {
        PayrollRunId = runId,
        WorkerId = row.WorkerId,
        WorkerCode = row.Code,
        WorkerName = row.Name,
        DailySalary = row.DailySalary,
        FullDays = row.FullDays,
        HalfDays = row.HalfDays,
        OddHours = row.OddHours,
        TotalHours = row.TotalHours,
        AttendanceCount = row.AttendanceCount,
        Amount = row.Amount,
        AttendanceJson = JsonSerializer.Serialize(row.Days),
        AdvanceAmount = row.AdvanceAmount,
        NetAmount = row.NetAmount,
        AdvancesJson = JsonSerializer.Serialize(row.Advances)
    };

    public static bool ValidAdvanceAmount(decimal? amount) => amount is >= 1 and <= 1000000000 && decimal.Truncate(amount.Value) == amount;

    private static void ApplyAdvances(PayrollMonthViewModel model, IEnumerable<WorkerAdvance> source, IReadOnlyDictionary<int, Worker> workers, int unassigned)
    {
        var advances = source.Where(a => a.PayrollYear == model.Year && a.PayrollMonth == model.Month).OrderBy(a => a.Id).ToList();
        model.AdvanceRecordCount = advances.Count;
        model.AdvanceHash = Hash(JsonSerializer.Serialize(advances.Select(a => new { a.Id, a.WorkerId, a.Reference, a.PaidDate, a.PayrollYear, a.PayrollMonth, a.Amount })));
        if (unassigned > 0) model.Issues.Add(new($"Có {unassigned} phiếu tạm ứng chưa có tháng khấu trừ hợp lệ.", Advance: true));
        var duplicateReferences = advances.Where(a => !string.IsNullOrWhiteSpace(a.Reference)).GroupBy(a => a.Reference!.Trim().ToUpperInvariant()).Where(g => g.Count() > 1).Select(g => g.Key).ToHashSet();
        foreach (var group in advances.GroupBy(a => a.WorkerId))
        {
            if (!group.Key.HasValue || !workers.TryGetValue(group.Key.Value, out var worker))
            {
                model.Issues.Add(new("Phiếu tạm ứng thiếu công nhân hoặc tham chiếu không tồn tại.", Advance: true)); continue;
            }
            var row = model.Rows.FirstOrDefault(r => r.WorkerId == worker.Id);
            if (row == null)
            {
                row = new PayrollRow { WorkerId = worker.Id, Code = worker.Code, Name = worker.FullName, DailySalary = worker.DailySalary, Amount = 0 };
                model.Rows.Add(row);
            }
            foreach (var advance in group)
            {
                if (!ValidAdvanceAmount(advance.Amount) || !advance.PaidDate.HasValue || advance.PaidDate.Value.Year < 2 || advance.PaidDate.Value.Date > DateTime.Today
                    || string.IsNullOrWhiteSpace(advance.Reference) || duplicateReferences.Contains(advance.Reference.Trim().ToUpperInvariant()))
                {
                    row.AdvancesInvalid = true; row.Errors.Add($"Phiếu tạm ứng #{advance.Id} thiếu/sai dữ liệu hoặc bị trùng mã."); continue;
                }
                row.Advances.Add(new() { AdvanceId = advance.Id, Reference = advance.Reference, PaidDate = advance.PaidDate.Value.Date, Amount = advance.Amount!.Value });
            }
        }
        foreach (var row in model.Rows)
        {
            row.AdvanceAmount = row.AdvancesInvalid ? null : row.Advances.Sum(a => a.Amount);
            if (row.Errors.Count == 0 && row.Amount.HasValue)
            {
                if (row.AdvanceAmount > row.Amount) row.Errors.Add("Tổng tạm ứng vượt lương tháng. Cần điều chỉnh tháng khấu trừ trước khi chốt; không tự chuyển nợ.");
                else row.NetAmount = row.Amount - row.AdvanceAmount;
            }
            if (row.Errors.Count > 0)
            {
                row.NetAmount = null;
                foreach (var error in row.Errors.Where(e => !model.Issues.Any(i => i.WorkerId == row.WorkerId && i.Message == e)))
                    model.Issues.Add(new(error, row.WorkerId, Advance: true));
            }
        }
    }

    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
