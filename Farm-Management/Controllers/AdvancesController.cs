using Farm_Management.Data;
using Farm_Management.Models;
using Farm_Management.Models.ViewModels;
using Farm_Management.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Farm_Management.Controllers;

public class AdvancesController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index(int? year, int? month, bool incomplete = false)
    {
        var y = year ?? DateTime.Today.Year; var m = month ?? DateTime.Today.Month;
        if (!ModelState.IsValid || !PayrollService.ValidPeriod(y, m)) return BadRequest("Tháng khấu trừ không hợp lệ");
        var query = db.WorkerAdvances.AsNoTracking();
        if (!incomplete) query = query.Where(a => a.PayrollYear == y && a.PayrollMonth == m);
        var items = await query.OrderByDescending(a => a.PaidDate).ThenByDescending(a => a.Id).ToListAsync();
        var workers = await db.Workers.AsNoTracking().ToDictionaryAsync(w => w.Id);
        if (incomplete) items = items.Where(a => !a.WorkerId.HasValue || !workers.ContainsKey(a.WorkerId.Value) || !PayrollService.ValidPeriod(a.PayrollYear ?? 0, a.PayrollMonth ?? 0)
            || !PayrollService.ValidAdvanceAmount(a.Amount) || a.PaidDate == null || a.PaidDate.Value.Year < 2 || a.PaidDate.Value.Date > DateTime.Today || string.IsNullOrWhiteSpace(a.Reference)).ToList();
        return View(new AdvanceListViewModel
        {
            Year = y,
            Month = m,
            Items = items,
            Workers = workers,
            Incomplete = incomplete,
            Locked = await PayrollService.IsLockedAsync(db, new DateTime(y, m, 1))
        });
    }

    public async Task<IActionResult> Create(int? year, int? month)
    {
        var form = new AdvanceForm
        {
            PayrollYear = year ?? DateTime.Today.Year,
            PayrollMonth = month ?? DateTime.Today.Month,
            PaidDate = DateTime.Today,
            Reference = "TU-" + DateTime.Today.ToString("yyyyMMdd") + "-" + Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()
        };
        await Options(form); return View("Form", form);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AdvanceForm form)
    {
        form.Id = 0;
        await using var transaction = await db.BeginBusinessWriteAsync();
        await Validate(form, null);
        if (!ModelState.IsValid) { await Options(form); return View("Form", form); }
        var item = new WorkerAdvance { CreatedAt = DateTime.Now }; Set(item, form);
        db.WorkerAdvances.Add(item); await db.SaveChangesAsync(); await transaction.CommitAsync();
        TempData["Success"] = "Đã ghi nhận phiếu tạm ứng.";
        return RedirectToAction(nameof(Index), new { year = form.PayrollYear, month = form.PayrollMonth });
    }

    public async Task<IActionResult> Edit(int id)
    {
        var item = await db.WorkerAdvances.FindAsync(id); if (item == null) return NotFound();
        var form = AdvanceForm.FromEntity(item); await Options(form); return View("Form", form);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, AdvanceForm form)
    {
        if (id != form.Id) return NotFound();
        await using var transaction = await db.BeginBusinessWriteAsync();
        var item = await db.WorkerAdvances.FindAsync(id); if (item == null) return NotFound();
        await Validate(form, item);
        if (!ModelState.IsValid) { await Options(form); return View("Form", form); }
        Set(item, form); await db.SaveChangesAsync(); await transaction.CommitAsync();
        TempData["Success"] = "Đã cập nhật phiếu tạm ứng.";
        return RedirectToAction(nameof(Index), new { year = form.PayrollYear, month = form.PayrollMonth });
    }

    public async Task<IActionResult> Delete(int id)
    {
        var item = await db.WorkerAdvances.FindAsync(id); if (item == null) return NotFound();
        ViewBag.Locked = await Locked(item.PayrollYear, item.PayrollMonth);
        ViewBag.Worker = item.WorkerId.HasValue ? await db.Workers.FindAsync(item.WorkerId.Value) : null;
        return View(item);
    }

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        await using var transaction = await db.BeginBusinessWriteAsync();
        var item = await db.WorkerAdvances.FindAsync(id); if (item == null) return NotFound();
        var y = item.PayrollYear; var m = item.PayrollMonth;
        if (await Locked(y, m)) TempData["Error"] = PayrollService.LockedMessage;
        else { db.WorkerAdvances.Remove(item); await db.SaveChangesAsync(); await transaction.CommitAsync(); TempData["Success"] = "Đã xóa phiếu khỏi bản tạm tính; bản chốt cũ vẫn được giữ."; }
        return RedirectToAction(nameof(Index), new { year = y, month = m });
    }

    private async Task Validate(AdvanceForm form, WorkerAdvance? old)
    {
        form.Reference = form.Reference?.Trim();
        if (!form.WorkerId.HasValue || !await db.Workers.AnyAsync(w => w.Id == form.WorkerId)) ModelState.AddModelError(nameof(form.WorkerId), "Công nhân không tồn tại.");
        if (await db.WorkerAdvances.AnyAsync(a => a.Id != form.Id && a.Reference == form.Reference)) ModelState.AddModelError(nameof(form.Reference), "Mã phiếu đã tồn tại; kiểm tra để tránh ghi nhận trùng.");
        if (await Locked(form.PayrollYear, form.PayrollMonth) || (old != null && await Locked(old.PayrollYear, old.PayrollMonth))) ModelState.AddModelError("", PayrollService.LockedMessage);
    }
    private Task<bool> Locked(int? y, int? m) => PayrollService.ValidPeriod(y ?? 0, m ?? 0) ? PayrollService.IsLockedAsync(db, new DateTime(y!.Value, m!.Value, 1)) : Task.FromResult(false);
    private async Task Options(AdvanceForm form)
    {
        var workers = await db.Workers.OrderBy(w => w.Code).Select(w => new { w.Id, Text = (w.Code ?? "Thiếu mã") + " · " + (w.FullName ?? "Thiếu tên") + (w.IsActive == false ? " (đã nghỉ)" : "") }).ToListAsync();
        ViewBag.WorkerOptions = new SelectList(workers, "Id", "Text", form.WorkerId);
        ViewBag.Locked = await Locked(form.PayrollYear, form.PayrollMonth);
    }
    private static void Set(WorkerAdvance a, AdvanceForm form)
    {
        a.WorkerId = form.WorkerId; a.Reference = form.Reference; a.PaidDate = form.PaidDate?.Date;
        a.PayrollYear = form.PayrollYear; a.PayrollMonth = form.PayrollMonth; a.Amount = form.Amount; a.Notes = form.Notes;
    }
}
