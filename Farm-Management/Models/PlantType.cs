using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace Farm_Management.Models
{
    public class PlantType : ICultivationRecord
    {
        public int Id { get; set; }
        public int? Revision { get; set; }
        public bool? IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }

        [StringLength(100)]
        [Display(Name = "Tên loại cây")]
        public string? Name { get; set; }

        [Display(Name = "Chu kỳ sinh trưởng (ngày)")]
        public int? GrowthCycleDays { get; set; }

        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        [Display(Name = "Ngày tạo")]
        public DateTime? CreatedAt { get; set; } = DateTime.Now;

        // Navigation
        [NotMapped, ValidateNever]
        public ICollection<Seed> Seeds { get; set; } = new List<Seed>();
        [NotMapped, ValidateNever]
        public ICollection<Plant> Plants { get; set; } = new List<Plant>();
    }
}