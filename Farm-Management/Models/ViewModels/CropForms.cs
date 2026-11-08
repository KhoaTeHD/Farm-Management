using System.ComponentModel.DataAnnotations;

namespace Farm_Management.Models.ViewModels;

public class PlantTypeForm
{
    public int Id { get; set; }
    [Required, StringLength(100), Display(Name = "Tên loại cây")] public string? Name { get; set; }
    [Range(1, 36500), Display(Name = "Chu kỳ sinh trưởng (ngày)")] public int? GrowthCycleDays { get; set; }
    [Display(Name = "Mô tả")] public string? Description { get; set; }
}

public class SeedForm
{
    public int Id { get; set; }
    [Display(Name="Lô giống đầu vào (nếu có hồ sơ nhập)")] public int? InputLotId {get;set;}
    [Required, Range(1, int.MaxValue), Display(Name = "Loại cây")] public int? PlantTypeId { get; set; }
    [Required, StringLength(100), Display(Name = "Tên giống")] public string? Name { get; set; }
    [Required, StringLength(200), Display(Name = "Nguồn gốc / nhà cung cấp")] public string? Origin { get; set; }
    [StringLength(50), Display(Name = "Số lô giống")] public string? BatchNumber { get; set; }
    [StringLength(50), Display(Name = "Số hóa đơn")] public string? InvoiceNumber { get; set; }
    [Display(Name = "Có chứng nhận")] public bool? IsCertified { get; set; }
    [Display(Name = "Ghi chú")] public string? Notes { get; set; }
}

public class PlantForm : IValidatableObject
{
    public int Id { get; set; }
    [StringLength(100), Display(Name = "Tên mùa vụ")] public string? SeasonName { get; set; }
    [StringLength(2000), Display(Name = "Quy trình / cách chăm sóc dự kiến")] public string? CareProtocol { get; set; }
    [Display(Name = "Lấy kế hoạch từ mùa vụ trước (nếu muốn)")] public int? SourcePlantId { get; set; }
    [Required, StringLength(50), Display(Name = "Mã lô cây")] public string? BatchCode { get; set; }
    [Required, Range(1, int.MaxValue), Display(Name = "Vùng trồng")] public int? AreaId { get; set; }
    [Required, Range(1, int.MaxValue), Display(Name = "Loại cây")] public int? PlantTypeId { get; set; }
    [Required, Range(1, int.MaxValue), Display(Name = "Giống cây")] public int? SeedId { get; set; }
    [Range(1, int.MaxValue), Display(Name = "Số lượng cây")] public int? Quantity { get; set; }
    [Required, DataType(DataType.Date), Display(Name = "Ngày trồng")] public DateTime? PlantingDate { get; set; }
    [DataType(DataType.Date), Display(Name = "Dự kiến thu hoạch")] public DateTime? ExpectedHarvestDate { get; set; }
    [Required, Display(Name = "Trạng thái")] public string? Status { get; set; } = "Growing";
    [Display(Name = "Ghi chú")] public string? Notes { get; set; }
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (PlantingDate.HasValue && (PlantingDate.Value.Year < 2 || PlantingDate.Value.Date > DateTime.Today))
            yield return new("Ngày trồng phải là ngày đã phát sinh, không ở tương lai.", new[] { nameof(PlantingDate) });
        if (ExpectedHarvestDate.HasValue && (ExpectedHarvestDate.Value.Year > 9998 || ExpectedHarvestDate < PlantingDate))
            yield return new("Ngày dự kiến phải từ ngày trồng trở đi và không vượt năm 9998.", new[] { nameof(ExpectedHarvestDate) });
        if (!Services.CropRules.Statuses.ContainsKey(Status ?? "")) yield return new("Trạng thái không hợp lệ.", new[] { nameof(Status) });
    }
}
