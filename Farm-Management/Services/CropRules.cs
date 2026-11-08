using Farm_Management.Models;

namespace Farm_Management.Services;

public static class CropRules
{
    public static IReadOnlyDictionary<string, string> Statuses { get; } = new Dictionary<string, string>
    {
        ["Growing"] = "Đang trồng",
        ["ReadyToHarvest"] = "Sẵn sàng thu hoạch",
        ["Harvested"] = "Đã thu hoạch",
        ["Destroyed"] = "Đã hủy"
    };
    public static string Label(string? status) => status != null && Statuses.TryGetValue(status, out var text) ? text : "Chưa rõ trạng thái";
    public static DateTime? Expected(DateTime? planting, int? cycle)
    {
        if (!planting.HasValue || cycle is not (>= 1 and <= 36500)) return null;
        try { var date = planting.Value.Date.AddDays(cycle.Value); return date.Year <= 9998 ? date : null; }
        catch (ArgumentOutOfRangeException) { return null; }
    }
    public static (DateTime? SafeDate, bool Incomplete) Safety(IEnumerable<PesticideApplicationLog> logs)
    {
        DateTime? latest = null; var incomplete = false;
        foreach (var log in logs)
        {
            if (!log.ApplicationDate.HasValue || log.ApplicationDate.Value.Year < 2 || log.PHI_Days is not (>= 0 and <= 36500)) { incomplete = true; continue; }
            try
            {
                var day = log.ApplicationDate.Value.Date.AddDays(log.PHI_Days.Value);
                if (log.SafeHarvestDate.HasValue && log.SafeHarvestDate.Value.Date < day) incomplete = true;
                if (log.SafeHarvestDate.HasValue && log.SafeHarvestDate > day) day = log.SafeHarvestDate.Value.Date;
                if (!latest.HasValue || day > latest) latest = day;
            }
            catch (ArgumentOutOfRangeException) { incomplete = true; }
        }
        return (latest, incomplete);
    }
}
