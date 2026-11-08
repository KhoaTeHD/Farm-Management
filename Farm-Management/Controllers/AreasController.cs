using Farm_Management.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Farm_Management.Data;
using Farm_Management.Models;

namespace Farm_Management.Controllers
{
    public class AreasController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AreasController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Areas
        //Hiển thị danh sách vùng rồng
        public async Task<IActionResult> Index()
        {
            var areas = await _context.Areas.ToListAsync();
            return View(areas);
        }

        // GET: Areas/Details/5
        //Xem chi tiết vùng trồng
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var area = await _context.Areas
                .FirstOrDefaultAsync(m => m.Id == id);
            
            if (area == null) return NotFound();

            area.Plants = await _context.Plants.Where(p => p.AreaId == area.Id).ToListAsync();
            return View(area);
        }

        // GET: Areas/Create
        // Tạo mới vùng trồng
        public IActionResult Create()
        {
            return View(new AreaForm());
        }

        // POST: Areas/Create
        // xử lý form tạo mới vùng trồng
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Acreage,GpsCoordinates,SoilType,SoilTestResult,SoilTestDate,Notes")] AreaForm form)
        {
            var area = form.ToEntity();
            await using var transaction = await _context.BeginBusinessWriteAsync();
            if (ModelState.IsValid)
            {
                _context.Add(area);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                TempData["Success"] = "Thêm vùng trồng thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(AreaForm.FromEntity(area));
        }

        // GET: Areas/Edit/5
        // chỉnh sửa vùng trồng
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var area = await _context.Areas.FindAsync(id);
            if (area == null) return NotFound();

            return View(AreaForm.FromEntity(area));
        }

        // POST: Areas/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Acreage,GpsCoordinates,SoilType,SoilTestResult,SoilTestDate,Notes,CreatedAt")] AreaForm form)
        {
            var area = form.ToEntity();
            await using var transaction = await _context.BeginBusinessWriteAsync();
            if (id != area.Id) return NotFound();
            var existing = await _context.Areas.FirstOrDefaultAsync(x => x.Id == id);
            if (existing == null) return NotFound();
            area.CreatedAt = existing.CreatedAt;
            area.Revision = existing.Revision; area.IsDeleted = existing.IsDeleted; area.DeletedAt = existing.DeletedAt;

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Entry(existing).CurrentValues.SetValues(area);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    TempData["Success"] = "Cập nhật vùng trồng thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AreaExists(area.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(AreaForm.FromEntity(area));
        }

        // GET: Areas/Delete/5
        // Hiển thị trang xác nhận xóa
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var area = await _context.Areas
                .FirstOrDefaultAsync(a => a.Id == id);
            if (area == null) return NotFound();

            return View(area);
        }

        // POST: Areas/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await using var transaction = await _context.BeginBusinessWriteAsync();
            var area = await _context.Areas.FindAsync(id);
            if (area != null)
            {
                // Kiểm tra có Plant nào đang dùng Area này không
                var hasPlants = await _context.Plants.IgnoreQueryFilters().AnyAsync(p => p.AreaId == id);
                if (hasPlants || await _context.FieldResources.IgnoreQueryFilters().AnyAsync(r=>r.AreaId==id))
                {
                    TempData["Error"] = "Không thể xóa vùng trồng đang có lô cây!";
                    return RedirectToAction(nameof(Index));
                }

                _context.Areas.Remove(area);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                TempData["Success"] = "Xóa vùng trồng thành công!";
            }
            return RedirectToAction(nameof(Index));
        }

        private bool AreaExists(int id)
        {
            return _context.Areas.Any(e => e.Id == id);
        }
    }
}
