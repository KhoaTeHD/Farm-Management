using Farm_Management.Data;
using Farm_Management.Models;
using Farm_Management.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Farm_Management.Controllers;

public class PlantTypesController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index() => View(await db.PlantTypes.AsNoTracking().OrderBy(t => t.Name).ToListAsync());
    public IActionResult Create() => View("Form", new PlantTypeForm());
    public async Task<IActionResult> Edit(int id)
    {
        var t = await db.PlantTypes.FindAsync(id); return t == null ? NotFound() : View("Form", new PlantTypeForm { Id = t.Id, Name = t.Name, GrowthCycleDays = t.GrowthCycleDays, Description = t.Description });
    }
    [HttpPost, ValidateAntiForgeryToken] public Task<IActionResult> Create(PlantTypeForm form) { form.Id = 0; return Save(form, true); }
    [HttpPost, ValidateAntiForgeryToken] public Task<IActionResult> Edit(int id, PlantTypeForm form) => id != form.Id ? Task.FromResult<IActionResult>(NotFound()) : Save(form, false);
    private async Task<IActionResult> Save(PlantTypeForm form, bool create)
    {
        await using var tx = await db.BeginBusinessWriteAsync(); form.Name = form.Name?.Trim();
        if (await db.PlantTypes.IgnoreQueryFilters().AnyAsync(t => t.Id != form.Id && t.Name == form.Name)) ModelState.AddModelError(nameof(form.Name), "Tên loại cây đã tồn tại.");
        if (!ModelState.IsValid) return View("Form", form);
        var item = create ? new PlantType() : await db.PlantTypes.FindAsync(form.Id); if (item == null) return NotFound();
        item.Name = form.Name; item.GrowthCycleDays = form.GrowthCycleDays; item.Description = form.Description;
        if (create) db.PlantTypes.Add(item); await db.SaveChangesAsync(); await tx.CommitAsync();
        TempData["Success"] = "Đã lưu loại cây."; return RedirectToAction(nameof(Index));
    }
    public async Task<IActionResult> Delete(int id) { var t = await db.PlantTypes.FindAsync(id); return t == null ? NotFound() : View(t); }
    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        await using var tx = await db.BeginBusinessWriteAsync(); var item = await db.PlantTypes.FindAsync(id); if (item == null) return NotFound();
        if (await db.Seeds.IgnoreQueryFilters().AnyAsync(s => s.PlantTypeId == id) || await db.Plants.IgnoreQueryFilters().AnyAsync(p => p.PlantTypeId == id)) TempData["Error"] = "Loại cây đang được giống hoặc lô cây sử dụng; không thể xóa.";
        else { db.PlantTypes.Remove(item); await db.SaveChangesAsync(); await tx.CommitAsync(); TempData["Success"] = "Đã xóa loại cây."; }
        return RedirectToAction(nameof(Index));
    }
}
