using Farm_Management.Data;
using Farm_Management.Models;
using Farm_Management.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Farm_Management.Controllers;

public class SeedsController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        var seeds = await db.Seeds.AsNoTracking().OrderBy(s => s.Name).ToListAsync();
        var types = await db.PlantTypes.AsNoTracking().ToDictionaryAsync(t => t.Id);
        foreach (var seed in seeds) seed.PlantType = seed.PlantTypeId.HasValue ? types.GetValueOrDefault(seed.PlantTypeId.Value) : null;
        return View(seeds);
    }
    public async Task<IActionResult> Create() { var f = new SeedForm(); await Options(f); return View("Form", f); }
    public async Task<IActionResult> Edit(int id)
    {
        var s = await db.Seeds.FindAsync(id); if (s == null) return NotFound();
        var f = new SeedForm { Id = s.Id, InputLotId = s.InputLotId, Name = s.Name, PlantTypeId = s.PlantTypeId, Origin = s.Origin, BatchNumber = s.BatchNumber, InvoiceNumber = s.InvoiceNumber, IsCertified = s.IsCertified, Notes = s.Notes };
        await Options(f); return View("Form", f);
    }
    [HttpPost, ValidateAntiForgeryToken] public Task<IActionResult> Create(SeedForm form) { form.Id = 0; return Save(form, true); }
    [HttpPost, ValidateAntiForgeryToken] public Task<IActionResult> Edit(int id, SeedForm form) => id != form.Id ? Task.FromResult<IActionResult>(NotFound()) : Save(form, false);
    private async Task<IActionResult> Save(SeedForm form, bool create)
    {
        await using var tx = await db.BeginBusinessWriteAsync(); form.Name = form.Name?.Trim();
        if (!await db.PlantTypes.AnyAsync(t => t.Id == form.PlantTypeId)) ModelState.AddModelError(nameof(form.PlantTypeId), "Loại cây không tồn tại.");
        if (await db.Seeds.IgnoreQueryFilters().AnyAsync(s => s.Id != form.Id && s.PlantTypeId == form.PlantTypeId && s.Name == form.Name)) ModelState.AddModelError(nameof(form.Name), "Giống này đã tồn tại trong loại cây.");
        if (!create && await db.Plants.IgnoreQueryFilters().AnyAsync(p => p.SeedId == form.Id && p.PlantTypeId != form.PlantTypeId)) ModelState.AddModelError(nameof(form.PlantTypeId), "Giống đang dùng cho lô cây thuộc loại khác. Điều chỉnh lô cây trước khi đổi loại.");
        if(form.InputLotId.HasValue && !await db.InputLots.AnyAsync(l=>l.Id==form.InputLotId && l.Kind=="Seed")) ModelState.AddModelError(nameof(form.InputLotId),"Lô giống đầu vào không tồn tại.");
        if (!ModelState.IsValid) { await Options(form); return View("Form", form); }
        var item = create ? new Seed() : await db.Seeds.FindAsync(form.Id); if (item == null) return NotFound();
        item.InputLotId = form.InputLotId; item.Name = form.Name; item.PlantTypeId = form.PlantTypeId; item.Origin = form.Origin; item.BatchNumber = form.BatchNumber; item.InvoiceNumber = form.InvoiceNumber; item.IsCertified = form.IsCertified; item.Notes = form.Notes;
        if (create) db.Seeds.Add(item); await db.SaveChangesAsync(); await tx.CommitAsync(); TempData["Success"] = "Đã lưu giống cây."; return RedirectToAction(nameof(Index));
    }
    public async Task<IActionResult> Delete(int id) { var s = await db.Seeds.FindAsync(id); return s == null ? NotFound() : View(s); }
    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        await using var tx = await db.BeginBusinessWriteAsync(); var item = await db.Seeds.FindAsync(id); if (item == null) return NotFound();
        if (await db.Plants.IgnoreQueryFilters().AnyAsync(p => p.SeedId == id)) TempData["Error"] = "Giống đang được lô cây sử dụng; không thể xóa.";
        else { db.Seeds.Remove(item); await db.SaveChangesAsync(); await tx.CommitAsync(); TempData["Success"] = "Đã xóa giống."; }
        return RedirectToAction(nameof(Index));
    }
    private async Task Options(SeedForm f) { ViewBag.Types = new SelectList(await db.PlantTypes.OrderBy(t => t.Name).ToListAsync(), "Id", "Name", f.PlantTypeId); ViewBag.Inputs = new SelectList(await db.InputLots.Where(l=>l.Kind=="Seed").Select(l=>new{l.Id,Text=l.Code+" · "+l.ProductName}).ToListAsync(),"Id","Text",f.InputLotId); }
}
