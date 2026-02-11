using System.ComponentModel.DataAnnotations;

namespace Farm_Management.Models
{
    public class Worker
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Mã công nhân là bắt buộc")]
        [StringLength(20)]
        [Display(Name = "Mã công nhân")]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "Họ tên là bắt buộc")]
        [StringLength(100)]
        [Display(Name = "Họ tên")]
        public string FullName { get; set; } = string.Empty;

        [StringLength(15)]
        [Display(Name = "Số điện thoại")]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "Lương ngày là bắt buộc")]
        [Display(Name = "Lương ngày (VNĐ)")]
        [Range(1, 10000000, ErrorMessage = "Lương từ 1 đến 10 triệu")]
        public decimal DailySalary { get; set; }

        [Display(Name = "Ngày vào làm")]
        [DataType(DataType.Date)]
        public DateTime? HireDate { get; set; }

        [Display(Name = "Đang làm việc")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Ngày tạo")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation - VietGAP Logs
        public ICollection<PesticideApplicationLog> PesticideApplicationLogs { get; set; } = new List<PesticideApplicationLog>();
        public ICollection<FertilizationLog> FertilizationLogs { get; set; } = new List<FertilizationLog>();
        public ICollection<WaterIrrigationLog> WaterIrrigationLogs { get; set; } = new List<WaterIrrigationLog>();
        public ICollection<HarvestBatch> HarvestBatches { get; set; } = new List<HarvestBatch>();

        // Navigation - Payroll
        public ICollection<AttendanceLog> AttendanceLogs { get; set; } = new List<AttendanceLog>();
    }
}