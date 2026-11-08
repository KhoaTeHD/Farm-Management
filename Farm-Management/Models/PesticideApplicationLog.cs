using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace Farm_Management.Models
{
    public class PesticideApplicationLog
    {
        public int Id { get; set; }

        [Display(Name = "Lô cây")]
        public int? PlantId { get; set; }

        [Display(Name = "Thuốc sử dụng")]
        public int? PesticideId { get; set; }

        [Display(Name = "Người thực hiện")]
        public int? WorkerId { get; set; }

        [Display(Name = "Ngày phun")]
        public DateTime? ApplicationDate { get; set; }

        [StringLength(100)]
        [Display(Name = "Liều lượng")]
        public string? Dosage { get; set; }

        [Display(Name = "Phương pháp phun")]
        [StringLength(100)]
        public string? Method { get; set; }

        [Display(Name = "Lý do phun")]
        [StringLength(500)]
        public string? Reason { get; set; }

        [Display(Name = "Thời gian cách ly (ngày)")]
        public int? PHI_Days { get; set; } // Copy từ PesticideRegistry tại thời điểm phun

        [Display(Name = "Ngày an toàn thu hoạch")]
        [DataType(DataType.Date)]
        public DateTime? SafeHarvestDate { get; set; } // ApplicationDate + PHI_Days

        [Display(Name = "Ảnh minh chứng")]
        [StringLength(500)]
        public string? PhotoPath { get; set; }

        [Display(Name = "Ghi chú")]
        public string? Notes { get; set; }

        [Display(Name = "Ngày tạo")]
        public DateTime? CreatedAt { get; set; } = DateTime.Now;

        // Navigation
        [NotMapped, ValidateNever]
        public Plant? Plant { get; set; }
        [NotMapped, ValidateNever]
        public Pesticide? Pesticide { get; set; }
        [NotMapped, ValidateNever]
        public Worker? Worker { get; set; }
    }
}