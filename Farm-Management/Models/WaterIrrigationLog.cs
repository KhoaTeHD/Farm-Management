using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace Farm_Management.Models
{
    public class WaterIrrigationLog
    {
        public int Id { get; set; }

        [Display(Name = "Lô cây")]
        public int? PlantId { get; set; }

        [Display(Name = "Nguồn nước")]
        public int? WaterSourceId { get; set; }

        [Display(Name = "Người thực hiện")]
        public int? WorkerId { get; set; }

        [Display(Name = "Ngày tưới")]
        public DateTime? IrrigationDate { get; set; }

        [Display(Name = "Lượng nước (lít)")]
        public decimal? WaterAmount { get; set; }

        [Display(Name = "Phương pháp tưới")]
        [StringLength(100)]
        public string? Method { get; set; }

        [Display(Name = "Ghi chú")]
        public string? Notes { get; set; }

        [Display(Name = "Ngày tạo")]
        public DateTime? CreatedAt { get; set; } = DateTime.Now;

        // Navigation
        [NotMapped, ValidateNever]
        public Plant? Plant { get; set; }
        [NotMapped, ValidateNever]
        public WaterSource? WaterSource { get; set; }
        [NotMapped, ValidateNever]
        public Worker? Worker { get; set; }
    }
}