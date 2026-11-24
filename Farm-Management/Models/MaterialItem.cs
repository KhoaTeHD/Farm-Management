using System.ComponentModel.DataAnnotations;

namespace Farm_Management.Models;

/// <summary>
/// Danh mục vật liệu sử dụng hằng ngày (phân bón, thuốc BVTV, vật liệu khác).
/// </summary>
public class MaterialItem : ICultivationRecord
{
    public int Id { get; set; }
    public int? Revision { get; set; }
    public bool? IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    [StringLength(200)]
    public string? Name { get; set; }

    [StringLength(30)]
    public string? Category { get; set; }

    [StringLength(20)]
    public string? DefaultUnit { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }

    public bool? IsActive { get; set; }
    public DateTime? CreatedAt { get; set; }
}
