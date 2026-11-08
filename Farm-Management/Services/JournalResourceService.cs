using Farm_Management.Data;
using Farm_Management.Models;
using Microsoft.EntityFrameworkCore;

namespace Farm_Management.Services;

public class JournalResourceService(ApplicationDbContext db)
{
    public async Task<string?> ValidateAsync(CropJournalEntry entry, Plant plant, int excludeId = 0)
    {
        if (entry.Kind is "Fertilize" or "Pesticide")
        {
            var lot = await db.InputLots.FindAsync(entry.InputLotId ?? 0);
            if (lot == null || !FarmRecordRules.InputComplete(lot) || lot.Status != "Released" || lot.Kind != (entry.Kind == "Fertilize" ? "Fertilizer" : "Pesticide")) return "Chọn đúng lô phân/thuốc đủ nguồn gốc, ngày/hạn, lượng nhận và đã cho sử dụng trong mục Vật tư & kiểm nghiệm.";
            if (!lot.ReceivedDate.HasValue || entry.Date < lot.ReceivedDate || (lot.ExpiryDate.HasValue && entry.Date > lot.ExpiryDate)) return "Ngày dùng nằm trước ngày nhận hoặc sau hạn dùng của lô vật tư.";
            if (!await FarmRecordRules.TestsAllowAsync(db, "InputLot", lot.Id)) return "Lô vật tư còn chỉ tiêu kiểm nghiệm chưa đạt/chưa đủ kết luận.";
            var usages = await db.CropJournalEntries.Where(e => e.InputLotId == lot.Id && e.Id != excludeId).ToListAsync();
            var quantities = usages.Select(e => FarmRecordRules.ConvertAmount(e.Amount, e.Unit, lot.Unit)).ToList(); var current = FarmRecordRules.ConvertAmount(entry.Amount, entry.Unit, lot.Unit);
            if (!current.HasValue || !lot.Quantity.HasValue || quantities.Any(q => q == null) || quantities.Sum() + current > lot.Quantity) return "Lượng dùng vượt lượng còn lại đã ghi, thiếu dữ liệu hoặc đơn vị không đổi được về đơn vị nhận.";
            entry.Material = lot.ProductName;
        }
        if (entry.Kind == "Water")
        {
            var source = await db.FieldResources.FindAsync(entry.WaterResourceId ?? 0);
            if (source == null || source.Kind != "Water" || source.AreaId != plant.AreaId || source.Status != "Released" || string.IsNullOrWhiteSpace(source.Name) || string.IsNullOrWhiteSpace(source.Location) || string.IsNullOrWhiteSpace(source.RiskAssessment)) return "Chọn nguồn nước đủ tên, vị trí, kiểm tra nguy cơ, đúng vùng trồng và đã cho sử dụng.";
            if (!await FarmRecordRules.TestsAllowAsync(db, "FieldResource", source.Id)) return "Nguồn nước còn chỉ tiêu chưa đạt/chưa có kết luận, cần kiểm tra trước khi dùng.";
        }
        return null;
    }
}
