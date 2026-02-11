using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Farm_Management.Data;
using Farm_Management.Models;

namespace Farm_Management.Controllers
{
    public class WorkersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public WorkersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Workers
        // Hiển thị danh sách công nhân (active first, then by code)
        public async Task<IActionResult> Index()
        {
            var workers = await _context.Workers
                .OrderByDescending(w => w.IsActive)
                .ThenBy(w => w.Code)
                .ToListAsync();
            return View(workers);
        }

        // GET: Workers/Create
        // Tạo mới công nhân
        public IActionResult Create()
        {
            return View();
        }

        // POST: Workers/Create
        // Xử lý form tạo mới công nhân
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Code,FullName,Phone,DailySalary,HireDate,IsActive")] Worker worker)
        {
            if (ModelState.IsValid)
            {
                // Check duplicate Code
                if (await _context.Workers.AnyAsync(w => w.Code == worker.Code))
                {
                    ModelState.AddModelError("Code", "Mã công nhân đã tồn tại");
                    return View(worker);
                }

                // Validate salary > 0
                if (worker.DailySalary <= 0)
                {
                    ModelState.AddModelError("DailySalary", "Lương ngày phải lớn hơn 0");
                    return View(worker);
                }

                _context.Add(worker);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Thêm công nhân {worker.FullName} thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(worker);
        }

        // GET: Workers/Edit/5
        // Chỉnh sửa công nhân
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var worker = await _context.Workers.FindAsync(id);
            if (worker == null) return NotFound();

            return View(worker);
        }

        // POST: Workers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Code,FullName,Phone,DailySalary,HireDate,IsActive,CreatedAt")] Worker worker)
        {
            if (id != worker.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // Check duplicate Code (exclude current)
                    if (await _context.Workers.AnyAsync(w => w.Code == worker.Code && w.Id != id))
                    {
                        ModelState.AddModelError("Code", "Mã công nhân đã tồn tại");
                        return View(worker);
                    }

                    // Validate salary > 0
                    if (worker.DailySalary <= 0)
                    {
                        ModelState.AddModelError("DailySalary", "Lương ngày phải lớn hơn 0");
                        return View(worker);
                    }

                    _context.Update(worker);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = $"Cập nhật công nhân {worker.FullName} thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!WorkerExists(worker.Id))
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
            return View(worker);
        }

        // GET: Workers/Delete/5
        // Hiển thị trang xác nhận xóa
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var worker = await _context.Workers
                .FirstOrDefaultAsync(w => w.Id == id);
            if (worker == null) return NotFound();

            return View(worker);
        }

        // POST: Workers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var worker = await _context.Workers.FindAsync(id);
            if (worker != null)
            {
                // Kiểm tra có logs nào đang dùng worker này không
                // Rule: Không được xóa nếu có lịch sử làm việc (VietGAP traceability)
                var hasPesticideLogs = await _context.PesticideApplicationLogs.AnyAsync(l => l.WorkerId == id);
                var hasFertilizerLogs = await _context.FertilizationLogs.AnyAsync(l => l.WorkerId == id);
                var hasWaterLogs = await _context.WaterIrrigationLogs.AnyAsync(l => l.WorkerId == id);
                var hasHarvestLogs = await _context.HarvestBatches.AnyAsync(h => h.WorkerId == id);

                if (hasPesticideLogs || hasFertilizerLogs || hasWaterLogs || hasHarvestLogs)
                {
                    TempData["Error"] = $"Không thể xóa công nhân {worker.FullName} vì đã có lịch sử làm việc. " +
                                       "Bạn có thể đặt trạng thái 'Nghỉ việc' thay vì xóa để lưu trữ dữ liệu.";
                    return RedirectToAction(nameof(Index));
                }

                // Xóa nếu không có lịch sử
                _context.Workers.Remove(worker);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Xóa công nhân {worker.FullName} thành công!";
            }
            return RedirectToAction(nameof(Index));
        }

        private bool WorkerExists(int id)
        {
            return _context.Workers.Any(e => e.Id == id);
        }
    }
}
