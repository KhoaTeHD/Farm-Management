using Farm_Management.Services;
using System.Globalization;
using Farm_Management.Data;
using Farm_Management.Models;
using Farm_Management.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Farm_Management.Controllers;

public class AttendanceController(ApplicationDbContext context) : Controller
{
    public async Task<IActionResult> Index(DateTime? date, bool missingDate = false)
    {
        var selectedDate = (date ?? DateTime.Today).Date;
        if (selectedDate.Year < 2 || selectedDate.Year > 9998) return BadRequest("Ngày không hợp lệ");
        ViewBag.MissingDate = missingDate;
        ViewBag.SelectedDate = selectedDate;
        var nextDate = selectedDate == DateTime.MaxValue.Date ? selectedDate : selectedDate.AddDays(1);
        var logs = await context.AttendanceLogs
            .Where(a => missingDate ? a.Date == null : a.Date >= selectedDate && a.Date < nextDate).ToListAsync();
        await LoadWorkersAsync(logs);
        var validLogs = logs.Where(l => l.Worker != null && l.WorkerId.HasValue && l.Date.HasValue)
            .GroupBy(l => (l.WorkerId, l.Date!.Value.Date))
            .Where(g => g.Count() == 1).Select(g => g.Single())
            .Where(l => AttendanceRules.IsValidHours(l.HoursWorked)).ToList();
        ViewBag.ValidLogs = validLogs;
        ViewBag.DataWarning = validLogs.Count != logs.Count
            ? "Có bản ghi thiếu/sai dữ liệu hoặc trùng công nhân trong ngày. Tổng bên dưới chỉ tính bản ghi hợp lệ, không trùng. Hãy sửa hoặc xóa các bản ghi cần điều chỉnh."
            : null;
        var workerIds = logs.Where(l => l.WorkerId.HasValue).Select(l => l.WorkerId!.Value).ToList();
        ViewBag.AvailableWorkers = missingDate ? new List<Worker>() : await context.Workers
            .Where(w => w.IsActive == true && !workerIds.Contains(w.Id))
            .OrderBy(w => w.Code).ToListAsync();
        ViewBag.PayrollLocked = await PayrollService.IsLockedAsync(context, selectedDate);
        if (ViewBag.PayrollLocked == true) ViewBag.AvailableWorkers = new List<Worker>();
        return View(logs.OrderBy(l => l.Worker?.Code).ThenBy(l => l.Id).ToList());
    }

