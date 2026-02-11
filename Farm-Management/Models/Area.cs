using System.ComponentModel.DataAnnotations;

namespace Farm_Management.Models
{
    public class Area
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên vùng trồng là bắt buộc")]
        [StringLength(100)]
        [Display(Name = "Tên vùng trồng")]
        public string Name { get; set; } = string.Empty;

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
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation
        public ICollection<Plant> Plants { get; set; } = new List<Plant>();
    }
}