using System.ComponentModel.DataAnnotations;

namespace Farm_Management.Models.ViewModels
{
    public class AreaForm : IValidatableObject
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên vùng trồng là bắt buộc")]
        [StringLength(100)]
        [Display(Name = "Tên vùng trồng")]
        public string? Name { get; set; }

        [Range(typeof(decimal), "0.01", "9999999999999999.99", ErrorMessage = "Diện tích phải lớn hơn 0")]
        [Display(Name = "Diện tích (m²)")]
        public decimal? Acreage { get; set; }

        [StringLength(200)]
        [Display(Name = "Tọa độ GPS")]
        public string? GpsCoordinates { get; set; }

        [Display(Name = "Loại đất")]
        [StringLength(100)]
        public string? SoilType { get; set; }

        [Display(Name = "Kết quả xét nghiệm đất")]
        public string? SoilTestResult { get; set; }

        [Display(Name = "Ngày xét nghiệm đất")]
        [DataType(DataType.Date)]
        public DateTime? SoilTestDate { get; set; }

        [Display(Name = "Ghi chú")]
        public string? Notes { get; set; }

        [Display(Name = "Ngày tạo")]
        public DateTime? CreatedAt { get; set; } = DateTime.Now;


        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!string.IsNullOrWhiteSpace(GpsCoordinates))
            {
                var parts = GpsCoordinates.Split(',');
                if (parts.Length != 2
                    || !double.TryParse(parts[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var lat)
                    || !double.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var lng)
                    || !double.IsFinite(lat) || !double.IsFinite(lng) || lat < -90 || lat > 90 || lng < -180 || lng > 180)
                    yield return new ValidationResult("Tọa độ phải có dạng vĩ độ, kinh độ hợp lệ", new[] { nameof(GpsCoordinates) });
            }
        }

        public Area ToEntity() => new()
        {
            Id = Id,
            Name = Name,
            Acreage = Acreage,
            GpsCoordinates = GpsCoordinates,
            SoilType = SoilType,
            SoilTestResult = SoilTestResult,
            SoilTestDate = SoilTestDate,
            Notes = Notes,
            CreatedAt = CreatedAt,
        };

        public static AreaForm FromEntity(Area entity) => new()
        {
            Id = entity.Id,
            Name = entity.Name,
            Acreage = entity.Acreage,
            GpsCoordinates = entity.GpsCoordinates,
            SoilType = entity.SoilType,
            SoilTestResult = entity.SoilTestResult,
            SoilTestDate = entity.SoilTestDate,
            Notes = entity.Notes,
            CreatedAt = entity.CreatedAt,
        };
    }
}
