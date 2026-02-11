using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Farm_Management.Data;
using Farm_Management.Models;

namespace Farm_Management.Controllers
{
    public class AttendanceController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AttendanceController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Attendance
        // Hiển thị chấm công theo ngày (mặc định: hôm nay)
        public async Task<IActionResult> Index(DateTime? date)
        {
            var selectedDate = date ?? DateTime.Today;
            ViewBag.SelectedDate = selectedDate;

            var logs = await _context.AttendanceLogs
                .Include(a => a.Worker)
                .Where(a => a.Date == selectedDate)
                .OrderBy(a => a.Worker.Code)
                .ToListAsync();

            // Lấy danh sách workers active (chưa chấm công hôm nay)
            var workerIdsWithLogs = logs.Select(l => l.WorkerId).ToList();
            var availableWorkers = await _context.Workers
                .Where(w => w.IsActive && !workerIdsWithLogs.Contains(w.Id))
                .OrderBy(w => w.Code)
                .ToListAsync();

            ViewBag.AvailableWorkers = availableWorkers;

            return View(logs);
        }

        // POST: Attendance/MarkAttendance
        // Quick mark attendance cho nhiều workers
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAttendance(DateTime date, int[] workerIds, decimal[] hours, string[] notes)
        {
            if (workerIds == null || workerIds.Length == 0)
            {
                TempData["Error"] = "Chưa chọn công nhân nào";
                return RedirectToAction(nameof(Index), new { date });
            }

            var successCount = 0;
            var errors = new List<string>();

            for (int i = 0; i < workerIds.Length; i++)
            {
                var workerId = workerIds[i];
                var hoursWorked = hours[i];
                var note = notes?[i];

                // Validate
                if (hoursWorked < 0 || hoursWorked > 24)
                {
                    errors.Add($"Công nhân ID {workerId}: Số giờ không hợp lệ");
                    continue;
                }

                // Check duplicate
                var exists = await _context.AttendanceLogs
                    .AnyAsync(a => a.WorkerId == workerId && a.Date == date);

                if (exists)
                {
                    errors.Add($"Công nhân ID {workerId}: Đã chấm công ngày này");
                    continue;
                }

                // Classify work type
                var workType = ClassifyWorkType(hoursWorked);

                var log = new AttendanceLog
                {
                    WorkerId = workerId,
                    Date = date,
                    HoursWorked = hoursWorked,
                    WorkType = workType,
                    Notes = note
                };

                _context.AttendanceLogs.Add(log);
                successCount++;
            }

            await _context.SaveChangesAsync();

            if (successCount > 0)
            {
                TempData["Success"] = $"Đã chấm công {successCount} công nhân";
            }
            if (errors.Any())
            {
                TempData["Error"] = string.Join("<br>", errors);
            }

            return RedirectToAction(nameof(Index), new { date });
        }

        // GET: Attendance/Create
        public async Task<IActionResult> Create(DateTime? date)
        {
            var selectedDate = date ?? DateTime.Today;
            ViewBag.SelectedDate = selectedDate;

            // Lấy workers chưa chấm công ngày này
            var workerIdsWithLogs = await _context.AttendanceLogs
                .Where(a => a.Date == selectedDate)
                .Select(a => a.WorkerId)
                .ToListAsync();

            ViewData["WorkerId"] = new SelectList(
                await _context.Workers
                    .Where(w => w.IsActive && !workerIdsWithLogs.Contains(w.Id))
                    .OrderBy(w => w.Code)
                    .ToListAsync(),
                "Id", "FullName");

            return View();
        }

        // POST: Attendance/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("WorkerId,Date,HoursWorked,Notes")] AttendanceLog attendanceLog)
        {
            if (ModelState.IsValid)
            {
                // Check duplicate
                var exists = await _context.AttendanceLogs
                    .AnyAsync(a => a.WorkerId == attendanceLog.WorkerId && a.Date == attendanceLog.Date);

                if (exists)
                {
                    ModelState.AddModelError("", "Công nhân đã được chấm công vào ngày này");
                    await LoadWorkersDropdown(attendanceLog.Date);
                    return View(attendanceLog);
                }

                // Auto-classify
                attendanceLog.WorkType = ClassifyWorkType(attendanceLog.HoursWorked);

                _context.Add(attendanceLog);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Chấm công thành công!";
                return RedirectToAction(nameof(Index), new { date = attendanceLog.Date });
            }

            await LoadWorkersDropdown(attendanceLog.Date);
            return View(attendanceLog);
        }

        // GET: Attendance/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var log = await _context.AttendanceLogs
                .Include(a => a.Worker)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (log == null) return NotFound();

            return View(log);
        }

        // POST: Attendance/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,WorkerId,Date,HoursWorked,Notes,CreatedAt")] AttendanceLog attendanceLog)
        {
            if (id != attendanceLog.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // Auto-classify
                    attendanceLog.WorkType = ClassifyWorkType(attendanceLog.HoursWorked);

                    _context.Update(attendanceLog);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Cập nhật chấm công thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AttendanceLogExists(attendanceLog.Id))
                        return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index), new { date = attendanceLog.Date });
            }
            return View(attendanceLog);
        }

        // GET: Attendance/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var log = await _context.AttendanceLogs
                .Include(a => a.Worker)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (log == null) return NotFound();

            return View(log);
        }

        // POST: Attendance/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var log = await _context.AttendanceLogs.FindAsync(id);
            if (log != null)
            {
                var date = log.Date;
                _context.AttendanceLogs.Remove(log);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Xóa chấm công thành công!";
                return RedirectToAction(nameof(Index), new { date });
            }
            return RedirectToAction(nameof(Index));
        }

        // Helper: Classify work type based on hours
        private string ClassifyWorkType(decimal hours)
        {
            if (hours >= 8)
                return "FullDay"; // 1 công
            else if (hours == 4)
                return "HalfDay"; // 0.5 công
            else
                return "OddHours"; // Giờ lẻ
        }

        private bool AttendanceLogExists(int id)
        {
            return _context.AttendanceLogs.Any(e => e.Id == id);
        }

        private async Task LoadWorkersDropdown(DateTime date)
        {
            var workerIdsWithLogs = await _context.AttendanceLogs
                .Where(a => a.Date == date)
                .Select(a => a.WorkerId)
                .ToListAsync();

            ViewData["WorkerId"] = new SelectList(
                await _context.Workers
                    .Where(w => w.IsActive && !workerIdsWithLogs.Contains(w.Id))
                    .OrderBy(w => w.Code)
                    .ToListAsync(),
                "Id", "FullName");
        }
    }
}