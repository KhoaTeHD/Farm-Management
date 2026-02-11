using System.ComponentModel.DataAnnotations;

namespace Farm_Management.Models
{
    public class FertilizationLog
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Lô cây")]
        public int PlantId { get; set; }

        [Required]
        [Display(Name = "Phân bón")]
        public int FertilizerId { get; set; }

        [Required]
        [Display(Name = "Người thực hiện")]
        public int WorkerId { get; set; }

        [Required(ErrorMessage = "Ngày bón là bắt buộc")]
        [Display(Name = "Ngày bón")]
        public DateTime ApplicationDate { get; set; }

        [Required(ErrorMessage = "Liều lượng là bắt buộc")]
        [StringLength(100)]
        [Display(Name = "Liều lượng")]
        public string Dosage { get; set; } = string.Empty;

        [Display(Name = "Phương pháp bón")]
        [StringLength(100)]
        public string? Method { get; set; }

        [Display(Name = "Ghi chú")]
        public string? Notes { get; set; }

        [Display(Name = "Ngày tạo")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation
        public Plant Plant { get; set; } = null!;
        public Fertilizer Fertilizer { get; set; } = null!;
        public Worker Worker { get; set; } = null!;
    }
}