using System.ComponentModel.DataAnnotations;

namespace Farm_Management.Models
{
    public class Pesticide
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Thuốc đăng ký")]
        public int PesticideRegistryId { get; set; }

        [Required(ErrorMessage = "Số lô là bắt buộc")]
        [StringLength(50)]
        [Display(Name = "Số lô")]
        public string BatchNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nhà cung cấp là bắt buộc")]
        [StringLength(200)]
        [Display(Name = "Nhà cung cấp")]
        public string Supplier { get; set; } = string.Empty;

        [Display(Name = "Số lượng tồn kho")]
        public decimal StockQuantity { get; set; }

        [StringLength(20)]
        [Display(Name = "Đơn vị")]
        public string? Unit { get; set; } // lít, kg, gói...

        [Required(ErrorMessage = "Ngày hết hạn là bắt buộc")]
        [Display(Name = "Ngày hết hạn")]
        [DataType(DataType.Date)]
        public DateTime ExpiryDate { get; set; }

        [Display(Name = "Ngày nhập kho")]
        [DataType(DataType.Date)]
        public DateTime PurchaseDate { get; set; } = DateTime.Now;

        [Display(Name = "Số hóa đơn")]
        [StringLength(50)]
        public string? InvoiceNumber { get; set; }

        [Display(Name = "Ngày tạo")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation
        public PesticideRegistry PesticideRegistry { get; set; } = null!;
        public ICollection<PesticideApplicationLog> PesticideApplicationLogs { get; set; } = new List<PesticideApplicationLog>();
    }
}