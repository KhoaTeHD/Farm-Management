using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Farm_Management.Data;
using Farm_Management.Models;
using Microsoft.EntityFrameworkCore;

namespace Farm_Management.Services;

public class TraceSnapshot
{
    public required int PlantId { get; set; }
    public required string? LotCode { get; set; }
    public required string? Season { get; set; }
    public required string? Crop { get; set; }
    public required string? Seed { get; set; }
    public required string? SeedOrigin { get; set; }
    public required string? Area { get; set; }
    public required DateTime? PlantingDate { get; set; }
    public required DateTime HarvestDate { get; set; }
    public required decimal WeightKg { get; set; }
    public required string? Quality { get; set; }
    public required List<CropJournalEntry> Journal { get; set; }
    public required List<InputLot> Inputs { get; set; }
    public required List<FieldResource> Resources { get; set; }
    public required List<InspectionReport> Reports { get; set; }
    public required List<string> LegacyNotes { get; set; }
    public required List<string> Warnings { get; set; }
    // Optional for compatibility: older saved snapshots contain only the original legacy notes.
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public List<Farm_Management.Models.ViewModels.MaterialUsageRow>? LegacyMaterialUses { get; set; }
}
public class HarvestTraceService(ApplicationDbContext db)
{
    public static string Hash(string json) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)));
    public static TraceSnapshot? Read(HarvestTrace trace)
    {
        if (trace.SnapshotJson == null || trace.SourceHash?.Length != 64 || Hash(trace.SnapshotJson) != trace.SourceHash) return null;
        try
        {
            var s = JsonSerializer.Deserialize<TraceSnapshot>(trace.SnapshotJson);
            return s == null || s.PlantId != trace.PlantId || s.HarvestDate != trace.HarvestDate || s.WeightKg != trace.WeightKg || s.WeightKg <= 0 || s.Journal == null || s.Journal.Any(e=>e==null) || s.Inputs == null || s.Resources == null || s.Reports == null || s.LegacyNotes == null || s.Warnings == null || s.LegacyMaterialUses?.Any(e=>e==null)==true ? null : s;
        }
        catch (JsonException) { return null; }
    }
    public async Task<TraceSnapshot?> BuildAsync(int plantId, DateTime? date, decimal? weight, string? quality, int? traceId = null)
    {
        if (!date.HasValue || date.Value.Year < 2 || date.Value.Date > DateTime.Today || !weight.HasValue || weight <= 0) return null;
        var p = await db.Plants.AsNoTracking().FirstOrDefaultAsync(p => p.Id == plantId); if (p == null || !p.PlantingDate.HasValue || p.PlantingDate > date) return null;
        var area = await db.Areas.FindAsync(p.AreaId ?? 0); var type = await db.PlantTypes.FindAsync(p.PlantTypeId ?? 0); var seed = await db.Seeds.FindAsync(p.SeedId ?? 0);
        var journal = await db.CropJournalEntries.AsNoTracking().Where(e => e.PlantId == plantId && (e.Date == null || e.Date < date.Value.Date.AddDays(1))).OrderBy(e => e.Date).ThenBy(e => e.Id).ToListAsync();
        var ids = journal.Where(e => e.InputLotId.HasValue).Select(e => e.InputLotId!.Value).Distinct().ToList();
        if (seed?.InputLotId != null) ids.Add(seed.InputLotId.Value);
        var inputs = await db.InputLots.IgnoreQueryFilters().AsNoTracking().Where(l => ids.Contains(l.Id)).OrderBy(l => l.Id).ToListAsync();
        var resources = await db.FieldResources.IgnoreQueryFilters().AsNoTracking().Where(r => r.AreaId == p.AreaId).OrderBy(r => r.Id).ToListAsync();
        var resourceIds = resources.Select(r => r.Id).ToList();
        var reports = await db.InspectionReports.AsNoTracking().Where(r => (r.TargetType == "InputLot" && r.TargetId.HasValue && ids.Contains(r.TargetId.Value)) || (r.TargetType == "FieldResource" && r.TargetId.HasValue && resourceIds.Contains(r.TargetId.Value)) || (traceId.HasValue && r.TargetType == "HarvestTrace" && r.TargetId == traceId)).OrderBy(r => r.Id).ToListAsync();
        var s = new TraceSnapshot { PlantId = p.Id, LotCode = p.BatchCode, Season = p.SeasonName, Crop = type?.Name, Seed = seed?.Name, SeedOrigin = seed?.Origin, Area = area?.Name, PlantingDate = p.PlantingDate, HarvestDate = date.Value.Date, WeightKg = weight.Value, Quality = quality, Journal = journal, Inputs = inputs, Resources = resources, Reports = reports, LegacyNotes = new(), Warnings = new() };
        var oldWater = await db.WaterIrrigationLogs.AsNoTracking().Where(e => e.PlantId == plantId && (e.IrrigationDate == null || e.IrrigationDate <= date)).OrderBy(e => e.Id).ToListAsync();
        var oldFert = await db.FertilizationLogs.AsNoTracking().Where(e => e.PlantId == plantId && (e.ApplicationDate == null || e.ApplicationDate <= date)).OrderBy(e => e.Id).ToListAsync();
        var oldSprays = await db.PesticideApplicationLogs.AsNoTracking().Where(e => e.PlantId == plantId && (e.ApplicationDate == null || e.ApplicationDate <= date)).OrderBy(e => e.Id).ToListAsync();
        if(oldFert.Count + oldSprays.Count > 0) s.LegacyMaterialUses = await MaterialUsageService.FromLegacyAsync(db, oldFert, oldSprays);
        s.LegacyNotes.AddRange(oldWater.Select(e => $"Tưới #{e.Id} · {e.IrrigationDate:dd/MM/yyyy} · {e.WaterAmount} lít · nguồn cũ #{e.WaterSourceId} · {e.Notes}"));
        s.LegacyNotes.AddRange(oldFert.Select(e => $"Bón #{e.Id} · {e.ApplicationDate:dd/MM/yyyy} · vật tư cũ #{e.FertilizerId} · {e.Dosage} · {e.Notes}"));
        s.LegacyNotes.AddRange(oldSprays.Select(e => $"Thuốc #{e.Id} · {e.ApplicationDate:dd/MM/yyyy} · thuốc cũ #{e.PesticideId} · {e.Dosage} · cách ly {e.PHI_Days} ngày · {e.Notes}"));
        if (area == null || type == null || seed == null) s.Warnings.Add("Hồ sơ vùng, loại cây hoặc giống còn thiếu tham chiếu.");
        if(seed?.InputLotId==null || !inputs.Any(l=>l.Id==seed.InputLotId))s.Warnings.Add("Giống chưa liên kết được lô vật tư đầu vào; cần đối chiếu hồ sơ nguồn giống.");
        if (journal.Any(e => !CropSeasonRules.Complete(e, p))) s.Warnings.Add("Có nhật ký chưa đủ thông tin.");
        if (journal.Any(e => e.Kind is "Pesticide" or "Fertilize" && (!e.InputLotId.HasValue || !inputs.Any(l => l.Id == e.InputLotId)))) s.Warnings.Add("Có lần dùng vật tư chưa gắn được lô nhập.");
        if (journal.Any(e => e.Kind == "Water" && (!e.WaterResourceId.HasValue || !resources.Any(r => r.Id == e.WaterResourceId)))) s.Warnings.Add("Có lần tưới chưa xác định nguồn nước tương ứng.");
        if (!reports.Any()) s.Warnings.Add("Chưa có phiếu kiểm nghiệm liên kết; không suy ra là đã kiểm nghiệm đạt.");
        if (reports.Any(r => r.Conclusion != "Pass")) s.Warnings.Add("Có phiếu không đạt hoặc chưa đủ kết luận; cần đọc cùng kết quả khắc phục.");
        if (s.LegacyNotes.Any()) s.Warnings.Add("Có dữ liệu sổ cũ, chưa liên kết đầy đủ lô vật tư mới.");
        var safety = await new CropSeasonService(db).SafetyAsync(plantId, date);
        if (safety.Incomplete || safety.SafeDate > date || p.ExpectedSafeHarvestDate > date) s.Warnings.Add("Mốc cách ly thiếu hoặc mâu thuẫn với ngày thu hoạch.");
        return s;
    }
    public async Task<HarvestTrace?> EnsureAsync(CropJournalEntry e)
    {
        var trace = await db.HarvestTraces.FirstOrDefaultAsync(t => t.JournalEntryId == e.Id);
        if (e.Kind != "Harvest" || e.IsDeleted == true) { if (trace != null) trace.IsVoided = true; return trace; }
        if (trace != null) { trace.IsVoided = !await FarmRecordRules.TestsAllowAsync(db, "HarvestTrace", trace.Id); return trace; }
        var snapshot = await BuildAsync(e.PlantId ?? 0, e.Date, e.Amount, e.Quality); if (snapshot == null) return null;
        trace = new() { JournalEntryId = e.Id, PlantId = e.PlantId, IsVoided = false }; await FillAsync(trace, snapshot); db.HarvestTraces.Add(trace); return trace;
    }
    public async Task FillAsync(HarvestTrace trace, TraceSnapshot snapshot)
    {
        trace.Revision = (trace.Revision ?? 0) + 1;
        if (trace.Code == null)
        { do { trace.Code = "TH-" + snapshot.HarvestDate.ToString("yyyyMMdd") + "-" + Guid.NewGuid().ToString("N")[..12].ToUpperInvariant(); } while (await db.HarvestTraces.IgnoreQueryFilters().AnyAsync(t => t.Code == trace.Code)); }
        trace.HarvestDate = snapshot.HarvestDate; trace.WeightKg = snapshot.WeightKg; trace.Quality = snapshot.Quality; trace.SnapshotJson = JsonSerializer.Serialize(snapshot); trace.SourceHash = Hash(trace.SnapshotJson);
    }
}
