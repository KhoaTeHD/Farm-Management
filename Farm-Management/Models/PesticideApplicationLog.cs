using System.ComponentModel.DataAnnotations;

namespace Farm_Management.Models
{
    public class PesticideApplicationLog
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Lô cây")]
        public int PlantId { get; set; }

        [Required]
        [Display(Name = "Thuốc sử dụng")]
        public int PesticideId { get; set; }

        [Required]
        [Display(Name = "Người thực hiện")]
        public int WorkerId { get; set; }

        [Required(ErrorMessage = "Ngày phun là bắt buộc")]
        [Display(Name = "Ngày phun")]
        public DateTime ApplicationDate { get; set; }

        [Required(ErrorMessage = "Liều lượng là bắt buộc")]
        [StringLength(100)]
        [Display(Name = "Liều lượng")]
        public string Dosage { get; set; } = string.Empty;

        [Display(Name = "Phương pháp phun")]
        [StringLength(100)]
        public string? Method { get; set; }

        [Display(Name = "Lý do phun")]
        [StringLength(500)]
        public string? Reason { get; set; }

        [Display(Name = "Thời gian cách ly (ngày)")]
        public int PHI_Days { get; set; } // Copy từ PesticideRegistry tại thời điểm phun

        [Display(Name = "Ngày an toàn thu hoạch")]
        [DataType(DataType.Date)]
        public DateTime SafeHarvestDate { get; set; } // ApplicationDate + PHI_Days

        [Display(Name = "Ảnh minh chứng")]
        [StringLength(500)]
        public string? PhotoPath { get; set; }

        [Display(Name = "Ghi chú")]
        public string? Notes { get; set; }

        [Display(Name = "Ngày tạo")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation
        public Plant Plant { get; set; } = null!;
        public Pesticide Pesticide { get; set; } = null!;
        public Worker Worker { get; set; } = null!;
    }
}