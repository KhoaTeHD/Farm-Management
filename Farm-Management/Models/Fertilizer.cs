using System.ComponentModel.DataAnnotations;

namespace Farm_Management.Models
{
    public class Fertilizer
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên phân bón là bắt buộc")]
        [StringLength(200)]
        [Display(Name = "Tên phân bón")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Loại phân")]
        [StringLength(50)]
        public string? Type { get; set; } // Hữu cơ, vô cơ, vi sinh...

        [Display(Name = "Tỷ lệ NPK")]
        [StringLength(50)]
        public string? NPKRatio { get; set; }

        [Required(ErrorMessage = "Nhà cung cấp là bắt buộc")]
        [StringLength(200)]
        [Display(Name = "Nhà cung cấp")]
        public string Supplier { get; set; } = string.Empty;

        [Display(Name = "Số lượng tồn kho")]
        public decimal StockQuantity { get; set; }

        [StringLength(20)]
        [Display(Name = "Đơn vị")]
        public string? Unit { get; set; }

        [Display(Name = "Ngày hết hạn")]
        [DataType(DataType.Date)]
        public DateTime? ExpiryDate { get; set; }

        [Display(Name = "Số hóa đơn")]
        [StringLength(50)]
        public string? InvoiceNumber { get; set; }

        [Display(Name = "Ngày tạo")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation
        public ICollection<FertilizationLog> FertilizationLogs { get; set; } = new List<FertilizationLog>();
    }
}