using System.ComponentModel.DataAnnotations;

namespace Farm_Management.Models.ViewModels
{
    public class AttendanceLogForm : IValidatableObject
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Công nhân là bắt buộc")]
        [Display(Name = "Công nhân")]
        public int? WorkerId { get; set; }

        [Required(ErrorMessage = "Ngày chấm công là bắt buộc")]
        [Display(Name = "Ngày")]
        [DataType(DataType.Date)]
        public DateTime? Date { get; set; }

        [Required(ErrorMessage = "Số giờ làm là bắt buộc")]
        [Display(Name = "Số giờ làm")]
        [Range(0, 24, ErrorMessage = "Số giờ từ 0 đến 24")]
        public decimal? HoursWorked { get; set; }

        [Display(Name = "Ghi chú")]
        [StringLength(500)]
        public string? Notes { get; set; }

        // Auto-calculated fields (computed on save)
        [Display(Name = "Phân loại")]
        [StringLength(20)]
        public string? WorkType { get; set; } // "FullDay", "HalfDay", "OddHours"

        [Display(Name = "Ngày tạo")]
        public DateTime? CreatedAt { get; set; } = DateTime.Now;


        [Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidateNever]
        public Worker? Worker { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (HoursWorked.HasValue && !AttendanceRules.IsValidHours(HoursWorked))
                yield return new ValidationResult("Số giờ từ 0 đến 24, theo bước 0.5 giờ", new[] { nameof(HoursWorked) });
        }

        public AttendanceLog ToEntity() => new()
        {
            Id = Id,
            WorkerId = WorkerId,
            Date = Date,
            HoursWorked = HoursWorked,
            Notes = Notes,
            WorkType = WorkType,
            CreatedAt = CreatedAt,
        };

        public static AttendanceLogForm FromEntity(AttendanceLog entity) => new()
        {
            Worker = entity.Worker,
            Id = entity.Id,
            WorkerId = entity.WorkerId,
            Date = entity.Date,
            HoursWorked = entity.HoursWorked,
            Notes = entity.Notes,
            WorkType = entity.WorkType,
            CreatedAt = entity.CreatedAt,
        };
    }
}
