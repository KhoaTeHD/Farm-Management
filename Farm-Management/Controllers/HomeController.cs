using System.Diagnostics;
using Farm_Management.Models;
using Microsoft.AspNetCore.Mvc;
using Farm_Management.Data;
using Farm_Management.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Farm_Management.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var plants = await _context.Plants.AsNoTracking()
                .OrderBy(p => p.Status == "Harvested" || p.Status == "Destroyed" ? 1 : 0)
                .ThenByDescending(p => p.PlantingDate).Take(6).ToListAsync();
            var ids = plants.Select(p => p.Id).ToList();
            var types = await _context.PlantTypes.AsNoTracking().ToDictionaryAsync(t => t.Id);
            var steps = await _context.CropCareSteps.AsNoTracking().Where(s => s.PlantId.HasValue && ids.Contains(s.PlantId.Value)).ToListAsync();
            var entries = await _context.CropJournalEntries.AsNoTracking().Where(e => e.PlantId.HasValue && ids.Contains(e.PlantId.Value)).ToListAsync();
            var seasons = new List<CropSeasonViewModel>();
            foreach (var p in plants)
            {
                p.PlantType = p.PlantTypeId.HasValue ? types.GetValueOrDefault(p.PlantTypeId.Value) : null;
                seasons.Add(new() { Plant = p, Steps = steps.Where(s => s.PlantId == p.Id).ToList(), Entries = entries.Where(e => e.PlantId == p.Id).ToList() });
            }

            var recentEntries = await _context.CropJournalEntries.AsNoTracking()
                .OrderByDescending(e => e.Date)
                .ThenByDescending(e => e.Id)
                .Take(6)
                .ToListAsync();
            var recentIds = recentEntries.Select(e => e.Id).ToList();
            var recentPlantIds = recentEntries.Where(e => e.PlantId.HasValue).Select(e => e.PlantId!.Value).Distinct().ToList();
            var recentPlants = recentPlantIds.Count == 0
                ? new Dictionary<int, Plant>()
                : await _context.Plants.AsNoTracking().Where(p => recentPlantIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id);
            var recentLinks = recentIds.Count == 0
                ? new List<CropJournalWorker>()
                : await _context.CropJournalWorkers.AsNoTracking()
                    .Where(w => w.CropJournalEntryId.HasValue && recentIds.Contains(w.CropJournalEntryId.Value))
                    .OrderBy(w => w.Id)
                    .ToListAsync();
            var recentWorkerIds = recentLinks.Where(w => w.WorkerId.HasValue).Select(w => w.WorkerId!.Value).Distinct().ToList();
            var recentWorkers = recentWorkerIds.Count == 0
                ? new Dictionary<int, Worker>()
                : await _context.Workers.AsNoTracking().Where(w => recentWorkerIds.Contains(w.Id)).ToDictionaryAsync(w => w.Id);
            var recentMatIds = recentEntries.Where(e => e.MaterialItemId.HasValue).Select(e => e.MaterialItemId!.Value).Distinct().ToList();
            var recentMats = recentMatIds.Count == 0
                ? new Dictionary<int, MaterialItem>()
                : await _context.MaterialItems.AsNoTracking().Where(m => recentMatIds.Contains(m.Id)).ToDictionaryAsync(m => m.Id);

            var recentWorks = recentEntries.Select(e =>
            {
                var names = recentLinks
                    .Where(l => l.CropJournalEntryId == e.Id)
                    .Select(l => l.WorkerId.HasValue && recentWorkers.TryGetValue(l.WorkerId.Value, out var w)
                        ? (!string.IsNullOrWhiteSpace(w.FullName) ? w.FullName : w.Code)
                        : l.WorkerName)
                    .Where(n => !string.IsNullOrWhiteSpace(n))
                    .Select(n => n!)
                    .Distinct()
                    .ToList();
                if (names.Count == 0 && !string.IsNullOrWhiteSpace(e.Recorder))
                    names.Add(e.Recorder);
                var p = e.PlantId.HasValue ? recentPlants.GetValueOrDefault(e.PlantId.Value) : null;
                var mat = e.MaterialItemId.HasValue && recentMats.TryGetValue(e.MaterialItemId.Value, out var m) ? m.Name : e.Material;
                return new DailyWorkRow
                {
                    Entry = e,
                    PlantName = p?.SeasonName ?? p?.BatchCode,
                    PlantBatchCode = p?.BatchCode,
                    WorkerNames = names,
                    MaterialDisplay = mat
                };
            }).ToList();

            return View(new DashboardViewModel
            {
                SeasonCount = await _context.Plants.CountAsync(),
                GrowingSeasons = await _context.Plants.CountAsync(p => p.Status == "Growing" || p.Status == "ReadyToHarvest"),
                JournalCount = await _context.CropJournalEntries.CountAsync(),
                TodayWorkCount = await _context.CropJournalEntries.CountAsync(e => e.Date == DateTime.Today),
                MaterialCount = await _context.MaterialItems.CountAsync(m => m.IsActive == true),
                ActiveWorkerCount = await _context.Workers.CountAsync(w => w.IsActive == true),
                Seasons = seasons,
                RecentWorks = recentWorks
            });
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [Microsoft.AspNetCore.Authorization.AllowAnonymous]
        public IActionResult Status(int code)
        {
            code = code is >= 400 and <= 599 ? code : 404;
            Response.StatusCode = code;
            return View(code);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        [Microsoft.AspNetCore.Authorization.AllowAnonymous]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
