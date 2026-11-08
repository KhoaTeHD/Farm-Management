using System.Globalization;
using System.Text;
using Farm_Management.Data;
using Farm_Management.Models;
using Farm_Management.Models.ViewModels;
using Farm_Management.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Farm_Management.Controllers;

public class PayrollController(ApplicationDbContext context, PayrollService payroll) : Controller
{
    public async Task<IActionResult> Index(int? year, int? month)
    {
        var y = year ?? DateTime.Today.Year;
        var m = month ?? DateTime.Today.Month;
        if (!ModelState.IsValid || !PayrollService.ValidPeriod(y, m)) return BadRequest("Tháng hoặc năm không hợp lệ");
        // Use the application write scope for a coherent multi-query view while other requests edit data.
        await using var transaction = await context.BeginBusinessWriteAsync();
        var model = await payroll.GetMonthAsync(y, m);
        await transaction.CommitAsync();
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> FinalizeMonth(PayrollMonthForm form)
    {
        if (!ModelState.IsValid) return BadRequest("Thông tin chốt lương chưa hợp lệ");
        await using var transaction = await context.BeginBusinessWriteAsync();
        var model = await payroll.GetMonthAsync(form.Year!.Value, form.Month!.Value);
        if (!model.CanFinalize)
        {
            TempData["Error"] = model.Finalized != null ? "Tháng này đã được chốt lương." : "Chưa thể chốt: cần có chấm công hợp lệ và xử lý đầy đủ dữ liệu thiếu hoặc trùng.";
            return RedirectToAction(nameof(Index), new { year = form.Year, month = form.Month });
        }
        if (!string.Equals(form.ReviewToken, model.ReviewToken, StringComparison.Ordinal))
        {
            TempData["Error"] = "Dữ liệu chấm công, mức lương hoặc tạm ứng đã thay đổi. Hãy kiểm tra bảng lương mới trước khi chốt.";
            return RedirectToAction(nameof(Index), new { year = form.Year, month = form.Month });
        }
        var run = new PayrollRun
        {
            Year = form.Year,
            Month = form.Month,
            Status = PayrollService.Finalized,
            FinalizedAt = DateTime.Now,
            WorkerCount = model.Rows.Count,
            TotalAmount = model.Total,
            AttendanceHash = model.AttendanceHash, CalculationVersion = 2, AdvanceHash = model.AdvanceHash, TotalAdvances = model.TotalAdvances, NetTotal = model.NetTotal
        };
        context.PayrollRuns.Add(run);
        await context.SaveChangesAsync();
        context.PayrollEntries.AddRange(model.Rows.Select(r => PayrollService.Snapshot(r, run.Id)));
        await context.SaveChangesAsync();
        await transaction.CommitAsync();
        TempData["Success"] = $"Đã chốt bảng lương tháng {form.Month:00}/{form.Year}. Mức lương và dữ liệu tính công đã được lưu riêng.";
        return RedirectToAction(nameof(Index), new { year = form.Year, month = form.Month });
    }

    public async Task<IActionResult> Review(int year, int month)
    {
        if (!ModelState.IsValid || !PayrollService.ValidPeriod(year, month)) return BadRequest("Tháng hoặc năm không hợp lệ");
        await using var transaction = await context.BeginBusinessWriteAsync();
        var model = await payroll.GetMonthAsync(year, month);
        await transaction.CommitAsync();
        return View(model);
    }

    public async Task<IActionResult> Archive(int id)
    {
        await using var transaction = await context.BeginBusinessWriteAsync();
        var model = await payroll.GetArchiveAsync(id);
        await transaction.CommitAsync();
        return model == null ? NotFound() : View("Index", model);
    }

    public async Task<IActionResult> Reopen(int? id)
    {
        var run = id.HasValue ? await context.PayrollRuns.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id) : null;
        if (run == null || !run.Year.HasValue || !run.Month.HasValue || !PayrollService.ValidPeriod(run.Year.Value, run.Month.Value)) return NotFound();
        return View(run);
    }

    [HttpPost, ActionName("Reopen"), ValidateAntiForgeryToken]
    public async Task<IActionResult> ReopenConfirmed(int id)
    {
        await using var transaction = await context.BeginBusinessWriteAsync();
        var run = await context.PayrollRuns.FindAsync(id);
        if (run == null || !run.Year.HasValue || !run.Month.HasValue || !PayrollService.ValidPeriod(run.Year.Value, run.Month.Value)) return NotFound();
        if (run.Status != PayrollService.Finalized)
        {
            TempData["Error"] = "Bảng lương này không còn ở trạng thái đã chốt.";
            return RedirectToAction(nameof(Index), new { year = run.Year, month = run.Month });
        }
        run.Status = PayrollService.Reopened;
        run.ReopenedAt = DateTime.Now;
        await context.SaveChangesAsync();
        await transaction.CommitAsync();
        TempData["Success"] = "Đã mở lại tháng để điều chỉnh chấm công và tạm ứng. Bản chốt cũ vẫn được giữ trong lịch sử.";
        return RedirectToAction(nameof(Index), new { year = run.Year, month = run.Month });
    }

    public async Task<IActionResult> Export(int year, int month)
    {
        if (!ModelState.IsValid || !PayrollService.ValidPeriod(year, month)) return BadRequest("Tháng hoặc năm không hợp lệ");
        await using var transaction = await context.BeginBusinessWriteAsync();
        var model = await payroll.GetMonthAsync(year, month);
        if (model.ConflictingRuns || model.SnapshotInvalid || (model.Finalized == null && !model.CanFinalize))
            return BadRequest("Chưa thể xuất bảng lương khi dữ liệu thiếu, trùng hoặc không hợp lệ.");
        var csv = new StringBuilder();
        csv.AppendLine("Trạng thái,Mã công nhân,Họ tên,Lương ngày,Ngày đủ công,Nửa ngày,Tổng công,Giờ lẻ,Tổng giờ,Lương gộp (VND),Tạm ứng (VND),Thực lĩnh (VND)");
        foreach (var row in model.Rows)
            csv.AppendLine(string.Join(",", new[] { model.Finalized == null ? "Tạm tính" : "Đã chốt", Cell(row.Code), Cell(row.Name),
                Num(row.DailySalary!.Value), row.FullDays.ToString(CultureInfo.InvariantCulture), row.HalfDays.ToString(CultureInfo.InvariantCulture),
                Num(row.WorkDays), Num(row.OddHours), Num(row.TotalHours), Num(row.Amount!.Value), Num(row.AdvanceAmount!.Value), Num(row.NetAmount!.Value) }));
        await transaction.CommitAsync();
        var encoding = new UTF8Encoding(true);
        return File(encoding.GetPreamble().Concat(encoding.GetBytes(csv.ToString())).ToArray(), "text/csv; charset=utf-8", $"BangLuong_{year}_{month:00}.csv");
    }

    private static string Num(decimal value) => value.ToString("0.##", CultureInfo.InvariantCulture);
    private static string Cell(string? value)
    {
        value ??= "";
        // Names and worker codes remain text when opened by spreadsheet software.
        if (value.TrimStart().StartsWith('=') || value.TrimStart().StartsWith('+') || value.TrimStart().StartsWith('-') || value.TrimStart().StartsWith('@') || value.StartsWith('\t') || value.StartsWith('\r')) value = "'" + value;
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
}
