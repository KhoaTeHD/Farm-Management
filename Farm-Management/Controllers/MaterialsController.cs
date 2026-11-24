using Farm_Management.Data;
using Farm_Management.Models;
using Farm_Management.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Farm_Management.Controllers;

public class MaterialsController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        var items = await db.MaterialItems.AsNoTracking()
            .OrderByDescending(m => m.IsActive == true)
            .ThenBy(m => m.Category)
            .ThenBy(m => m.Name)
            .ToListAsync();

        ViewBag.UsageCounts = await db.CropJournalEntries.AsNoTracking()
            .Where(e => e.MaterialItemId.HasValue)
            .GroupBy(e => e.MaterialItemId!.Value)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);

        return View(items);
    }

    public IActionResult Create() => View("Form", new MaterialItemForm());

    public async Task<IActionResult> Edit(int id)
    {
        var item = await db.MaterialItems.FindAsync(id);
        if (item == null) return NotFound();
        return View("Form", new MaterialItemForm
        {
            Id = item.Id,
            Name = item.Name,
            Category = item.Category ?? "Fertilizer",
            DefaultUnit = item.DefaultUnit,
            Notes = item.Notes,
            IsActive = item.IsActive ?? true
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Create(MaterialItemForm form)
    {
        form.Id = 0;
        return Save(form, true);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Edit(int id, MaterialItemForm form) =>
        id != form.Id ? Task.FromResult<IActionResult>(NotFound()) : Save(form, false);

    private async Task<IActionResult> Save(MaterialItemForm form, bool create)
    {
        await using var tx = await db.BeginBusinessWriteAsync();
        form.Name = form.Name?.Trim();
        form.DefaultUnit = string.IsNullOrWhiteSpace(form.DefaultUnit) ? null : form.DefaultUnit.Trim();
        form.Notes = string.IsNullOrWhiteSpace(form.Notes) ? null : form.Notes.Trim();

        var item = create ? new MaterialItem() : await db.MaterialItems.FindAsync(form.Id);
        if (item == null) return NotFound();

        var normalized = form.Name?.ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(normalized) &&
            await db.MaterialItems.AnyAsync(m => m.Id != form.Id && m.Name != null && m.Name.ToLower() == normalized))
        {
            ModelState.AddModelError(nameof(form.Name), "Tên vật liệu này đã có trong danh mục.");
        }

        if (!ModelState.IsValid) return View("Form", form);

        item.Name = form.Name;
        item.Category = form.Category;
        item.DefaultUnit = form.DefaultUnit;
        item.Notes = form.Notes;
        item.IsActive = create ? true : form.IsActive;
        if (create)
        {
            item.CreatedAt = DateTime.Now;
            db.MaterialItems.Add(item);
        }

        await db.SaveChangesAsync();
        await tx.CommitAsync();
        TempData["Success"] = $"Đã lưu vật liệu \"{item.Name}\".";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int id)
    {
        var item = await db.MaterialItems.FindAsync(id);
        return item == null ? NotFound() : View(item);
    }

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        await using var tx = await db.BeginBusinessWriteAsync();
        var item = await db.MaterialItems.FindAsync(id);
        if (item == null) return NotFound();

        if (await db.CropJournalEntries.IgnoreQueryFilters().AnyAsync(e => e.MaterialItemId == id))
        {
            TempData["Error"] = "Vật liệu đã được chọn trong việc hằng ngày; không thể xóa. Bạn có thể chuyển sang 'Ngừng sử dụng'.";
        }
        else
        {
            db.MaterialItems.Remove(item);
            await db.SaveChangesAsync();
            await tx.CommitAsync();
            TempData["Success"] = "Đã xóa vật liệu khỏi danh mục.";
        }

        return RedirectToAction(nameof(Index));
    }
}
