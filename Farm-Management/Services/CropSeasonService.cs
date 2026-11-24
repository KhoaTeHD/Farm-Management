using Farm_Management.Data;
using Farm_Management.Models;
using Farm_Management.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Farm_Management.Services;

public class CropSeasonService(ApplicationDbContext db)
{
    public async Task<CropSeasonViewModel?> LoadAsync(int id)
    {
        var plant = await db.Plants.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id); if (plant == null) return null;
        plant.Area = plant.AreaId.HasValue ? await db.Areas.FindAsync(plant.AreaId.Value) : null;
        plant.PlantType = plant.PlantTypeId.HasValue ? await db.PlantTypes.FindAsync(plant.PlantTypeId.Value) : null;
        plant.Seed = plant.SeedId.HasValue ? await db.Seeds.FindAsync(plant.SeedId.Value) : null;
        var model = new CropSeasonViewModel
        {
            Plant = plant,
            Entries = await db.CropJournalEntries.AsNoTracking().Where(e => e.PlantId == id).OrderByDescending(e => e.Date).ThenByDescending(e => e.Id).ToListAsync(),
            Steps = await db.CropCareSteps.AsNoTracking().Where(s => s.PlantId == id).OrderBy(s => s.PlannedDay).ThenBy(s => s.Id).ToListAsync()
        };
        var sprays = await db.PesticideApplicationLogs.AsNoTracking().Where(l => l.PlantId == id).ToListAsync();
        var water = await db.WaterIrrigationLogs.AsNoTracking().Where(l => l.PlantId == id).ToListAsync();
        var fertilizer = await db.FertilizationLogs.AsNoTracking().Where(l => l.PlantId == id).ToListAsync();
        var harvests = await db.HarvestBatches.AsNoTracking().Where(l => l.PlantId == id).ToListAsync();
        var entryIds = model.Entries.Select(e => e.Id).ToList();
        if (entryIds.Count > 0)
        {
            var links = await db.CropJournalWorkers.AsNoTracking()
                .Where(w => w.CropJournalEntryId.HasValue && entryIds.Contains(w.CropJournalEntryId.Value))
                .OrderBy(w => w.Id)
                .ToListAsync();
            var workerIds = links.Where(w => w.WorkerId.HasValue).Select(w => w.WorkerId!.Value).Distinct().ToList();
            var workers = workerIds.Count > 0
                ? await db.Workers.AsNoTracking().Where(w => workerIds.Contains(w.Id)).ToDictionaryAsync(w => w.Id)
                : new Dictionary<int, Worker>();
            foreach (var group in links.GroupBy(l => l.CropJournalEntryId!.Value))
            {
                var names = group.Select(l =>
                {
                    if (l.WorkerId.HasValue && workers.TryGetValue(l.WorkerId.Value, out var w))
                        return !string.IsNullOrWhiteSpace(w.FullName) ? w.FullName : w.Code;
                    return l.WorkerName;
                }).Where(n => !string.IsNullOrWhiteSpace(n)).Select(n => n!).Distinct().ToList();
                if (names.Count > 0) model.EntryWorkers[group.Key] = names;
            }
        }
        model.MaterialUses = MaterialUsageService.FromJournal(model.Entries, model.EntryWorkers);
        model.MaterialUses.AddRange(await MaterialUsageService.FromLegacyAsync(db, fertilizer, sprays));
        var safety = CropRules.Safety(sprays.Concat(model.Entries.Where(e => e.Kind == "Pesticide" && (e.WithdrawalDays.HasValue || !e.Date.HasValue)).Select(CropSeasonRules.AsSpray)));
        model.SafeDate = safety.SafeDate;
        // A manually recorded older limit must never be shortened by new journals.
        if (plant.ExpectedSafeHarvestDate.HasValue && (!model.SafeDate.HasValue || plant.ExpectedSafeHarvestDate > model.SafeDate)) model.SafeDate = plant.ExpectedSafeHarvestDate;
        model.SafetyIncomplete = safety.Incomplete;
        model.IncompleteEntries = model.Entries.Count(e => !CropSeasonRules.Complete(e, plant, model.HasWorkers(e.Id))
            || (e.CareStepId.HasValue && !model.Steps.Any(s => s.Id == e.CareStepId && s.Kind == e.Kind)));
        var newHarvests = model.Entries.Where(e => e.Kind == "Harvest").ToList();
        model.LegacyHarvestCount = harvests.Count;
        model.HarvestIncomplete = !plant.PlantingDate.HasValue || harvests.Any(h => !h.YieldKg.HasValue || h.YieldKg <= 0 || !h.HarvestDate.HasValue || h.HarvestDate < plant.PlantingDate || h.HarvestDate > DateTime.Today)
            || newHarvests.Any(e => !CropSeasonRules.Complete(e, plant, model.HasWorkers(e.Id))
                || (e.Amount.HasValue && (e.Amount <= 0 || (!string.IsNullOrWhiteSpace(e.Unit) && e.Unit != "kg"))));
        if (!model.HarvestIncomplete && (newHarvests.Any(e => e.Amount.HasValue) || harvests.Count > 0))
            model.HarvestKg = newHarvests.Where(e => e.Amount.HasValue).Sum(e => e.Amount!.Value) + harvests.Sum(h => h.YieldKg!.Value);
        model.Timeline.AddRange(model.Entries.Select(e => new CropTimelineItem(e.Date, e.Kind ?? "", CropSeasonRules.KindLabel(e.Kind), e.Summary, e.Id)));
        model.Timeline.AddRange(sprays.Select(l => new CropTimelineItem(l.ApplicationDate, "Pesticide", "Dùng thuốc · sổ cũ", $"{l.Dosage ?? "Chưa rõ liều"} · {l.Method} · Cách ly {l.PHI_Days?.ToString() ?? "chưa rõ"} ngày · {l.Notes}")));
        model.Timeline.AddRange(water.Select(l => new CropTimelineItem(l.IrrigationDate, "Water", "Tưới nước · sổ cũ", $"{l.WaterAmount?.ToString("0.##") ?? "Chưa rõ lượng"} lít · {l.Method} · {l.Notes}")));
        model.Timeline.AddRange(fertilizer.Select(l => new CropTimelineItem(l.ApplicationDate, "Fertilize", "Bón phân · sổ cũ", $"{l.Dosage ?? "Chưa rõ liều"} · {l.Method} · {l.Notes}")));
        model.Timeline.AddRange(harvests.Select(l => new CropTimelineItem(l.HarvestDate, "Harvest", "Thu hoạch · sổ cũ", $"{l.YieldKg?.ToString("0.##") ?? "Chưa rõ sản lượng"} kg · {l.QualityGrade} · {l.Notes}")));
        model.Timeline = model.Timeline.OrderByDescending(t => t.Date).ThenByDescending(t => t.EntryId).ToList();
        return model;
    }

    public async Task<(DateTime? SafeDate, bool Incomplete)> SafetyAsync(int plantId, DateTime? harvestDate = null, int excludeId = 0, CropJournalEntry? proposed = null)
    {
        var legacy = await db.PesticideApplicationLogs.Where(l => l.PlantId == plantId).ToListAsync();
        var entries = await db.CropJournalEntries.Where(e => e.PlantId == plantId && e.Kind == "Pesticide" && (e.WithdrawalDays.HasValue || !e.Date.HasValue) && e.Id != excludeId).ToListAsync();
        if (proposed?.Kind == "Pesticide" && (proposed.WithdrawalDays.HasValue || !proposed.Date.HasValue)) entries.Add(proposed);
        var sprays = legacy.Concat(entries.Select(CropSeasonRules.AsSpray));
        if (harvestDate.HasValue) sprays = sprays.Where(s => !s.ApplicationDate.HasValue || s.ApplicationDate.Value.Date <= harvestDate.Value.Date);
        return CropRules.Safety(sprays);
    }
}
