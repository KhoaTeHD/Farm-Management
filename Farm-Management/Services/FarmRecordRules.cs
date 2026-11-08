using Farm_Management.Data;
using Farm_Management.Models;
using Microsoft.EntityFrameworkCore;

namespace Farm_Management.Services;

public static class FarmRecordRules
{
    public static bool InputComplete(InputLot lot)=>!string.IsNullOrWhiteSpace(lot.Code) && !string.IsNullOrWhiteSpace(lot.ProductName) && !string.IsNullOrWhiteSpace(lot.ManufacturerBatch) && !string.IsNullOrWhiteSpace(lot.Supplier)
        && lot.ReceivedDate.HasValue && lot.ReceivedDate.Value.Year>=2 && lot.Quantity>0 && Units.Contains(lot.Unit??"") && InputKinds.ContainsKey(lot.Kind??"")
        && (lot.ExpiryDate.HasValue ? lot.ExpiryDate>=lot.ReceivedDate : lot.Kind!="Pesticide" && !string.IsNullOrWhiteSpace(lot.Notes))
        && (lot.Kind!="Pesticide" || !string.IsNullOrWhiteSpace(lot.RegistrationNumber));
    public static string Time(DateTime? utc)=>utc.HasValue?new DateTimeOffset(DateTime.SpecifyKind(utc.Value,DateTimeKind.Utc)).ToOffset(TimeSpan.FromHours(7)).ToString("dd/MM/yyyy HH:mm:ss"):"Chưa rõ thời điểm";
    public static string RecordLabel(string? type)=>type switch {"Area"=>"Vùng trồng","PlantType"=>"Loại cây","Seed"=>"Giống cây","Plant"=>"Mùa vụ","CropCareStep"=>"Việc dự kiến","CropJournalEntry"=>"Nhật ký chăm sóc","InputLot"=>"Lô vật tư","FieldResource"=>"Đất / nguồn nước","InspectionReport"=>"Phiếu kiểm nghiệm","HarvestTrace"=>"Hồ sơ truy xuất",_=>"Hồ sơ chưa xác định"};
    public static IReadOnlyDictionary<string, string> InputKinds { get; } = new Dictionary<string, string> { { "Fertilizer", "Phân bón" }, { "Pesticide", "Thuốc bảo vệ thực vật" }, { "Seed", "Giống / hạt giống" }, { "Other", "Vật tư khác" } };
    public static IReadOnlyDictionary<string, string> ResourceKinds { get; } = new Dictionary<string, string> { { "Water", "Nguồn nước" }, { "Soil", "Đất / giá thể" } };
    public static IReadOnlyDictionary<string, string> Statuses { get; } = new Dictionary<string, string> { { "Hold", "Tạm giữ / chưa cho dùng" }, { "Released", "Đã kiểm tra, cho sử dụng" } };
    public static IReadOnlyDictionary<string, string> TargetTypes { get; } = new Dictionary<string, string> { { "InputLot", "Lô vật tư" }, { "FieldResource", "Đất / nguồn nước" }, { "HarvestTrace", "Lô thu hoạch" } };
    public static IReadOnlyDictionary<string, string> Conclusions { get; } = new Dictionary<string, string> { { "Pending", "Chưa đủ kết luận" }, { "Pass", "Đạt theo phiếu" }, { "Fail", "Không đạt" } };
    public static string[] Units { get; } = ["kg", "g", "l", "ml", "count"];
    public static decimal? ConvertAmount(decimal? amount, string? from, string? to)
    {
        if (!amount.HasValue || amount < 0) return null;
        if (from == to && Units.Contains(from ?? "")) return amount;
        return (from, to) switch { ("g", "kg") or ("ml", "l") => amount / 1000m, ("kg", "g") or ("l", "ml") => amount * 1000m, _ => null };
    }
    public static async Task<bool> TestsAllowAsync(ApplicationDbContext db, string type, int id)
    {
        var reports = await db.InspectionReports.AsNoTracking().Where(r => r.TargetType == type && r.TargetId == id).ToListAsync();
        return reports.GroupBy(r => r.Indicator?.Trim().ToUpperInvariant() ?? "").All(g =>
        {
            var r = g.OrderByDescending(r => r.SampleDate).ThenByDescending(r => r.ReportDate).ThenByDescending(r => r.Id).First();
            return r.Conclusion == "Pass" && !string.IsNullOrWhiteSpace(r.Indicator) && !string.IsNullOrWhiteSpace(r.Result) && !string.IsNullOrWhiteSpace(r.LimitReference)
                && !string.IsNullOrWhiteSpace(r.Laboratory) && !string.IsNullOrWhiteSpace(r.ReportNumber) && r.SampleDate.HasValue && r.SampleDate.Value.Year >= 2 && r.ReportDate.HasValue && r.ReportDate >= r.SampleDate && r.ReportDate <= DateTime.Today;
        });
    }
    public static async Task<bool> TargetExistsAsync(ApplicationDbContext db, string? type, int? id) => type switch
    { "InputLot" => await db.InputLots.AnyAsync(r => r.Id == id), "FieldResource" => await db.FieldResources.AnyAsync(r => r.Id == id), "HarvestTrace" => await db.HarvestTraces.AnyAsync(r => r.Id == id), _ => false };
    public static bool EvidenceLocal(string? path) => path != null && System.Text.RegularExpressions.Regex.IsMatch(path, "^/uploads/evidence/[a-f0-9]{32}\\.(pdf|jpg|png|webp)$");
}
