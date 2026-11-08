using System.ComponentModel.DataAnnotations;
using Farm_Management.Services;

namespace Farm_Management.Models.ViewModels;

public class CropJournalForm : IValidatableObject
{
    public int Id { get; set; }
    [Required, Range(1, int.MaxValue)] public int? PlantId { get; set; }
    public int? CareStepId { get; set; }
    [Display(Name="Lô vật tư đã dùng")] public int? InputLotId {get;set;}
    [Display(Name="Nguồn nước đã dùng")] public int? WaterResourceId {get;set;}
    [Required, RegularExpression("^[a-f0-9]{32}$")] public string? RecordCode { get; set; }
    [Required, DataType(DataType.Date), Display(Name = "Ngày thực hiện")] public DateTime? Date { get; set; }
    [Required, Display(Name = "Việc đã làm")] public string? Kind { get; set; }
    [Required, StringLength(100), Display(Name = "Người ghi / người thực hiện")] public string? Recorder { get; set; }
    [Required, StringLength(2000), Display(Name = "Cách làm và điều nhìn thấy")] public string? Summary { get; set; }
    [StringLength(200), Display(Name = "Tên phân / thuốc đã dùng")] public string? Material { get; set; }
    [Range(typeof(decimal), "0.01", "1000000000"), Display(Name = "Lượng dùng / sản lượng")] public decimal? Amount { get; set; }
    [Display(Name = "Đơn vị")] public string? Unit { get; set; }
    [StringLength(200), Display(Name = "Cách thực hiện")] public string? Method { get; set; }
    [StringLength(200), Display(Name = "Thời tiết")] public string? Weather { get; set; }
    [Range(0, 36500), Display(Name = "Số ngày cách ly ghi trên nhãn")] public int? WithdrawalDays { get; set; }
    [Display(Name = "Giai đoạn của cây")] public string? GrowthStage { get; set; }
    [Display(Name = "Tình trạng cây")] public string? Condition { get; set; }
    [Range(typeof(decimal), "0", "100000"), Display(Name = "Chiều cao điển hình (cm)")] public decimal? HeightCm { get; set; }
    [Range(typeof(decimal), "0", "100"), Display(Name = "Tỷ lệ sống (%)")] public decimal? SurvivalPercent { get; set; }
    [Range(typeof(decimal), "0", "100"), Display(Name = "Tỷ lệ cây bị ảnh hưởng (%)")] public decimal? AffectedPercent { get; set; }
    [StringLength(50), Display(Name = "Chất lượng thu hoạch")] public string? Quality { get; set; }
    [Display(Name = "Ảnh cây / việc chăm sóc")] public IFormFile? Photo { get; set; }
    public bool RemovePhoto { get; set; }
    public string? ExistingPhoto { get; set; }
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (Date.HasValue && (Date.Value.Year < 2 || Date.Value.Date > DateTime.Today)) yield return new("Chọn ngày việc đã xảy ra, không chọn ngày tương lai.", new[] { nameof(Date) });
        if (!CropSeasonRules.Kinds.ContainsKey(Kind ?? "")) yield return new("Hãy chọn một việc trong danh sách.", new[] { nameof(Kind) });
        if (Kind is "Water" or "Fertilize" or "Pesticide" or "Harvest")
        {
            if (!Amount.HasValue) yield return new("Nhập lượng thực tế đã dùng hoặc thu được.", new[] { nameof(Amount) });
            if (!CropSeasonRules.ValidUnit(Kind, Unit)) yield return new("Đơn vị không phù hợp với việc đã chọn.", new[] { nameof(Unit) });
        }
        if (Kind is "Fertilize" or "Pesticide" && string.IsNullOrWhiteSpace(Material)) yield return new("Ghi rõ tên sản phẩm đã dùng.", new[] { nameof(Material) });
        if (Kind == "Pesticide" && !WithdrawalDays.HasValue) yield return new("Đọc nhãn và nhập số ngày cách ly; chưa biết thì chưa lưu phiếu sử dụng thuốc.", new[] { nameof(WithdrawalDays) });
        if (Kind == "Harvest" && string.IsNullOrWhiteSpace(Quality)) yield return new("Mô tả chất lượng, ví dụ: quả đều, khoảng 10% bị dập.", new[] { nameof(Quality) });
        if (Kind == "Observation" && (!CropSeasonRules.Stages.ContainsKey(GrowthStage ?? "") || !CropSeasonRules.Conditions.ContainsKey(Condition ?? ""))) yield return new("Chọn giai đoạn và tình trạng cây khi thăm vườn.", new[] { nameof(GrowthStage), nameof(Condition) });
        foreach (var field in new[] { (nameof(Amount), Amount), (nameof(HeightCm), HeightCm), (nameof(SurvivalPercent), SurvivalPercent), (nameof(AffectedPercent), AffectedPercent) })
            if (field.Item2.HasValue && decimal.Round(field.Item2.Value, 2) != field.Item2) yield return new("Chỉ nhập tối đa 2 chữ số sau dấu thập phân.", new[] { field.Item1 });
    }
}

public class CareStepForm
{
    public int Id { get; set; }
    [Required, Range(1, int.MaxValue)] public int? PlantId { get; set; }
    [Required, StringLength(150), Display(Name = "Tên việc dự định làm")] public string? Title { get; set; }
    [Required, Display(Name = "Nhóm công việc")] public string? Kind { get; set; }
    [Required, Range(0, 36500), Display(Name = "Làm vào ngày thứ mấy sau trồng?")] public int? PlannedDay { get; set; }
    [Required, StringLength(2000), Display(Name = "Cách làm dự kiến và điều cần kiểm tra")] public string? Instructions { get; set; }
}

public class SeasonEvaluationForm
{
    public int Id { get; set; }
    [StringLength(2000), Display(Name = "Quy trình / cách chăm sóc của vụ này")] public string? CareProtocol { get; set; }
    [Required, StringLength(3000), Display(Name = "Kết quả và kinh nghiệm của vụ này")] public string? SeasonReview { get; set; }
    [Required, StringLength(3000), Display(Name = "Mùa sau sẽ thay đổi gì?")] public string? NextSeasonChanges { get; set; }
}
