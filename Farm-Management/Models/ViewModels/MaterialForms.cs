using System.ComponentModel.DataAnnotations;
using Farm_Management.Services;

namespace Farm_Management.Models.ViewModels;

public class MaterialItemForm : IValidatableObject
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên vật liệu.")]
    [StringLength(200, ErrorMessage = "Tên vật liệu tối đa 200 ký tự.")]
    [Display(Name = "Tên vật liệu")]
    public string? Name { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn nhóm vật liệu.")]
    [Display(Name = "Nhóm vật liệu")]
    public string? Category { get; set; } = "Fertilizer";

    [StringLength(20, ErrorMessage = "Đơn vị mặc định tối đa 20 ký tự.")]
    [Display(Name = "Đơn vị mặc định (tùy chọn)")]
    public string? DefaultUnit { get; set; }

    [StringLength(1000, ErrorMessage = "Ghi chú tối đa 1.000 ký tự.")]
    [Display(Name = "Ghi chú")]
    public string? Notes { get; set; }

    [Display(Name = "Đang sử dụng")]
    public bool IsActive { get; set; } = true;

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (!MaterialRules.Categories.ContainsKey(Category ?? ""))
            yield return new("Chọn nhóm vật liệu hợp lệ.", new[] { nameof(Category) });
    }
}
