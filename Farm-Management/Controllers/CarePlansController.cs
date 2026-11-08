using Farm_Management.Data;
using Farm_Management.Models;
using Farm_Management.Models.ViewModels;
using Farm_Management.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Farm_Management.Controllers;

public class CarePlansController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Create(int plantId)
    {
        ViewBag.Plant = await db.Plants.FindAsync(plantId); if (ViewBag.Plant == null) return NotFound();
        return View("Form", new CareStepForm { PlantId = plantId, PlannedDay = 0, Kind = "Observation" });
    }
    public async Task<IActionResult> Edit(int id)
    {
        var s = await db.CropCareSteps.FindAsync(id); if (s == null) return NotFound(); ViewBag.Plant = await db.Plants.FindAsync(s.PlantId ?? 0);
        return View("Form", new CareStepForm { Id = s.Id, PlantId = s.PlantId, Title = s.Title, Kind = s.Kind, PlannedDay = s.PlannedDay, Instructions = s.Instructions });
    }
    [HttpPost, ValidateAntiForgeryToken] public Task<IActionResult> Create(CareStepForm form) { form.Id = 0; return Save(form, true); }
    [HttpPost, ValidateAntiForgeryToken] public Task<IActionResult> Edit(int id, CareStepForm form) => id == form.Id ? Save(form, false) : Task.FromResult<IActionResult>(NotFound());
    private async Task<IActionResult> Save(CareStepForm f, bool create)
    {
        await using var tx = await db.BeginBusinessWriteAsync(); var item = create ? new CropCareStep() : await db.CropCareSteps.FindAsync(f.Id); if (item == null) return NotFound();
        var plant = await db.Plants.FindAsync(f.PlantId ?? 0); ViewBag.Plant = plant;
        if (plant == null || CropSeasonRules.PlanDate(plant.PlantingDate, f.PlannedDay) == null) ModelState.AddModelError(nameof(f.PlannedDay), "Cần lô cây có ngày trồng và ngày dự kiến nằm trong phạm vi hợp lệ.");
        if (!CropSeasonRules.Kinds.ContainsKey(f.Kind ?? "")) ModelState.AddModelError(nameof(f.Kind), "Chọn nhóm công việc hợp lệ.");
        if (!create && item.PlantId.HasValue && item.PlantId != f.PlantId) ModelState.AddModelError("", "Không chuyển việc dự kiến sang mùa vụ khác.");
        var hasActual = !create && await db.CropJournalEntries.AnyAsync(e => e.CareStepId == f.Id);
        var originalComplete = CropSeasonRules.PlanDate(plant?.PlantingDate, item.PlannedDay).HasValue && CropSeasonRules.Kinds.ContainsKey(item.Kind ?? "") && !string.IsNullOrWhiteSpace(item.Title) && !string.IsNullOrWhiteSpace(item.Instructions);
        if (hasActual && originalComplete && (item.Kind != f.Kind || item.PlannedDay != f.PlannedDay || item.Title != f.Title?.Trim() || item.Instructions != f.Instructions?.Trim())) ModelState.AddModelError("", "Việc đã có nhật ký nên giữ nguyên kế hoạch để đối chiếu. Hãy thêm việc dự kiến mới khi cần thay đổi.");
        if (hasActual && await db.CropJournalEntries.AnyAsync(e => e.CareStepId == f.Id && (e.PlantId != f.PlantId || e.Kind != f.Kind))) ModelState.AddModelError("", "Nhật ký đang liên kết chưa khớp lô hoặc nhóm công việc; sửa nhật ký trước khi bổ sung kế hoạch.");
        var title = f.Title?.Trim();
        if (await db.CropCareSteps.AnyAsync(s => s.Id != f.Id && s.PlantId == f.PlantId && s.Title == title && s.PlannedDay == f.PlannedDay && s.Kind == f.Kind)) ModelState.AddModelError(nameof(f.Title), "Việc này đã có trong kế hoạch cùng ngày.");
        if (!ModelState.IsValid) return View("Form", f);
        item.PlantId = f.PlantId; item.Title = f.Title?.Trim(); item.Kind = f.Kind; item.PlannedDay = f.PlannedDay; item.Instructions = f.Instructions?.Trim();
        if (create) { item.CreatedAt = DateTime.Now; db.CropCareSteps.Add(item); }
        await db.SaveChangesAsync(); await tx.CommitAsync(); TempData["Success"] = "Đã lưu việc dự kiến. Khi làm xong, bấm Ghi việc đã làm để ghi lượng và kết quả thực tế.";
        return RedirectToAction("Details", "Plants", new { id = item.PlantId });
    }
    public async Task<IActionResult> Delete(int id) { var s = await db.CropCareSteps.FindAsync(id); return s == null ? NotFound() : View(s); }
    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        await using var tx = await db.BeginBusinessWriteAsync(); var s = await db.CropCareSteps.FindAsync(id); if (s == null) return NotFound();
        if (await db.CropJournalEntries.AnyAsync(e => e.CareStepId == id)) TempData["Error"] = "Việc đã có nhật ký thực tế, cần giữ kế hoạch để so sánh.";
        else { db.CropCareSteps.Remove(s); await db.SaveChangesAsync(); await tx.CommitAsync(); TempData["Success"] = "Đã bỏ việc chưa thực hiện khỏi kế hoạch."; }
        return RedirectToAction("Details", "Plants", new { id = s.PlantId });
    }
}
