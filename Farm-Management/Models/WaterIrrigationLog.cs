using System.ComponentModel.DataAnnotations;

namespace Farm_Management.Models
{
    public class WaterIrrigationLog
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Lô cây")]
        public int PlantId { get; set; }

        [Required]
        [Display(Name = "Nguồn nước")]
        public int WaterSourceId { get; set; }

        [Required]
        [Display(Name = "Người thực hiện")]
        public int WorkerId { get; set; }

        [Required(ErrorMessage = "Ngày tưới là bắt buộc")]
        [Display(Name = "Ngày tưới")]
        public DateTime IrrigationDate { get; set; }

        [Display(Name = "Lượng nước (lít)")]
        public decimal? WaterAmount { get; set; }

        [Display(Name = "Phương pháp tưới")]
        [StringLength(100)]
        public string? Method { get; set; }

        [Display(Name = "Ghi chú")]
        public string? Notes { get; set; }

        [Display(Name = "Ngày tạo")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation
        public Plant Plant { get; set; } = null!;
        public WaterSource WaterSource { get; set; } = null!;
        public Worker Worker { get; set; } = null!;
    }
}