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
    { ["Good"] = "Phát triển tốt", ["Watch"] = "Cần theo dõi", ["Poor"] = "Phát triển kém / có vấn đề" };
    public static string KindLabel(string? kind) => kind != null && Kinds.TryGetValue(kind, out var label) ? label : "Chưa rõ công việc";
    public static bool ValidUnit(string? kind, string? unit) => kind switch
    { "Water" => unit is "l" or "m3", "Fertilize" or "Pesticide" => unit is "g" or "kg" or "ml" or "l", "Harvest" => unit == "kg", _ => true };
    public static DateTime? PlanDate(DateTime? start, int? day)
    {
        if (!start.HasValue || start.Value.Year < 2 || day is not (>= 0 and <= 36500)) return null;
        try { var result = start.Value.Date.AddDays(day.Value); return result.Year <= 9998 ? result : null; }
        catch (ArgumentOutOfRangeException) { return null; }
    }
    public static bool PhotoIsLocal(string? path) => path != null && System.Text.RegularExpressions.Regex.IsMatch(path, "^/uploads/crops/[a-f0-9]{32}\\.(jpg|png|webp)$");
    public static PesticideApplicationLog AsSpray(CropJournalEntry entry) => new() { ApplicationDate = entry.Date, PHI_Days = entry.WithdrawalDays };
    public static bool Complete(CropJournalEntry e, Plant p)
    {
        if (!p.PlantingDate.HasValue || !e.Date.HasValue || e.Date.Value.Year < 2 || e.Date.Value.Date < p.PlantingDate.Value.Date || e.Date.Value.Date > DateTime.Today
            || !Kinds.ContainsKey(e.Kind ?? "") || string.IsNullOrWhiteSpace(e.Summary) || string.IsNullOrWhiteSpace(e.Recorder)) return false;
        if (e.Kind is "Water" or "Fertilize" or "Pesticide" or "Harvest" && (e.Amount is not (>= .01m and <= 1000000000m) || !ValidUnit(e.Kind, e.Unit))) return false;
        if (e.Kind is "Fertilize" or "Pesticide" && string.IsNullOrWhiteSpace(e.Material)) return false;
        if (e.Kind == "Pesticide" && e.WithdrawalDays is not (>= 0 and <= 36500)) return false;
        if (e.Kind == "Harvest" && string.IsNullOrWhiteSpace(e.Quality)) return false;
        if (e.Kind == "Observation" && (!Stages.ContainsKey(e.GrowthStage ?? "") || !Conditions.ContainsKey(e.Condition ?? "") || e.HeightCm < 0 || e.HeightCm > 100000 || e.SurvivalPercent < 0 || e.SurvivalPercent > 100 || e.AffectedPercent < 0 || e.AffectedPercent > 100)) return false;
        return true;
    }
}
