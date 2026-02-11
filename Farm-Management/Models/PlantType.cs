using System.ComponentModel.DataAnnotations;

namespace Farm_Management.Models
{
    public class PlantType
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên loại cây là bắt buộc")]
        [StringLength(100)]
        [Display(Name = "Tên loại cây")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Chu kỳ sinh trưởng (ngày)")]
        public int? GrowthCycleDays { get; set; }

        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        [Display(Name = "Ngày tạo")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation
        public ICollection<Seed> Seeds { get; set; } = new List<Seed>();
        public ICollection<Plant> Plants { get; set; } = new List<Plant>();
    }
}