    public async Task<IActionResult> Calendar(int? year, int? month)
    {
        var selectedYear = year ?? DateTime.Today.Year;
        var selectedMonth = month ?? DateTime.Today.Month;
        if (selectedYear < 2 || selectedYear > 9998 || selectedMonth < 1 || selectedMonth > 12)
            return BadRequest("Tháng hoặc năm không hợp lệ");
        var start = new DateTime(selectedYear, selectedMonth, 1);
        ViewBag.PayrollLocked = await PayrollService.IsLockedAsync(context, start);
        var end = start.AddMonths(1);
        var logs = await context.AttendanceLogs.Where(a => a.Date >= start && a.Date < end)
            .OrderBy(a => a.Id).ToListAsync();
        // Include former workers with attendance in this month so historical totals remain visible.
        var ids = logs.Where(l => l.WorkerId.HasValue).Select(l => l.WorkerId!.Value).ToList();
        var workers = await context.Workers.Where(w => w.IsActive == true || ids.Contains(w.Id))
            .OrderBy(w => w.Code).ToListAsync();
        var groups = logs.Where(l => l.WorkerId.HasValue && l.Date.HasValue)
            .GroupBy(l => (WorkerId: l.WorkerId!.Value, Day: l.Date!.Value.Day)).ToList();
        var duplicates = groups.Where(g => g.Count() > 1).ToList();
        // Duplicate cells must be repaired from the list, never silently overwritten.
        var invalidCount = logs.Count(l => !l.WorkerId.HasValue || !workers.Any(w => w.Id == l.WorkerId)
            || !AttendanceRules.IsValidHours(l.HoursWorked));
        ViewBag.DataWarning = duplicates.Count > 0 || invalidCount > 0
            ? $"Có {duplicates.Count} ô chấm công bị trùng và {invalidCount} bản ghi thiếu hoặc sai dữ liệu. Hãy mở danh sách theo ngày để sửa; tổng chỉ tính bản ghi hợp lệ, không trùng."
            : null;
        return View(new AttendanceCalendarViewModel
        {
            Year = selectedYear,
            Month = selectedMonth,
            DaysInMonth = DateTime.DaysInMonth(selectedYear, selectedMonth),
            Workers = workers,
            Logs = groups.Where(g => g.Count() == 1 && workers.Any(w => w.Id == g.Key.WorkerId))
                .ToDictionary(g => g.Key, g => g.Single()),
            DuplicateCells = duplicates.Select(g => g.Key).ToHashSet()
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveCell(int? workerId, string? date, decimal? hours)
    {
        if (!ModelState.IsValid || !AttendanceRules.IsValidHours(hours))
            return Json(new { success = false, message = "Số giờ từ 0 đến 24, theo bước 0.5 giờ" });
        if (!DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var day) || day.Year < 2 || day.Year > 9998)
            return Json(new { success = false, message = "Ngày không hợp lệ" });
        await using var transaction = await context.BeginBusinessWriteAsync();
        if (await PayrollService.IsLockedAsync(context, day))
            return Json(new { success = false, message = PayrollService.LockedMessage });
        var worker = workerId.HasValue ? await context.Workers.FindAsync(workerId.Value) : null;
        if (worker == null)
            return Json(new { success = false, message = "Công nhân không tồn tại" });
        var logs = await context.AttendanceLogs.Where(a => a.WorkerId == workerId
            && a.Date >= day && a.Date < day.AddDays(1)).ToListAsync();
        if (logs.Count > 1)
            return Json(new { success = false, message = "Ngày này có nhiều bản ghi. Hãy sửa trong danh sách chấm công." });
        var log = logs.SingleOrDefault();
        if (worker.IsActive != true && log == null)
            return Json(new { success = false, message = "Không thể tạo chấm công mới cho công nhân đã nghỉ hoặc chưa có trạng thái" });
        if (hours == 0)
        {
            if (log != null) context.AttendanceLogs.Remove(log);
        }
        else
        {
            if (log == null)
            {
                log = new AttendanceLog { WorkerId = workerId, Date = day };
                context.AttendanceLogs.Add(log);
            }
            log.Date = day;
            log.HoursWorked = hours;
            log.WorkType = AttendanceRules.Classify(hours);
        }
        await context.SaveChangesAsync();
        await transaction.CommitAsync();
        return Json(new { success = true, workType = hours == 0 ? "" : AttendanceRules.Classify(hours) });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAttendance(DateTime? date, int[] workerIds, decimal[] hours, string[]? notes)
    {
        if (!ModelState.IsValid || !date.HasValue || date.Value.Year < 2 || date.Value.Year > 9998
            || workerIds == null || workerIds.Length == 0 || hours == null
            || hours.Length != workerIds.Length || (notes != null && notes.Length != workerIds.Length))
        {
            TempData["Error"] = "Dữ liệu chấm công chưa đầy đủ hoặc không hợp lệ";
            return RedirectToAction(nameof(Index), new { date });
        }
        var day = date.Value.Date;
        await using var transaction = await context.BeginBusinessWriteAsync();
        if (await PayrollService.IsLockedAsync(context, day))
        {
            TempData["Error"] = PayrollService.LockedMessage;
            return RedirectToAction(nameof(Index), new { date });
        }
        var workers = await context.Workers.Where(w => workerIds.Contains(w.Id) && w.IsActive == true)
            .Select(w => w.Id).ToListAsync();
        var recordedIds = await context.AttendanceLogs.Where(a => a.Date >= day && a.Date < day.AddDays(1))
            .Select(a => a.WorkerId).ToListAsync();
        var seen = new HashSet<int>();
        var errors = new List<string>();
        var count = 0;
        for (var i = 0; i < workerIds.Length; i++)
        {
            var id = workerIds[i];
            var note = notes?[i];
            if (!seen.Add(id) || !workers.Contains(id) || recordedIds.Contains(id)
                || !AttendanceRules.IsValidHours(hours[i]) || note?.Length > 500)
            {
                errors.Add($"Công nhân ID {id}: Trùng chấm công, không đang làm việc hoặc dữ liệu không hợp lệ");
                continue;
            }
            context.AttendanceLogs.Add(new AttendanceLog
            {
                WorkerId = id, Date = day, HoursWorked = hours[i],
                WorkType = AttendanceRules.Classify(hours[i]), Notes = note
            });
            count++;
        }
        await context.SaveChangesAsync();
        await transaction.CommitAsync();
        if (count > 0) TempData["Success"] = $"Đã chấm công {count} công nhân";
        if (errors.Count > 0) TempData["Error"] = string.Join("; ", errors);
        return RedirectToAction(nameof(Index), new { date = day });
    }

    public async Task<IActionResult> Create(DateTime? date)
    {
        var form = new AttendanceLogForm { Date = (date ?? DateTime.Today).Date, HoursWorked = 8 };
        await LoadFormAsync(form);
        return View(form);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("WorkerId,Date,HoursWorked,Notes")] AttendanceLogForm form)
    {
        await using var transaction = await context.BeginBusinessWriteAsync();
        await ValidateAttendanceAsync(form, null);
        if (!ModelState.IsValid)
        {
            await LoadFormAsync(form);
            return View(form);
        }
        var log = form.ToEntity();
        log.Date = form.Date!.Value.Date;
        log.WorkType = AttendanceRules.Classify(log.HoursWorked);
        context.AttendanceLogs.Add(log);
        await context.SaveChangesAsync();
        await transaction.CommitAsync();
        TempData["Success"] = "Chấm công thành công!";
        return RedirectToAction(nameof(Index), new { date = log.Date });
    }

    public async Task<IActionResult> Edit(int? id)
    {
        var log = id.HasValue ? await context.AttendanceLogs.FindAsync(id.Value) : null;
        if (log == null) return NotFound();
        var form = AttendanceLogForm.FromEntity(log);
        await LoadFormAsync(form);
        return View(form);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,WorkerId,Date,HoursWorked,Notes")] AttendanceLogForm form)
    {
        if (id != form.Id) return NotFound();
        await using var transaction = await context.BeginBusinessWriteAsync();
        var existing = await context.AttendanceLogs.FindAsync(id);
        if (existing == null) return NotFound();
        await ValidateAttendanceAsync(form, existing);
        if (!ModelState.IsValid)
        {
            await LoadFormAsync(form);
            return View(form);
        }
        existing.WorkerId = form.WorkerId;
        existing.Date = form.Date!.Value.Date;
        existing.HoursWorked = form.HoursWorked;
        existing.WorkType = AttendanceRules.Classify(form.HoursWorked);
        existing.Notes = form.Notes;
        await context.SaveChangesAsync();
        await transaction.CommitAsync();
        TempData["Success"] = "Cập nhật chấm công thành công!";
        return RedirectToAction(nameof(Index), new { date = existing.Date });
    }

    public async Task<IActionResult> Delete(int? id)
    {
        var log = id.HasValue ? await context.AttendanceLogs.FindAsync(id.Value) : null;
        if (log == null) return NotFound();
        await LoadWorkersAsync(new[] { log });
        ViewBag.PayrollLocked = log.Date.HasValue && await PayrollService.IsLockedAsync(context, log.Date.Value);
        return View(log);
    }

    public async Task<IActionResult> Details(int? id)
    {
        var log = id.HasValue ? await context.AttendanceLogs.FindAsync(id.Value) : null;
        if (log == null) return NotFound();
        await LoadWorkersAsync(new[] { log });
        return View(log);
    }

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        await using var transaction = await context.BeginBusinessWriteAsync();
        var log = await context.AttendanceLogs.FindAsync(id);
        if (log == null) return NotFound();
        var date = log.Date;
        if (date.HasValue && await PayrollService.IsLockedAsync(context, date.Value))
        {
            TempData["Error"] = PayrollService.LockedMessage;
            return RedirectToAction(nameof(Index), new { date });
        }
        context.AttendanceLogs.Remove(log);
        await context.SaveChangesAsync();
        await transaction.CommitAsync();
        TempData["Success"] = "Xóa chấm công thành công!";
        return RedirectToAction(nameof(Index), new { date });
    }

    private async Task LoadWorkersAsync(IEnumerable<AttendanceLog> logs)
    {
        var ids = logs.Where(l => l.WorkerId.HasValue).Select(l => l.WorkerId!.Value).Distinct().ToList();
        var workers = await context.Workers.Where(w => ids.Contains(w.Id)).ToDictionaryAsync(w => w.Id);
        foreach (var log in logs)
            log.Worker = log.WorkerId.HasValue && workers.TryGetValue(log.WorkerId.Value, out var worker) ? worker : null;
    }

    private async Task ValidateAttendanceAsync(AttendanceLogForm form, AttendanceLog? existing)
    {
        if ((form.Date.HasValue && await PayrollService.IsLockedAsync(context, form.Date.Value))
            || (existing?.Date.HasValue == true && await PayrollService.IsLockedAsync(context, existing.Date.Value)))
            ModelState.AddModelError("", PayrollService.LockedMessage);
        if (!form.Date.HasValue || form.Date.Value.Year < 2 || form.Date.Value.Year > 9998)
            ModelState.AddModelError("Date", "Ngày chấm công không hợp lệ");
        var worker = form.WorkerId.HasValue ? await context.Workers.FindAsync(form.WorkerId.Value) : null;
        if (worker == null || (worker.IsActive != true && existing?.WorkerId != worker.Id))
            ModelState.AddModelError("WorkerId", "Công nhân không tồn tại hoặc không đang làm việc");
        if (form.Date.HasValue && form.Date.Value.Year >= 2 && form.Date.Value.Year <= 9998)
        {
            var day = form.Date.Value.Date;
            if (await context.AttendanceLogs.AnyAsync(a => a.Id != form.Id && a.WorkerId == form.WorkerId
                && a.Date >= day && a.Date < day.AddDays(1)))
                ModelState.AddModelError("", "Công nhân đã được chấm công vào ngày này");
        }
    }

    private async Task LoadFormAsync(AttendanceLogForm form)
    {
        ViewBag.SelectedDate = form.Date;
        ViewBag.PayrollLocked = form.Date.HasValue && await PayrollService.IsLockedAsync(context, form.Date.Value);
        form.Worker = form.WorkerId.HasValue ? await context.Workers.FindAsync(form.WorkerId.Value) : null;
        var workers = await context.Workers.Where(w => w.IsActive == true || w.Id == form.WorkerId)
            .OrderBy(w => w.Code).ToListAsync();
        ViewBag.WorkerSalaries = workers.ToDictionary(w => w.Id.ToString(), w => w.DailySalary);
        ViewBag.WorkerId = new SelectList(workers.Select(w => new { w.Id, Label = (w.Code ?? "Chưa có mã") + " · " + (w.FullName ?? "Chưa có tên") }), "Id", "Label", form.WorkerId);
    }
}
