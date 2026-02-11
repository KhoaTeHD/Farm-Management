using System.ComponentModel.DataAnnotations;

namespace Farm_Management.Models
{
    public class HarvestBatch
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Lô cây")]
        public int PlantId { get; set; }

        [Required]
        [Display(Name = "Người thu hoạch")]
        public int WorkerId { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Mã truy xuất")]
        public string TraceabilityCode { get; set; } = string.Empty; // Auto: FARM-TYPE-YYYYMMDD-XXXX

        [Required(ErrorMessage = "Ngày thu hoạch là bắt buộc")]
        [Display(Name = "Ngày thu hoạch")]
        [DataType(DataType.Date)]
        public DateTime HarvestDate { get; set; }

        [Display(Name = "Sản lượng (kg)")]
        public decimal? YieldKg { get; set; }

        [Display(Name = "Chất lượng")]
        [StringLength(50)]
        public string? QualityGrade { get; set; } // A, B, C

        [Display(Name = "PHI hợp lệ")]
        public bool PHI_Compliant { get; set; }

        [Display(Name = "Ghi chú")]
        public string? Notes { get; set; }

        [Display(Name = "Ngày tạo")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation
        public Plant Plant { get; set; } = null!;
        public Worker Worker { get; set; } = null!;
    }
}