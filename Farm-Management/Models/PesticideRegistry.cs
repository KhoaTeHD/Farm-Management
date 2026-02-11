using System.ComponentModel.DataAnnotations;

namespace Farm_Management.Models
{
    public class PesticideRegistry
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên hoạt chất là bắt buộc")]
        [StringLength(200)]
        [Display(Name = "Tên hoạt chất")]
        public string ActiveIngredient { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên thương mại là bắt buộc")]
        [StringLength(200)]
        [Display(Name = "Tên thương mại")]
        public string TradeName { get; set; } = string.Empty;

        [Display(Name = "Đối tượng cây trồng")]
        [StringLength(500)]
        public string? TargetCrops { get; set; }

        [Display(Name = "Đối tượng phòng trừ")]
        [StringLength(500)]
        public string? TargetPests { get; set; }

        [Display(Name = "Liều lượng khuyến cáo")]
        [StringLength(200)]
        public string? RecommendedDosage { get; set; }

        [Required(ErrorMessage = "Thời gian cách ly là bắt buộc")]
        [Display(Name = "Thời gian cách ly (ngày)")]
        public int PHI_Days { get; set; }

        [Required]
        [StringLength(20)]
        [Display(Name = "Trạng thái")]
        public string Status { get; set; } = "Approved"; // Approved, Banned, Restricted

        [Display(Name = "Ghi chú")]
        public string? Notes { get; set; }

        [Display(Name = "Ngày tạo")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation
        public ICollection<Pesticide> Pesticides { get; set; } = new List<Pesticide>();
    }
}