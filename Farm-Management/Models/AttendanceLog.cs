using System.ComponentModel.DataAnnotations;

namespace Farm_Management.Models
{
    public class AttendanceLog
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Công nhân là bắt buộc")]
        [Display(Name = "Công nhân")]
        public int WorkerId { get; set; }

        [Required(ErrorMessage = "Ngày chấm công là bắt buộc")]
        [Display(Name = "Ngày")]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; }

        [Required(ErrorMessage = "Số giờ làm là bắt buộc")]
        [Display(Name = "Số giờ làm")]
        [Range(0, 24, ErrorMessage = "Số giờ từ 0 đến 24")]
        public decimal HoursWorked { get; set; }

        [Display(Name = "Ghi chú")]
        [StringLength(500)]
        public string? Notes { get; set; }

        // Auto-calculated fields (computed on save)
        [Display(Name = "Phân loại")]
        [StringLength(20)]
        public string WorkType { get; set; } = string.Empty; // "FullDay", "HalfDay", "OddHours"

        [Display(Name = "Ngày tạo")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation
        public Worker Worker { get; set; } = null!;
    }
}