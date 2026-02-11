using System.ComponentModel.DataAnnotations;

namespace Farm_Management.Models
{
    public class WaterSource
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên nguồn nước là bắt buộc")]
        [StringLength(100)]
        [Display(Name = "Tên nguồn nước")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Loại nguồn")]
        [StringLength(50)]
        public string? SourceType { get; set; } // Giếng khoan, sông, hồ...

        [Display(Name = "Kết quả xét nghiệm")]
        public string? TestResult { get; set; }

        [Display(Name = "Ngày xét nghiệm")]
        [DataType(DataType.Date)]
        public DateTime? TestDate { get; set; }

        [Display(Name = "Đạt chuẩn")]
        public bool IsQualified { get; set; } = true;

        [Display(Name = "Ghi chú")]
        public string? Notes { get; set; }

        [Display(Name = "Ngày tạo")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation
        public ICollection<WaterIrrigationLog> WaterIrrigationLogs { get; set; } = new List<WaterIrrigationLog>();
    }
}