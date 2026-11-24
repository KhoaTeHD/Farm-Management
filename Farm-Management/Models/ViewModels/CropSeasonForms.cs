using System.ComponentModel.DataAnnotations;
using Farm_Management.Services;

namespace Farm_Management.Models.ViewModels;

public class CropJournalForm : IValidatableObject
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn mùa vụ / lô cây."), Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn mùa vụ / lô cây.")]
    [Display(Name = "Mùa vụ / lô cây")]
    public int? PlantId { get; set; }

    public int? CareStepId { get; set; }

    [Display(Name = "Vật liệu đã dùng")]
    public int? MaterialItemId { get; set; }

    [Display(Name = "Công nhân thực hiện")]
    public List<int> WorkerIds { get; set; } = new();

    public string? ReturnUrl { get; set; }
    public string? LegacyRecorder { get; set; }
    public string? LegacyMaterial { get; set; }

    [Display(Name = "Lô vật tư đã dùng")]
    public int? InputLotId { get; set; }

    [Display(Name = "Nguồn nước đã dùng")]
    public int? WaterResourceId { get; set; }

    [Required, RegularExpression("^[a-f0-9]{32}$")]
    public string? RecordCode { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn ngày thực hiện."), DataType(DataType.Date), Display(Name = "Ngày thực hiện")]
    public DateTime? Date { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn loại công việc."), Display(Name = "Loại công việc")]
    public string? Kind { get; set; }

    [StringLength(100), Display(Name = "Người ghi / người thực hiện")]
    public string? Recorder { get; set; }

    [StringLength(2000), Display(Name = "Ghi chú công việc")]
    public string? Summary { get; set; }

    [StringLength(200), Display(Name = "Tên vật liệu")]
    public string? Material { get; set; }

    [Range(typeof(decimal), "0.01", "1000000000", ErrorMessage = "Số lượng phải lớn hơn 0."), Display(Name = "Số lượng (tùy chọn)")]
    public decimal? Amount { get; set; }

    [StringLength(20), Display(Name = "Đơn vị (tùy chọn)")]
    public string? Unit { get; set; }

    [StringLength(200), Display(Name = "Cách thực hiện (tùy chọn)")]
    public string? Method { get; set; }

    [StringLength(200), Display(Name = "Thời tiết (tùy chọn)")]
    public string? Weather { get; set; }

    [Range(0, 36500, ErrorMessage = "Số ngày cách ly từ 0 đến 36.500."), Display(Name = "Số ngày cách ly (tùy chọn)")]
    public int? WithdrawalDays { get; set; }

    [Display(Name = "Giai đoạn của cây (tùy chọn)")]
    public string? GrowthStage { get; set; }

    [Display(Name = "Tình trạng cây (tùy chọn)")]
    public string? Condition { get; set; }

    [Range(typeof(decimal), "0", "100000", ErrorMessage = "Chiều cao không âm."), Display(Name = "Chiều cao điển hình (cm)")]
    public decimal? HeightCm { get; set; }

    [Range(typeof(decimal), "0", "100", ErrorMessage = "Tỷ lệ sống từ 0 đến 100%."), Display(Name = "Tỷ lệ sống (%)")]
    public decimal? SurvivalPercent { get; set; }

    [Range(typeof(decimal), "0", "100", ErrorMessage = "Tỷ lệ ảnh hưởng từ 0 đến 100%."), Display(Name = "Tỷ lệ cây bị ảnh hưởng (%)")]
    public decimal? AffectedPercent { get; set; }

    [StringLength(50), Display(Name = "Chất lượng thu hoạch (tùy chọn)")]
    public string? Quality { get; set; }

    [Display(Name = "Ảnh công việc (tùy chọn)")]
    public IFormFile? Photo { get; set; }

    public bool RemovePhoto { get; set; }
    public string? ExistingPhoto { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (Date.HasValue && (Date.Value.Year < 2 || Date.Value.Date > DateTime.Today))
            yield return new("Chọn ngày việc đã xảy ra, không chọn ngày tương lai.", new[] { nameof(Date) });

        if (!CropSeasonRules.Kinds.ContainsKey(Kind ?? ""))
            yield return new("Hãy chọn một việc trong danh sách.", new[] { nameof(Kind) });

        if (WorkerIds == null || WorkerIds.Count == 0)
            yield return new("Vui lòng chọn ít nhất 1 công nhân thực hiện.", new[] { nameof(WorkerIds) });

        if (!string.IsNullOrWhiteSpace(Unit) && !CropSeasonRules.ValidUnit(Kind, Unit))
            yield return new("Đơn vị tối đa 20 ký tự.", new[] { nameof(Unit) });

        if (!string.IsNullOrWhiteSpace(GrowthStage) && !CropSeasonRules.Stages.ContainsKey(GrowthStage))
            yield return new("Giai đoạn cây không hợp lệ.", new[] { nameof(GrowthStage) });

        if (!string.IsNullOrWhiteSpace(Condition) && !CropSeasonRules.Conditions.ContainsKey(Condition))
            yield return new("Tình trạng cây không hợp lệ.", new[] { nameof(Condition) });

        foreach (var field in new[] { (nameof(Amount), Amount), (nameof(HeightCm), HeightCm), (nameof(SurvivalPercent), SurvivalPercent), (nameof(AffectedPercent), AffectedPercent) })
            if (field.Item2.HasValue && decimal.Round(field.Item2.Value, 2) != field.Item2)
                yield return new("Chỉ nhập tối đa 2 chữ số sau dấu thập phân.", new[] { field.Item1 });
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
