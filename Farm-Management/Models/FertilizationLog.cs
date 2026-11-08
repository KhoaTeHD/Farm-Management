using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace Farm_Management.Models
{
    public class FertilizationLog
    {
        public int Id { get; set; }

        [Display(Name = "Lô cây")]
        public int? PlantId { get; set; }

        [Display(Name = "Phân bón")]
        public int? FertilizerId { get; set; }

        [Display(Name = "Người thực hiện")]
        public int? WorkerId { get; set; }

        [Display(Name = "Ngày bón")]
        public DateTime? ApplicationDate { get; set; }

        [StringLength(100)]
        [Display(Name = "Liều lượng")]
        public string? Dosage { get; set; }

        [Display(Name = "Phương pháp bón")]
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
        public Fertilizer? Fertilizer { get; set; }
        [NotMapped, ValidateNever]
        public Worker? Worker { get; set; }
    }
}