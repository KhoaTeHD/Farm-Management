using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace Farm_Management.Models
{
    public class Worker
    {
        public int Id { get; set; }

        [StringLength(20)]
        [Display(Name = "Mã công nhân")]
        public string? Code { get; set; }

        [StringLength(100)]
        [Display(Name = "Họ tên")]
        public string? FullName { get; set; }

        [StringLength(15)]
        [Display(Name = "Số điện thoại")]
        public string? Phone { get; set; }

        [Display(Name = "Lương ngày (VNĐ)")]
        [Range(1, 10000000, ErrorMessage = "Lương từ 1 đến 10 triệu")]
        public decimal? DailySalary { get; set; }

        [Display(Name = "Ngày vào làm")]
        [DataType(DataType.Date)]
        public DateTime? HireDate { get; set; }

        [Display(Name = "Đang làm việc")]
        public bool? IsActive { get; set; } = true;

        [Display(Name = "Ngày tạo")]
        public DateTime? CreatedAt { get; set; } = DateTime.Now;

        // Navigation - VietGAP Logs
        [NotMapped, ValidateNever]
        public ICollection<PesticideApplicationLog> PesticideApplicationLogs { get; set; } = new List<PesticideApplicationLog>();
        [NotMapped, ValidateNever]
        public ICollection<FertilizationLog> FertilizationLogs { get; set; } = new List<FertilizationLog>();
        [NotMapped, ValidateNever]
        public ICollection<WaterIrrigationLog> WaterIrrigationLogs { get; set; } = new List<WaterIrrigationLog>();
        [NotMapped, ValidateNever]
        public ICollection<HarvestBatch> HarvestBatches { get; set; } = new List<HarvestBatch>();

        // Navigation - Payroll
        [NotMapped, ValidateNever]
        public ICollection<AttendanceLog> AttendanceLogs { get; set; } = new List<AttendanceLog>();
    }
}