using Farm_Management.Data;
using Farm_Management.Models;
using Farm_Management.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Farm_Management.Controllers;

public class RecordHistoryController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index(string? recordType, int? recordId, int? plantId, int page = 1)
    {
        if (!ModelState.IsValid || page < 1 || page > 100000) return BadRequest();
        var query = db.CultivationAudits.AsNoTracking();
        if (recordType != null) query = query.Where(a => a.RecordType == recordType);
        if (recordId.HasValue) query = query.Where(a => a.RecordId == recordId);
        if (plantId.HasValue) query = query.Where(a => a.PlantId == plantId);
        ViewBag.RecordType = recordType; ViewBag.RecordId = recordId; ViewBag.PlantId = plantId; ViewBag.Page = page; ViewBag.Total = await query.CountAsync();
        return View(await query.OrderByDescending(a => a.Id).Skip((page - 1) * 30).Take(30).ToListAsync());
    }
    public async Task<IActionResult> Archived()
    {
        var rows = new List<(string Type, int Id, string Name, DateTime? Date)>();
        rows.AddRange((await db.Areas.IgnoreQueryFilters().Where(r => r.IsDeleted == true).ToListAsync()).Select(r => ("Area", r.Id, r.Name ?? "Vùng chưa có tên", r.DeletedAt)));
        rows.AddRange((await db.PlantTypes.IgnoreQueryFilters().Where(r => r.IsDeleted == true).ToListAsync()).Select(r => ("PlantType", r.Id, r.Name ?? "Loại cây chưa có tên", r.DeletedAt)));
        rows.AddRange((await db.Seeds.IgnoreQueryFilters().Where(r => r.IsDeleted == true).ToListAsync()).Select(r => ("Seed", r.Id, r.Name ?? "Giống chưa có tên", r.DeletedAt)));
        rows.AddRange((await db.Plants.IgnoreQueryFilters().Where(r => r.IsDeleted == true).ToListAsync()).Select(r => ("Plant", r.Id, r.SeasonName ?? r.BatchCode ?? "Mùa vụ thiếu tên", r.DeletedAt)));
        rows.AddRange((await db.CropJournalEntries.IgnoreQueryFilters().Where(r => r.IsDeleted == true).ToListAsync()).Select(r => ("CropJournalEntry", r.Id, $"{CropSeasonRules.KindLabel(r.Kind)} · {r.Date:dd/MM/yyyy} · {r.Summary}", r.DeletedAt)));
        rows.AddRange((await db.CropCareSteps.IgnoreQueryFilters().Where(r => r.IsDeleted == true).ToListAsync()).Select(r => ("CropCareStep", r.Id, r.Title ?? "Việc thiếu tên", r.DeletedAt)));
        rows.AddRange((await db.InputLots.IgnoreQueryFilters().Where(r => r.IsDeleted == true).ToListAsync()).Select(r => ("InputLot", r.Id, $"{r.Code} · {r.ProductName}", r.DeletedAt)));
        rows.AddRange((await db.FieldResources.IgnoreQueryFilters().Where(r => r.IsDeleted == true).ToListAsync()).Select(r => ("FieldResource", r.Id, r.Name ?? "Nguồn chưa có tên", r.DeletedAt)));
        return View(rows.OrderByDescending(r => r.Date).ToList());
    }
    public async Task<IActionResult> Archive(string recordType, int id)
    {
        var item = await RecordChangeFilter.FindAsync(db, recordType, id); if (item == null || item.IsDeleted == true || recordType is not ("InputLot" or "FieldResource")) return NotFound();
        ViewBag.RecordType = recordType; ViewBag.RecordRevision = item.Revision ?? 0; ViewBag.ChangeIsEdit = true; return View("Confirm", item);
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Archive(string recordType, int id, string? unused = null)
    {
        await using var tx = await db.BeginBusinessWriteAsync(); var item = await RecordChangeFilter.FindAsync(db, recordType, id);
        if (item == null || item.IsDeleted == true || recordType is not ("InputLot" or "FieldResource")) return NotFound();
        if ((recordType == "InputLot" && (await db.CropJournalEntries.IgnoreQueryFilters().AnyAsync(e => e.InputLotId == id) || await db.Seeds.IgnoreQueryFilters().AnyAsync(s => s.InputLotId == id))) || (recordType == "FieldResource" && await db.CropJournalEntries.IgnoreQueryFilters().AnyAsync(e => e.WaterResourceId == id))
            || await db.InspectionReports.IgnoreQueryFilters().AnyAsync(r => r.TargetType == recordType && r.TargetId == id))
        { TempData["Error"] = "Hồ sơ đã được dùng hoặc có kiểm nghiệm; cần giữ liên kết truy xuất. Đổi trạng thái Tạm giữ thay vì lưu trữ."; return RedirectToAction("Index", "FarmRecords"); }
        item.IsDeleted = true; await db.SaveChangesAsync(); await tx.CommitAsync(); TempData["Success"] = "Đã chuyển hồ sơ vào mục lưu trữ, vẫn giữ lịch sử và tệp cũ."; return RedirectToAction(nameof(Archived));
    }
    public async Task<IActionResult> Restore(string recordType, int id)
    {
        var item = await RecordChangeFilter.FindAsync(db, recordType, id); if (item == null || item.IsDeleted != true) return NotFound();
        ViewBag.RecordType = recordType; ViewBag.RecordRevision = item.Revision ?? 0; ViewBag.ChangeIsEdit = true; ViewBag.Restoring = true; return View("Confirm", item);
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(string recordType, int id, string? unused = null)
    {
        await using var tx = await db.BeginBusinessWriteAsync(); var item = await RecordChangeFilter.FindAsync(db, recordType, id); if (item == null || item.IsDeleted != true) return NotFound();
        string? error = null;
        if (item is CropJournalEntry journal)
        {
            var p = await db.Plants.FindAsync(journal.PlantId ?? 0);
            if (p == null || !CropSeasonRules.Complete(journal, p)) error = "Cần khôi phục lô cây và bổ sung dữ liệu trước khi khôi phục nhật ký.";
            else if (journal.CareStepId.HasValue && !await db.CropCareSteps.AnyAsync(s => s.Id == journal.CareStepId && s.PlantId == journal.PlantId && s.Kind == journal.Kind)) error = "Khôi phục việc dự kiến được liên kết trước.";
            else error = await new JournalResourceService(db).ValidateAsync(journal, p, journal.Id);
            if (error == null && journal.Kind is "Harvest" or "Pesticide")
            {
                var dates = await db.CropJournalEntries.Where(e => e.PlantId == journal.PlantId && e.Kind == "Harvest").Select(e => e.Date).ToListAsync();
                dates.AddRange(await db.HarvestBatches.Where(e => e.PlantId == journal.PlantId).Select(e => e.HarvestDate).ToListAsync()); if (journal.Kind == "Harvest") dates.Add(journal.Date);
                foreach (var date in dates) { if (!date.HasValue) { error = "Có thu hoạch thiếu ngày; bổ sung trước."; break; } var safety = await new CropSeasonService(db).SafetyAsync(journal.PlantId!.Value, date, journal.Id, journal); if (safety.Incomplete || safety.SafeDate > date || p!.ExpectedSafeHarvestDate > date) { error = "Khôi phục gây mâu thuẫn thời gian cách ly với thu hoạch."; break; } }
            }
            if (error == null) { foreach (var trace in await db.HarvestTraces.Where(t => t.JournalEntryId == id).ToListAsync()) trace.IsVoided = journal.Kind != "Harvest" || !await FarmRecordRules.TestsAllowAsync(db, "HarvestTrace", trace.Id); }
        }
        else if (item is CropCareStep step && !await db.Plants.AnyAsync(p => p.Id == step.PlantId)) error = "Khôi phục mùa vụ trước.";
        else if (item is Plant plant && (!await db.Areas.AnyAsync(a => a.Id == plant.AreaId) || !await db.Seeds.AnyAsync(s => s.Id == plant.SeedId && s.PlantTypeId == plant.PlantTypeId))) error = "Vùng hoặc giống không còn khớp. Cần bổ sung trước khi khôi phục mùa vụ.";
        else if (item is FieldResource source && !await db.Areas.AnyAsync(a => a.Id == source.AreaId)) error = "Vùng trồng của nguồn không còn tồn tại.";
        else if (item is Seed seed && !await db.PlantTypes.AnyAsync(t => t.Id == seed.PlantTypeId)) error = "Khôi phục loại cây của giống trước.";
        else if (item is not (CropJournalEntry or CropCareStep or Plant or FieldResource or InputLot or Area or Seed or PlantType)) return BadRequest();
        if (error != null) { TempData["Error"] = error; return RedirectToAction(nameof(Archived)); }
        item.IsDeleted = false; await db.SaveChangesAsync(); await tx.CommitAsync(); TempData["Success"] = "Đã khôi phục và ghi thêm một phiên bản lịch sử."; return RedirectToAction(nameof(Archived));
    }
    public async Task<IActionResult> TraceVersion(int id, string side = "After")
    {
        if (side is not ("Before" or "After")) return BadRequest();
        var audit = await db.CultivationAudits.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id && a.RecordType == "HarvestTrace"); if (audit == null) return NotFound();
        HarvestTrace? trace;
        try { trace = System.Text.Json.JsonSerializer.Deserialize<HarvestTrace>((side == "Before" ? audit.BeforeJson : audit.AfterJson) ?? "null"); } catch (System.Text.Json.JsonException) { return BadRequest("Bản lưu không đọc được."); }
        if (trace == null) return NotFound(); ViewBag.Historical = true; ViewBag.SourceChanged = false; ViewBag.Snapshot = HarvestTraceService.Read(trace);
        return View("~/Views/HarvestTrace/Details.cshtml", trace);
    }
}
