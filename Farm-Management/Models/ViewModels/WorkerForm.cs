using System.ComponentModel.DataAnnotations;

namespace Farm_Management.Models.ViewModels
{
    public class WorkerForm
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Mã công nhân là bắt buộc")]
        [StringLength(20)]
        [Display(Name = "Mã công nhân")]
        public string? Code { get; set; }

        [Required(ErrorMessage = "Họ tên là bắt buộc")]
        [StringLength(100)]
        [Display(Name = "Họ tên")]
        public string? FullName { get; set; }

        [StringLength(15)]
        [Display(Name = "Số điện thoại")]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "Lương ngày là bắt buộc")]
        [Display(Name = "Lương ngày (VNĐ)")]
        [Range(1, 10000000, ErrorMessage = "Lương từ 1 đến 10 triệu")]
        public decimal? DailySalary { get; set; }

        [Display(Name = "Ngày vào làm")]
        [DataType(DataType.Date)]
        public DateTime? HireDate { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn trạng thái làm việc")]
        [Display(Name = "Đang làm việc")]
        public bool? IsActive { get; set; } = true;

        [Display(Name = "Ngày tạo")]
        public DateTime? CreatedAt { get; set; } = DateTime.Now;


        public Worker ToEntity() => new()
        {
            Id = Id,
            Code = Code,
            FullName = FullName,
            Phone = Phone,
            DailySalary = DailySalary,
            HireDate = HireDate,
            IsActive = IsActive,
            CreatedAt = CreatedAt,
        };

        public static WorkerForm FromEntity(Worker entity) => new()
        {
            Id = entity.Id,
            Code = entity.Code,
            FullName = entity.FullName,
            Phone = entity.Phone,
            DailySalary = entity.DailySalary,
            HireDate = entity.HireDate,
            IsActive = entity.IsActive,
            CreatedAt = entity.CreatedAt,
        };
    }
}
