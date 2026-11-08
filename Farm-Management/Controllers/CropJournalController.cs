using Farm_Management.Data;
using Farm_Management.Models;
using Farm_Management.Models.ViewModels;
using Farm_Management.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Farm_Management.Controllers;

public class CropJournalController(ApplicationDbContext db, CropSeasonService seasons, IWebHostEnvironment environment) : Controller
{
    public async Task<IActionResult> Create(int plantId, string kind = "Observation", int? stepId = null)
    {
        var plant = await db.Plants.FindAsync(plantId); if (plant == null) return NotFound();
        var step = stepId.HasValue ? await db.CropCareSteps.FindAsync(stepId.Value) : null;
        if (stepId.HasValue && (step == null || step.PlantId != plantId)) return BadRequest("Việc dự kiến không thuộc mùa vụ này.");
        var form = new CropJournalForm { PlantId = plantId, Kind = step?.Kind ?? kind, CareStepId = stepId, Date = DateTime.Today, RecordCode = Guid.NewGuid().ToString("N") };
        await Options(form); return View("Form", form);
    }
    public async Task<IActionResult> Edit(int id)
    {
        var item = await db.CropJournalEntries.FindAsync(id); if (item == null) return NotFound();
        var f = From(item); await Options(f); return View("Form", f);
    }
    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(6 * 1024 * 1024)]
    public Task<IActionResult> Create(CropJournalForm form) { form.Id = 0; return Save(form, true); }
    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(6 * 1024 * 1024)]
    public Task<IActionResult> Edit(int id, CropJournalForm form) => id == form.Id ? Save(form, false) : Task.FromResult<IActionResult>(NotFound());

    private async Task<IActionResult> Save(CropJournalForm form, bool create)
    {
        await using var tx = await db.BeginBusinessWriteAsync();
        var old = create ? new CropJournalEntry() : await db.CropJournalEntries.FindAsync(form.Id); if (old == null) return NotFound();
        var plant = await db.Plants.FindAsync(form.PlantId ?? 0);
        if (plant == null) ModelState.AddModelError("", "Không tìm thấy lô cây. Hãy chọn lại từ danh sách mùa vụ.");
        else if (!plant.PlantingDate.HasValue || form.Date?.Date < plant.PlantingDate.Value.Date) ModelState.AddModelError(nameof(form.Date), "Ngày ghi phải từ ngày trồng. Nếu chưa có ngày trồng, bổ sung hồ sơ trước.");
        if (!create && old.PlantId.HasValue && old.PlantId != form.PlantId) ModelState.AddModelError("", "Không chuyển nhật ký sang lô khác; hãy sửa trong đúng hồ sơ mùa vụ.");
        if (create && await db.CropJournalEntries.AnyAsync(e => e.RecordCode == form.RecordCode)) ModelState.AddModelError("", "Phiếu này đã được lưu. Quay về hồ sơ để xem, tránh ghi hai lần.");
        if (form.CareStepId.HasValue)
        {
            var step = await db.CropCareSteps.FindAsync(form.CareStepId.Value);
            if (step == null || step.PlantId != form.PlantId || step.Kind != form.Kind) ModelState.AddModelError(nameof(form.CareStepId), "Việc dự kiến không khớp lô cây hoặc nhóm công việc.");
        }
        var item = new CropJournalEntry
        {
            Id = old.Id,
            PlantId = form.PlantId,
            Revision = old.Revision, IsDeleted = old.IsDeleted, DeletedAt = old.DeletedAt,
            InputLotId = form.Kind is "Fertilize" or "Pesticide" ? form.InputLotId : null,
            WaterResourceId = form.Kind == "Water" ? form.WaterResourceId : null,
            CareStepId = form.CareStepId,
            Date = form.Date?.Date,
            Kind = form.Kind,
            RecordCode = create ? form.RecordCode : old.RecordCode ?? form.RecordCode,
            Recorder = form.Recorder?.Trim(),
            Summary = form.Summary?.Trim(),
            Method = form.Method,
            Weather = form.Weather
        };
        if (form.Kind is "Water" or "Fertilize" or "Pesticide" or "Harvest") { item.Amount = form.Amount; item.Unit = form.Unit; }
        if (form.Kind is "Fertilize" or "Pesticide") item.Material = form.Material?.Trim();
        if (form.Kind == "Pesticide") item.WithdrawalDays = form.WithdrawalDays;
        if (form.Kind == "Harvest") item.Quality = form.Quality?.Trim();
        if (form.Kind == "Observation") { item.GrowthStage = form.GrowthStage; item.Condition = form.Condition; item.HeightCm = form.HeightCm; item.SurvivalPercent = form.SurvivalPercent; item.AffectedPercent = form.AffectedPercent; }
        if (plant != null)
        {
            var resourceError = await new JournalResourceService(db).ValidateAsync(item,plant,form.Id);
            if(resourceError!=null) ModelState.AddModelError("",resourceError);
            else if(form.Kind is "Fertilize" or "Pesticide") { ModelState.Remove(nameof(form.Material)); form.Material=item.Material; }
            var harvestDates = await db.CropJournalEntries.Where(e => e.PlantId == plant.Id && e.Kind == "Harvest" && e.Id != form.Id).Select(e => e.Date).ToListAsync();
            harvestDates.AddRange(await db.HarvestBatches.Where(h => h.PlantId == plant.Id).Select(h => h.HarvestDate).ToListAsync());
            if (form.Kind == "Harvest") harvestDates.Add(form.Date);
            if (form.Kind is "Pesticide" or "Harvest" || old.Kind == "Pesticide")
            {
                foreach (var date in harvestDates)
                {
                    if (!date.HasValue) { ModelState.AddModelError("", "Có đợt thu hoạch thiếu ngày. Bổ sung ngày trước khi thay đổi thuốc hoặc thu hoạch."); break; }
                    var safety = await seasons.SafetyAsync(plant.Id, date, create ? 0 : form.Id, item);
                    if (safety.Incomplete || safety.SafeDate > date.Value.Date || plant.ExpectedSafeHarvestDate > date.Value.Date)
                    { ModelState.AddModelError("", "Ngày thu hoạch chưa qua mốc cách ly hoặc nhật ký thuốc thiếu thông tin. Kiểm tra ngày dùng thuốc và nhãn sản phẩm."); break; }
                }
                if (form.Kind == "Pesticide" && item.Date.HasValue && item.WithdrawalDays.HasValue)
                    try { _ = item.Date.Value.AddDays(item.WithdrawalDays.Value); } catch (ArgumentOutOfRangeException) { ModelState.AddModelError(nameof(form.WithdrawalDays), "Ngày kết thúc cách ly vượt phạm vi cho phép."); }
            }
        }
        var photo = await ReadPhoto(form.Photo);
        if (!ModelState.IsValid) { form.ExistingPhoto = old.PhotoPath; await Options(form); return View("Form", form); }
        string? newPath = null;
        try
        {
            if (photo.HasValue)
            {
                newPath = "/uploads/crops/" + Guid.NewGuid().ToString("N") + photo.Value.Extension;
                var directory = Path.Combine(environment.WebRootPath, "uploads", "crops"); Directory.CreateDirectory(directory);
                await System.IO.File.WriteAllBytesAsync(Path.Combine(directory, Path.GetFileName(newPath)), photo.Value.Bytes);
            }
            item.PhotoPath = newPath ?? (form.RemovePhoto ? null : old.PhotoPath);
            item.CreatedAt = create ? DateTime.Now : old.CreatedAt; item.UpdatedAt = DateTime.Now;
            if (create) db.CropJournalEntries.Add(item); else db.Entry(old).CurrentValues.SetValues(item);
            await db.SaveChangesAsync();
            await new HarvestTraceService(db).EnsureAsync(item); await db.SaveChangesAsync();
            await tx.CommitAsync();
        }
        catch
        {
            if (newPath != null) System.IO.File.Delete(Path.Combine(environment.WebRootPath, "uploads", "crops", Path.GetFileName(newPath)));
            throw;
        }
        TempData["Success"] = "Đã lưu vào sổ mùa vụ. Lần thăm vườn sau, ghi thêm một lần mới để thấy cây thay đổi.";
        return RedirectToAction("Details", "Plants", new { id = item.PlantId });
    }
    public async Task<IActionResult> Delete(int id) { var item = await db.CropJournalEntries.FindAsync(id); return item == null ? NotFound() : View(item); }
    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        await using var tx = await db.BeginBusinessWriteAsync(); var item = await db.CropJournalEntries.FindAsync(id); if (item == null) return NotFound();
        db.CropJournalEntries.Remove(item); await db.SaveChangesAsync();
        await new HarvestTraceService(db).EnsureAsync(item); await db.SaveChangesAsync(); await tx.CommitAsync();
        TempData["Success"] = "Đã chuyển ghi chép vào lưu trữ, giữ nguyên lịch sử và ảnh. Có thể khôi phục trong mục Hồ sơ lưu trữ.";
        return item.PlantId.HasValue ? RedirectToAction("Details", "Plants", new { id = item.PlantId }) : RedirectToAction("Index", "Plants");
    }
    private async Task Options(CropJournalForm form)
    {
        ViewBag.Plant = await db.Plants.FindAsync(form.PlantId ?? 0);
        ViewBag.Steps = await db.CropCareSteps.Where(s => s.PlantId == form.PlantId).OrderBy(s => s.PlannedDay).ToListAsync();
        ViewBag.Inputs = await db.InputLots.OrderBy(l=>l.ProductName).ToListAsync();
        var plant=ViewBag.Plant as Plant; var areaId=plant?.AreaId;
        ViewBag.WaterSources = await db.FieldResources.Where(r=>r.Kind=="Water" && r.AreaId==areaId).OrderBy(r=>r.Name).ToListAsync();
    }
    private async Task<(byte[] Bytes, string Extension)?> ReadPhoto(IFormFile? file)
    {
        if (file == null || file.Length == 0) return null;
        if (file.Length > 5 * 1024 * 1024) { ModelState.AddModelError(nameof(CropJournalForm.Photo), "Ảnh tối đa 5 MB. Hãy chọn ảnh nhỏ hơn."); return null; }
        using var stream = new MemoryStream(); await file.CopyToAsync(stream); var bytes = stream.ToArray();
        string? extension = bytes.Length >= 8 && bytes.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }) ? ".png"
            : bytes.Length >= 3 && bytes[0] == 255 && bytes[1] == 216 && bytes[2] == 255 ? ".jpg"
            : bytes.Length >= 12 && System.Text.Encoding.ASCII.GetString(bytes, 0, 4) == "RIFF" && System.Text.Encoding.ASCII.GetString(bytes, 8, 4) == "WEBP" ? ".webp" : null;
        if (extension == null) ModelState.AddModelError(nameof(CropJournalForm.Photo), "Chỉ nhận ảnh JPG, PNG hoặc WebP; không nhận PDF, SVG hay tệp khác.");
        return extension == null ? null : (bytes, extension);
    }
    private static CropJournalForm From(CropJournalEntry e) => new() { Id = e.Id, PlantId = e.PlantId, CareStepId = e.CareStepId, InputLotId = e.InputLotId, WaterResourceId = e.WaterResourceId, RecordCode = e.RecordCode ?? Guid.NewGuid().ToString("N"), Date = e.Date, Kind = e.Kind, Recorder = e.Recorder, Summary = e.Summary, Material = e.Material, Amount = e.Amount, Unit = e.Unit, Method = e.Method, Weather = e.Weather, WithdrawalDays = e.WithdrawalDays, GrowthStage = e.GrowthStage, Condition = e.Condition, HeightCm = e.HeightCm, SurvivalPercent = e.SurvivalPercent, AffectedPercent = e.AffectedPercent, Quality = e.Quality, ExistingPhoto = e.PhotoPath };
}
