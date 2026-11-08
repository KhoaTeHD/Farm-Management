using Farm_Management.Data;
using Farm_Management.Models;
using Farm_Management.Models.ViewModels;
using Farm_Management.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Farm_Management.Controllers;

public class PlantsController(ApplicationDbContext db, CropSeasonService seasons) : Controller
{
    public async Task<IActionResult> Index()
    {
        var plants = await db.Plants.AsNoTracking().OrderByDescending(p => p.PlantingDate).ToListAsync();
        var areas = await db.Areas.AsNoTracking().ToDictionaryAsync(a => a.Id);
        var types = await db.PlantTypes.AsNoTracking().ToDictionaryAsync(t => t.Id);
        var seeds = await db.Seeds.AsNoTracking().ToDictionaryAsync(s => s.Id);
        foreach (var p in plants) { p.Area = p.AreaId.HasValue ? areas.GetValueOrDefault(p.AreaId.Value) : null; p.PlantType = p.PlantTypeId.HasValue ? types.GetValueOrDefault(p.PlantTypeId.Value) : null; p.Seed = p.SeedId.HasValue ? seeds.GetValueOrDefault(p.SeedId.Value) : null; }
        return View(plants);
    }
    public async Task<IActionResult> Details(int id)
    {
        var model = await seasons.LoadAsync(id); return model == null ? NotFound() : View(model);
    }
    public async Task<IActionResult> Create()
    {
        var form = new PlantForm { BatchCode = "LO-" + DateTime.Today.ToString("yyyyMMdd") + "-" + Guid.NewGuid().ToString("N")[..6].ToUpperInvariant(), PlantingDate = DateTime.Today };
        await Options(form); return View("Form", form);
    }
    public async Task<IActionResult> Edit(int id)
    {
        var p = await db.Plants.FindAsync(id); if (p == null) return NotFound();
        var f = new PlantForm { Id = p.Id, SeasonName = p.SeasonName, CareProtocol = p.CareProtocol, BatchCode = p.BatchCode, AreaId = p.AreaId, PlantTypeId = p.PlantTypeId, SeedId = p.SeedId, Quantity = p.Quantity, PlantingDate = p.PlantingDate, ExpectedHarvestDate = p.ExpectedHarvestDate, Status = p.Status, Notes = p.Notes };
        await Options(f); return View("Form", f);
    }
    [HttpPost, ValidateAntiForgeryToken] public Task<IActionResult> Create(PlantForm form) { form.Id = 0; return Save(form, true); }
    [HttpPost, ValidateAntiForgeryToken] public Task<IActionResult> Edit(int id, PlantForm form) => id != form.Id ? Task.FromResult<IActionResult>(NotFound()) : Save(form, false);
    private async Task<IActionResult> Save(PlantForm form, bool create)
    {
        await using var tx = await db.BeginBusinessWriteAsync(); form.BatchCode = form.BatchCode?.Trim().ToUpperInvariant();
        var item = create ? new Plant() : await db.Plants.FindAsync(form.Id); if (item == null) return NotFound();
        var type = form.PlantTypeId.HasValue ? await db.PlantTypes.FindAsync(form.PlantTypeId.Value) : null;
        var seed = form.SeedId.HasValue ? await db.Seeds.FindAsync(form.SeedId.Value) : null;
        if (!await db.Areas.AnyAsync(a => a.Id == form.AreaId)) ModelState.AddModelError(nameof(form.AreaId), "Vùng trồng không tồn tại.");
        if (type == null) ModelState.AddModelError(nameof(form.PlantTypeId), "Loại cây không tồn tại.");
        if(seed?.InputLotId!=null){var seedLot=await db.InputLots.FindAsync(seed.InputLotId.Value);if(seedLot==null || !FarmRecordRules.InputComplete(seedLot) || seedLot.Kind!="Seed" || seedLot.Status!="Released" || !await FarmRecordRules.TestsAllowAsync(db,"InputLot",seedLot.Id) || seedLot.ReceivedDate==null || form.PlantingDate<seedLot.ReceivedDate || (seedLot.ExpiryDate.HasValue && form.PlantingDate>seedLot.ExpiryDate))ModelState.AddModelError(nameof(form.SeedId),"Lô giống đầu vào chưa được cho dùng hoặc không phù hợp ngày trồng.");}
        if (seed == null || seed.PlantTypeId != form.PlantTypeId) ModelState.AddModelError(nameof(form.SeedId), "Giống không tồn tại hoặc không thuộc loại cây đã chọn.");
        if (await db.Plants.IgnoreQueryFilters().AnyAsync(p => p.Id != form.Id && p.BatchCode == form.BatchCode)) ModelState.AddModelError(nameof(form.BatchCode), "Mã lô đã tồn tại.");
        var safety = await seasons.SafetyAsync(create ? 0 : form.Id);
        var safeDate = safety.SafeDate;
        if (item.ExpectedSafeHarvestDate.HasValue && (!safeDate.HasValue || item.ExpectedSafeHarvestDate > safeDate)) safeDate = item.ExpectedSafeHarvestDate;
        if (!create && form.PlantingDate.HasValue)
        {
            if (await db.CropJournalEntries.AnyAsync(e => e.PlantId == item.Id && e.Date < form.PlantingDate)
                || await db.WaterIrrigationLogs.AnyAsync(e => e.PlantId == item.Id && e.IrrigationDate < form.PlantingDate)
                || await db.FertilizationLogs.AnyAsync(e => e.PlantId == item.Id && e.ApplicationDate < form.PlantingDate)
                || await db.PesticideApplicationLogs.AnyAsync(e => e.PlantId == item.Id && e.ApplicationDate < form.PlantingDate)
                || await db.HarvestBatches.AnyAsync(e => e.PlantId == item.Id && e.HarvestDate < form.PlantingDate)) ModelState.AddModelError(nameof(form.PlantingDate), "Ngày trồng mới nằm sau nhật ký đã ghi. Kiểm tra lại trước khi sửa.");
        }
        Plant? source = null;
        if (create && form.SourcePlantId.HasValue)
        {
            source = await db.Plants.FindAsync(form.SourcePlantId.Value);
            if (source == null || source.PlantTypeId != form.PlantTypeId) ModelState.AddModelError(nameof(form.SourcePlantId), "Chọn mùa vụ cùng loại cây để lấy kế hoạch.");
        }
        if ((form.Status == "Harvested" || form.Status == "ReadyToHarvest") && (safety.Incomplete || safeDate > DateTime.Today)) ModelState.AddModelError(nameof(form.Status), "Chưa thể đánh dấu sẵn sàng hoặc đã thu hoạch: nhật ký thuốc chưa đủ dữ liệu hoặc chưa hết thời gian cách ly đã ghi nhận.");
        if (form.ExpectedHarvestDate == null && type?.GrowthCycleDays != null && form.PlantingDate.HasValue)
        {
            form.ExpectedHarvestDate = CropRules.Expected(form.PlantingDate, type.GrowthCycleDays);
            if (form.ExpectedHarvestDate == null) ModelState.AddModelError(nameof(form.ExpectedHarvestDate), "Không thể tính ngày dự kiến từ chu kỳ hiện tại. Hãy nhập ngày hợp lệ.");
        }
        if (!ModelState.IsValid) { await Options(form); return View("Form", form); }
        item.SeasonName = form.SeasonName?.Trim(); item.CareProtocol = form.CareProtocol;
        item.BatchCode = form.BatchCode; item.AreaId = form.AreaId; item.PlantTypeId = form.PlantTypeId; item.SeedId = form.SeedId;
        item.Quantity = form.Quantity; item.PlantingDate = form.PlantingDate?.Date; item.ExpectedHarvestDate = form.ExpectedHarvestDate?.Date; item.Status = form.Status; item.Notes = form.Notes;
        if (create) db.Plants.Add(item); await db.SaveChangesAsync();
        if (source != null)
        {
            var steps = await db.CropCareSteps.AsNoTracking().Where(s => s.PlantId == source.Id).ToListAsync();
            foreach (var step in steps)
            {
                if (CropSeasonRules.PlanDate(item.PlantingDate, step.PlannedDay) == null || !CropSeasonRules.Kinds.ContainsKey(step.Kind ?? "") || string.IsNullOrWhiteSpace(step.Title) || string.IsNullOrWhiteSpace(step.Instructions)) continue;
                db.CropCareSteps.Add(new() { PlantId = item.Id, Title = step.Title, Kind = step.Kind, PlannedDay = step.PlannedDay, Instructions = step.Instructions, CreatedAt = DateTime.Now });
            }
            await db.SaveChangesAsync();
        }
        await tx.CommitAsync(); TempData["Success"] = "Đã lưu lô cây / mùa vụ. Mở mục Việc dự kiến để lập kế hoạch, hoặc Ghi việc đã làm để bắt đầu nhật ký."; return RedirectToAction(nameof(Details), new { id = item.Id });
    }
    public async Task<IActionResult> Delete(int id) { var p = await db.Plants.FindAsync(id); return p == null ? NotFound() : View(p); }
    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        await using var tx = await db.BeginBusinessWriteAsync(); var p = await db.Plants.FindAsync(id); if (p == null) return NotFound();
        var used = await db.CropJournalEntries.AnyAsync(l => l.PlantId == id) || await db.CropCareSteps.AnyAsync(l => l.PlantId == id) || await db.PesticideApplicationLogs.AnyAsync(l => l.PlantId == id) || await db.FertilizationLogs.AnyAsync(l => l.PlantId == id) || await db.WaterIrrigationLogs.AnyAsync(l => l.PlantId == id) || await db.HarvestBatches.AnyAsync(l => l.PlantId == id);
        if (used) TempData["Error"] = "Lô cây có nhật ký canh tác hoặc thu hoạch; không thể xóa. Có thể đổi trạng thái thành Đã hủy.";
        else { db.Plants.Remove(p); await db.SaveChangesAsync(); await tx.CommitAsync(); TempData["Success"] = "Đã xóa lô cây chưa có nhật ký."; }
        return RedirectToAction(nameof(Index));
    }
    private async Task Options(PlantForm f)
    {
        ViewBag.Areas = new SelectList(await db.Areas.OrderBy(a => a.Name).ToListAsync(), "Id", "Name", f.AreaId);
        ViewBag.Types = await db.PlantTypes.OrderBy(t => t.Name).ToListAsync();
        ViewBag.Seeds = await db.Seeds.OrderBy(s => s.Name).ToListAsync();
        ViewBag.Seasons = await db.Plants.OrderByDescending(p => p.PlantingDate).ToListAsync();
    }

    public async Task<IActionResult> Evaluation(int id)
    {
        var p = await db.Plants.FindAsync(id); if (p == null) return NotFound(); ViewBag.Plant = p;
        return View(new SeasonEvaluationForm { Id = id, CareProtocol = p.CareProtocol, SeasonReview = p.SeasonReview, NextSeasonChanges = p.NextSeasonChanges });
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Evaluation(int id, SeasonEvaluationForm form)
    {
        if (id != form.Id) return NotFound(); await using var tx = await db.BeginBusinessWriteAsync();
        var p = await db.Plants.FindAsync(id); if (p == null) return NotFound(); ViewBag.Plant = p;
        if (!ModelState.IsValid) return View(form);
        p.CareProtocol = form.CareProtocol; p.SeasonReview = form.SeasonReview; p.NextSeasonChanges = form.NextSeasonChanges;
        await db.SaveChangesAsync(); await tx.CommitAsync(); TempData["Success"] = "Đã lưu kinh nghiệm và dự định điều chỉnh cho mùa sau.";
        return RedirectToAction(nameof(Details), new { id });
    }
    public async Task<IActionResult> Compare(int? leftId, int? rightId)
    {
        if (!ModelState.IsValid) return BadRequest();
        var model = new CropComparisonViewModel { LeftId = leftId, RightId = rightId, Choices = await db.Plants.AsNoTracking().OrderByDescending(p => p.PlantingDate).ToListAsync() };
        if (leftId.HasValue) { model.Left = await seasons.LoadAsync(leftId.Value); if (model.Left == null) ModelState.AddModelError("", "Mùa vụ thứ nhất không còn tồn tại."); }
        if (rightId.HasValue) { model.Right = await seasons.LoadAsync(rightId.Value); if (model.Right == null) ModelState.AddModelError("", "Mùa vụ thứ hai không còn tồn tại."); }
        if (leftId.HasValue && leftId == rightId) ModelState.AddModelError("", "Chọn hai mùa vụ khác nhau để so sánh.");
        return View(model);
    }
}
