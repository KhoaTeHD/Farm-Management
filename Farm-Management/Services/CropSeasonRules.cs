using Farm_Management.Models;

namespace Farm_Management.Services;

public static class CropSeasonRules
{
    public static IReadOnlyDictionary<string, string> Kinds { get; } = new Dictionary<string, string>
    {
        ["Observation"] = "Thăm vườn / đo cây",
        ["Water"] = "Tưới nước",
        ["Fertilize"] = "Bón phân",
        ["Pesticide"] = "Dùng thuốc",
        ["Care"] = "Tỉa cây / làm cỏ / chăm sóc khác",
        ["Harvest"] = "Thu hoạch"
    };

    public static IReadOnlyDictionary<string, string> Stages { get; } = new Dictionary<string, string>
    {
        ["Seedling"] = "Cây con / mới mọc",
        ["Vegetative"] = "Phát triển thân, lá",
        ["Flowering"] = "Ra hoa",
        ["Fruiting"] = "Nuôi quả / tạo củ",
        ["Mature"] = "Trưởng thành / gần thu hoạch"
    };

    public static IReadOnlyDictionary<string, string> Conditions { get; } = new Dictionary<string, string>
    {
        ["Good"] = "Phát triển tốt",
        ["Watch"] = "Cần theo dõi",
        ["Poor"] = "Phát triển kém / có vấn đề"
    };

    public static string KindLabel(string? kind) =>
        kind != null && Kinds.TryGetValue(kind, out var label) ? label : "Chưa rõ công việc";

    public static bool ValidUnit(string? kind, string? unit)
    {
        if (string.IsNullOrWhiteSpace(unit)) return true;
        return unit.Trim().Length <= 20;
    }

    public static DateTime? PlanDate(DateTime? start, int? day)
    {
        if (!start.HasValue || start.Value.Year < 2 || day is not (>= 0 and <= 36500)) return null;
        try { var result = start.Value.Date.AddDays(day.Value); return result.Year <= 9998 ? result : null; }
        catch (ArgumentOutOfRangeException) { return null; }
    }

    public static bool PhotoIsLocal(string? path) =>
        path != null && System.Text.RegularExpressions.Regex.IsMatch(path, "^/uploads/crops/[a-f0-9]{32}\\.(jpg|png|webp)$");

    public static PesticideApplicationLog AsSpray(CropJournalEntry entry) =>
        new() { ApplicationDate = entry.Date, PHI_Days = entry.WithdrawalDays };

    public static bool Complete(CropJournalEntry e, Plant p, bool hasAssignedWorkers = false)
    {
        if (!p.PlantingDate.HasValue || !e.Date.HasValue || e.Date.Value.Year < 2 || e.Date.Value.Date < p.PlantingDate.Value.Date || e.Date.Value.Date > DateTime.Today
            || !Kinds.ContainsKey(e.Kind ?? "") || (!hasAssignedWorkers && string.IsNullOrWhiteSpace(e.Recorder)))
            return false;

        if (e.Kind is "Fertilize" or "Pesticide" && !e.MaterialItemId.HasValue && string.IsNullOrWhiteSpace(e.Material))
            return false;

        if (e.Amount.HasValue && e.Amount is not (>= .01m and <= 1000000000m)) return false;
        if (!ValidUnit(e.Kind, e.Unit)) return false;
        if (e.WithdrawalDays.HasValue && e.WithdrawalDays is not (>= 0 and <= 36500)) return false;
        if (!string.IsNullOrWhiteSpace(e.GrowthStage) && !Stages.ContainsKey(e.GrowthStage)) return false;
        if (!string.IsNullOrWhiteSpace(e.Condition) && !Conditions.ContainsKey(e.Condition)) return false;
        if (e.HeightCm is < 0 or > 100000) return false;
        if (e.SurvivalPercent is < 0 or > 100) return false;
        if (e.AffectedPercent is < 0 or > 100) return false;

        return true;
    }
}
