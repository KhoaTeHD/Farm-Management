using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace Farm_Management.Models
{
    public class HarvestBatch
    {
        public int Id { get; set; }

        [Display(Name = "Lô cây")]
        public int? PlantId { get; set; }

        [Display(Name = "Người thu hoạch")]
        public int? WorkerId { get; set; }

        [StringLength(50)]
        [Display(Name = "Mã truy xuất")]
        public string? TraceabilityCode { get; set; } // Auto: FARM-TYPE-YYYYMMDD-XXXX

        [Display(Name = "Ngày thu hoạch")]
        [DataType(DataType.Date)]
        public DateTime? HarvestDate { get; set; }

        [Display(Name = "Sản lượng (kg)")]
        public decimal? YieldKg { get; set; }

        [Display(Name = "Chất lượng")]
        [StringLength(50)]
        public string? QualityGrade { get; set; } // A, B, C

        [Display(Name = "PHI hợp lệ")]
        public bool? PHI_Compliant { get; set; }

        [Display(Name = "Ghi chú")]
        public string? Notes { get; set; }

        [Display(Name = "Ngày tạo")]
        public DateTime? CreatedAt { get; set; } = DateTime.Now;

        // Navigation
        [NotMapped, ValidateNever]
        public Plant? Plant { get; set; }
        [NotMapped, ValidateNever]
        public Worker? Worker { get; set; }
    }
}