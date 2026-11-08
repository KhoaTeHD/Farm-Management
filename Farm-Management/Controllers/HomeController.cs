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
            return View(new DashboardViewModel
            {
                SeasonCount = await _context.Plants.CountAsync(),
                GrowingSeasons = await _context.Plants.CountAsync(p => p.Status == "Growing" || p.Status == "ReadyToHarvest"),
                JournalCount = await _context.CropJournalEntries.CountAsync(),
                Seasons = seasons
            });
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Status(int code)
        {
            code = code is >= 400 and <= 599 ? code : 404;
            Response.StatusCode = code;
            return View(code);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
