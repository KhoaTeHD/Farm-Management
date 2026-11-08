using System.Text.Json;
using Farm_Management.Data;
using Farm_Management.Models;
using Farm_Management.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Farm_Management.Controllers;

public class HarvestTraceController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index(string? code, int? plantId)
    {
        var query = db.HarvestTraces.AsNoTracking(); if (plantId.HasValue) query = query.Where(t => t.PlantId == plantId);
        if (!string.IsNullOrWhiteSpace(code)) query = query.Where(t => t.Code == code.Trim());
        ViewBag.Code = code; ViewBag.PlantId = plantId; ViewBag.Legacy = await db.HarvestBatches.AsNoTracking().Where(h => !plantId.HasValue || h.PlantId == plantId).Where(h => !db.HarvestTraces.Any(t => t.LegacyHarvestId == h.Id)).ToListAsync();
        return View(await query.OrderByDescending(t => t.HarvestDate).ToListAsync());
    }
    public async Task<IActionResult> Details(int id)
    {
        var t = await db.HarvestTraces.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id); if (t == null) return NotFound();
        var saved = HarvestTraceService.Read(t); ViewBag.Snapshot = saved;
        ViewBag.CodeConflict = string.IsNullOrWhiteSpace(t.Code) || await db.HarvestTraces.CountAsync(x => x.Code == t.Code) > 1;
        var source = await Source(t); ViewBag.SourceChanged = source == null || HarvestTraceService.Hash(JsonSerializer.Serialize(source)) != t.SourceHash;
        ViewBag.Tests = await db.InspectionReports.Where(r => r.TargetType == "HarvestTrace" && r.TargetId == id).OrderByDescending(r => r.ReportDate).ToListAsync();
        return View(t);
    }
    public async Task<IActionResult> CreateLegacy(int id)
    { var h = await db.HarvestBatches.FindAsync(id); return h == null ? NotFound() : View("Legacy", h); }
    [HttpPost, ValidateAntiForgeryToken, ActionName("CreateLegacy")]
    public async Task<IActionResult> CreateLegacyConfirmed(int id)
    {
        await using var tx = await db.BeginBusinessWriteAsync(); var h = await db.HarvestBatches.FindAsync(id); if (h == null) return NotFound();
        var existing = await db.HarvestTraces.FirstOrDefaultAsync(t => t.LegacyHarvestId == id); if (existing != null) return RedirectToAction(nameof(Details), new { id = existing.Id });
        var service = new HarvestTraceService(db); var s = await service.BuildAsync(h.PlantId ?? 0, h.HarvestDate, h.YieldKg, h.QualityGrade);
        if (s == null) { TempData["Error"] = "Đợt thu hoạch cũ chưa đủ lô, ngày hoặc sản lượng hợp lệ. Bổ sung dữ liệu trước; không tự điền thay dữ liệu thiếu."; return RedirectToAction(nameof(Index)); }
        var t = new HarvestTrace { PlantId = h.PlantId, LegacyHarvestId = id, IsVoided = false }; await service.FillAsync(t, s); db.HarvestTraces.Add(t); await db.SaveChangesAsync(); await tx.CommitAsync(); return RedirectToAction(nameof(Details), new { id = t.Id });
    }
    public async Task<IActionResult> Reissue(int id)
    { var t = await db.HarvestTraces.FindAsync(id); return t == null ? NotFound() : View(t); }
    [HttpPost, ValidateAntiForgeryToken, ActionName("Reissue")]
    public async Task<IActionResult> ReissueConfirmed(int id)
    {
        await using var tx = await db.BeginBusinessWriteAsync(); var t = await db.HarvestTraces.FindAsync(id); if (t == null) return NotFound();
        var snapshot = await Source(t); if (snapshot == null || !await FarmRecordRules.TestsAllowAsync(db, "HarvestTrace", t.Id)) { TempData["Error"] = "Hồ sơ nguồn chưa đủ/đã lưu trữ hoặc còn kết quả kiểm nghiệm chưa đạt/chưa rõ. Không thể cập nhật bản truy xuất."; return RedirectToAction(nameof(Details), new { id }); }
        t.IsVoided = false;
        await new HarvestTraceService(db).FillAsync(t, snapshot); await db.SaveChangesAsync(); await tx.CommitAsync(); TempData["Success"] = "Đã lưu bản truy xuất mới cùng mã. Bản trước nằm trong lịch sử."; return RedirectToAction(nameof(Details), new { id });
    }
    private async Task<TraceSnapshot?> Source(HarvestTrace trace)
    {
        if (trace.JournalEntryId.HasValue) { var e = await db.CropJournalEntries.AsNoTracking().FirstOrDefaultAsync(e => e.Id == trace.JournalEntryId && e.Kind == "Harvest"); return e == null ? null : await new HarvestTraceService(db).BuildAsync(e.PlantId ?? 0, e.Date, e.Amount, e.Quality, trace.Id); }
        if (trace.LegacyHarvestId.HasValue) { var h = await db.HarvestBatches.FindAsync(trace.LegacyHarvestId); return h == null ? null : await new HarvestTraceService(db).BuildAsync(h.PlantId ?? 0, h.HarvestDate, h.YieldKg, h.QualityGrade, trace.Id); }
        return null;
    }
}
