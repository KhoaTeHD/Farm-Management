using System.Globalization;
using Farm_Management.Data;
using Farm_Management.Models;
using Farm_Management.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Farm_Management.Services;

public static class MaterialUsageService
{
    public static List<MaterialUsageRow> FromJournal(IEnumerable<CropJournalEntry> entries) => entries
        .Where(e => e.IsDeleted != true && e.Kind is "Pesticide" or "Fertilize")
        .Select(e => new MaterialUsageRow
        {
            Kind = e.Kind,
            ProductName = e.Material,
            Date = e.Date,
            PerformedBy = e.Recorder,
            QuantityText = e.Amount.HasValue ? e.Amount.Value.ToString("0.##", CultureInfo.GetCultureInfo("vi-VN")) + " " + (string.IsNullOrWhiteSpace(e.Unit) ? "(chưa ghi đơn vị)" : e.Unit) : null,
            WithdrawalDays = e.WithdrawalDays,
            JournalEntryId = e.Id
        }).ToList();

    public static async Task<List<MaterialUsageRow>> FromLegacyAsync(ApplicationDbContext db,
        IReadOnlyCollection<FertilizationLog> fertilizers, IReadOnlyCollection<PesticideApplicationLog> sprays)
    {
        if (fertilizers.Count + sprays.Count == 0) return new();
        var fertilizerIds = fertilizers.Where(e => e.FertilizerId.HasValue).Select(e => e.FertilizerId!.Value).Distinct().ToList();
        var pesticideIds = sprays.Where(e => e.PesticideId.HasValue).Select(e => e.PesticideId!.Value).Distinct().ToList();
        var workerIds = fertilizers.Select(e => e.WorkerId).Concat(sprays.Select(e => e.WorkerId)).Where(i => i.HasValue).Select(i => i!.Value).Distinct().ToList();
        var names = await db.Fertilizers.AsNoTracking().Where(f => fertilizerIds.Contains(f.Id)).ToDictionaryAsync(f => f.Id, f => f.Name);
        var pesticides = await db.Pesticides.AsNoTracking().Where(p => pesticideIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id);
        var registryIds = pesticides.Values.Where(p => p.PesticideRegistryId.HasValue).Select(p => p.PesticideRegistryId!.Value).Distinct().ToList();
        var registry = await db.PesticideRegistries.AsNoTracking().Where(r => registryIds.Contains(r.Id)).ToDictionaryAsync(r => r.Id, r => r.TradeName);
        var workers = await db.Workers.AsNoTracking().Where(w => workerIds.Contains(w.Id)).ToDictionaryAsync(w => w.Id, w => w.FullName);
        var rows = fertilizers.Select(e => new MaterialUsageRow
        {
            Kind = "Fertilize",
            ProductName = e.FertilizerId.HasValue ? names.GetValueOrDefault(e.FertilizerId.Value) : null,
            Date = e.ApplicationDate,
            QuantityText = e.Dosage,
            PerformedBy = e.WorkerId.HasValue ? workers.GetValueOrDefault(e.WorkerId.Value) : null,
            IsLegacy = true
        }).ToList();
        foreach (var e in sprays)
        {
            var pesticide = e.PesticideId.HasValue ? pesticides.GetValueOrDefault(e.PesticideId.Value) : null;
            rows.Add(new()
            {
                Kind = "Pesticide",
                ProductName = pesticide?.PesticideRegistryId is int registryId ? registry.GetValueOrDefault(registryId) : null,
                Date = e.ApplicationDate,
                QuantityText = e.Dosage,
                WithdrawalDays = e.PHI_Days,
                PerformedBy = e.WorkerId.HasValue ? workers.GetValueOrDefault(e.WorkerId.Value) : null,
                IsLegacy = true
            });
        }
        return rows;
    }

    public static DateTime? WithdrawalEnd(MaterialUsageRow row)
    {
        if (!row.Date.HasValue || row.Date.Value.Year < 2 || row.WithdrawalDays is not (>= 0 and <= 36500)) return null;
        try { return row.Date.Value.Date.AddDays(row.WithdrawalDays.Value); }
        catch (ArgumentOutOfRangeException) { return null; }
    }
}
