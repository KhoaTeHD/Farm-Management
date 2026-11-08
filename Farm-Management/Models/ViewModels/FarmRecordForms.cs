using System.ComponentModel.DataAnnotations;

namespace Farm_Management.Models.ViewModels;

public class InputLotForm : IValidatableObject
{
    public int Id { get; set; }
    [Required, StringLength(50), Display(Name = "Mã lô vật tư")] public string? Code { get; set; }
    [Required, Display(Name = "Nhóm vật tư")] public string? Kind { get; set; }
    [Required, StringLength(200), Display(Name = "Tên sản phẩm trên bao bì")] public string? ProductName { get; set; }
    [Required, StringLength(100), Display(Name = "Số lô của nhà sản xuất")] public string? ManufacturerBatch { get; set; }
    [Required, StringLength(300), Display(Name = "Nhà cung cấp / nơi mua")] public string? Supplier { get; set; }
    [StringLength(100), Display(Name = "Số hóa đơn / chứng từ")] public string? InvoiceNumber { get; set; }
    [Required, DataType(DataType.Date), Display(Name = "Ngày nhận vật tư")] public DateTime? ReceivedDate { get; set; }
    [DataType(DataType.Date), Display(Name = "Hạn sử dụng")] public DateTime? ExpiryDate { get; set; }
    [Required, Range(typeof(decimal), "0.01", "1000000000"), Display(Name = "Số lượng nhận vào")] public decimal? Quantity { get; set; }
    [Required, Display(Name = "Đơn vị nhận")] public string? Unit { get; set; }
    [StringLength(100), Display(Name = "Số đăng ký / mã sản phẩm thuốc")] public string? RegistrationNumber { get; set; }
    [Required, Display(Name = "Trạng thái sau kiểm tra đầu vào")] public string? Status { get; set; } = "Hold";
    [StringLength(2000), Display(Name = "Ghi chú kiểm tra bao bì, nguồn gốc và xử lý")] public string? Notes { get; set; }
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if(Status=="Released" && !ExpiryDate.HasValue && (Kind=="Pesticide" || string.IsNullOrWhiteSpace(Notes)))yield return new("Thuốc phải có hạn dùng. Vật tư khác chưa có hạn: ghi rõ căn cứ không quy định hạn trong ghi chú; nếu chưa xác định, giữ Tạm giữ.",new[]{nameof(ExpiryDate),nameof(Notes)});
        if (!Services.FarmRecordRules.InputKinds.ContainsKey(Kind ?? "")) yield return new("Chọn nhóm vật tư hợp lệ.", new[] { nameof(Kind) });
        if (!Services.FarmRecordRules.Statuses.ContainsKey(Status ?? "")) yield return new("Chọn trạng thái hợp lệ.", new[] { nameof(Status) });
        if (!Services.FarmRecordRules.Units.Contains(Unit ?? "")) yield return new("Đơn vị không hợp lệ.", new[] { nameof(Unit) });
        if (ReceivedDate.HasValue && (ReceivedDate.Value.Year < 2 || ReceivedDate.Value.Date > DateTime.Today)) yield return new("Ngày nhận phải là ngày đã phát sinh.", new[] { nameof(ReceivedDate) });
        if (ExpiryDate.HasValue && (ExpiryDate.Value.Year < 2 || ExpiryDate.Value.Date < ReceivedDate?.Date)) yield return new("Hạn sử dụng không trước ngày nhận.", new[] { nameof(ExpiryDate) });
        if (Quantity.HasValue && decimal.Round(Quantity.Value, 2) != Quantity) yield return new("Số lượng tối đa 2 chữ số thập phân.", new[] { nameof(Quantity) });
        if (Kind == "Pesticide" && string.IsNullOrWhiteSpace(RegistrationNumber)) yield return new("Ghi số đăng ký sản phẩm để đối chiếu nhãn và danh mục được phép dùng.", new[] { nameof(RegistrationNumber) });
    }
}
public class ResourceForm
{
    public int Id { get; set; }
    [Required, Range(1, int.MaxValue), Display(Name = "Vùng trồng sử dụng")] public int? AreaId { get; set; }
    [Required, Display(Name = "Loại hồ sơ")] public string? Kind { get; set; }
    [Required, StringLength(200), Display(Name = "Tên nguồn nước / khu đất")] public string? Name { get; set; }
    [Required, StringLength(500), Display(Name = "Vị trí / mô tả nhận diện")] public string? Location { get; set; }
    [Required, Display(Name = "Trạng thái sử dụng")] public string? Status { get; set; } = "Hold";
    [Required, StringLength(2000), Display(Name = "Nguy cơ đã kiểm tra")] public string? RiskAssessment { get; set; }
    [StringLength(2000), Display(Name = "Biện pháp xử lý / khắc phục")] public string? Treatment { get; set; }
}
public class InspectionForm : IValidatableObject
{
    public int Id { get; set; }
    [Required, Display(Name = "Kiểm nghiệm cho")] public string? TargetType { get; set; }
    [Required, Range(1, int.MaxValue), Display(Name = "Hồ sơ được lấy mẫu")] public int? TargetId { get; set; }
    [Required, StringLength(100), Display(Name = "Số phiếu kết quả")] public string? ReportNumber { get; set; }
    [Required, StringLength(200), Display(Name = "Đơn vị kiểm nghiệm")] public string? Laboratory { get; set; }
    [Required, DataType(DataType.Date), Display(Name = "Ngày lấy mẫu")] public DateTime? SampleDate { get; set; }
    [Required, DataType(DataType.Date), Display(Name = "Ngày trả kết quả")] public DateTime? ReportDate { get; set; }
    [Required, StringLength(200), Display(Name = "Chỉ tiêu / nhóm chỉ tiêu")] public string? Indicator { get; set; }
    [Required, StringLength(2000), Display(Name = "Kết quả, giá trị và đơn vị theo phiếu")] public string? Result { get; set; }
    [Required, StringLength(1000), Display(Name = "Ngưỡng / tài liệu dùng đối chiếu")] public string? LimitReference { get; set; }
    [Required, Display(Name = "Kết luận theo phiếu / người phụ trách")] public string? Conclusion { get; set; } = "Pending";
    [Display(Name = "Ảnh hoặc PDF phiếu kiểm nghiệm")] public IFormFile? Evidence { get; set; }
    public string? ExistingEvidence { get; set; }
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (!Services.FarmRecordRules.TargetTypes.ContainsKey(TargetType ?? "")) yield return new("Loại đối tượng không hợp lệ.", new[] { nameof(TargetType) });
        if (!Services.FarmRecordRules.Conclusions.ContainsKey(Conclusion ?? "")) yield return new("Kết luận không hợp lệ.", new[] { nameof(Conclusion) });
        if (SampleDate.HasValue && (SampleDate.Value.Year < 2 || SampleDate.Value.Date > DateTime.Today)) yield return new("Ngày lấy mẫu phải là ngày đã phát sinh.", new[] { nameof(SampleDate) });
        if (ReportDate.HasValue && (ReportDate.Value.Year < 2 || ReportDate.Value.Date > DateTime.Today || ReportDate.Value.Date < SampleDate?.Date)) yield return new("Ngày kết quả không trước ngày lấy mẫu và không ở tương lai.", new[] { nameof(ReportDate) });
    }
}
