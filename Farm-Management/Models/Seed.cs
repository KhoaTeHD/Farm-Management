using System.ComponentModel.DataAnnotations;

namespace Farm_Management.Models
{
    public class Seed
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Loại cây")]
        public int PlantTypeId { get; set; }

        [Required(ErrorMessage = "Tên giống là bắt buộc")]
        [StringLength(100)]
        [Display(Name = "Tên giống")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nguồn gốc là bắt buộc")]
        [StringLength(200)]
        [Display(Name = "Nguồn gốc / Nhà cung cấp")]
        public string Origin { get; set; } = string.Empty;

        [StringLength(50)]
        [Display(Name = "Số lô giống")]
        public string? BatchNumber { get; set; }

        [StringLength(50)]
        [Display(Name = "Số hóa đơn")]
        public string? InvoiceNumber { get; set; }

        [Display(Name = "Có chứng nhận")]
        public bool IsCertified { get; set; }

        [Display(Name = "Ghi chú")]
        public string? Notes { get; set; }

        [Display(Name = "Ngày tạo")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation
        public PlantType PlantType { get; set; } = null!;
        public ICollection<Plant> Plants { get; set; } = new List<Plant>();
    }
}