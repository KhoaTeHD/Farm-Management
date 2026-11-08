using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace Farm_Management.Models
{
    public class Pesticide
    {
        public int Id { get; set; }

        [Display(Name = "Thuốc đăng ký")]
        public int? PesticideRegistryId { get; set; }

        [StringLength(50)]
        [Display(Name = "Số lô")]
        public string? BatchNumber { get; set; }

        [StringLength(200)]
        [Display(Name = "Nhà cung cấp")]
        public string? Supplier { get; set; }

        [Display(Name = "Số lượng tồn kho")]
        public decimal? StockQuantity { get; set; }

        [StringLength(20)]
        [Display(Name = "Đơn vị")]
        public string? Unit { get; set; } // lít, kg, gói...

        [Display(Name = "Ngày hết hạn")]
        [DataType(DataType.Date)]
        public DateTime? ExpiryDate { get; set; }

        [Display(Name = "Ngày nhập kho")]
        [DataType(DataType.Date)]
        public DateTime? PurchaseDate { get; set; } = DateTime.Now;

        [Display(Name = "Số hóa đơn")]
        [StringLength(50)]
        public string? InvoiceNumber { get; set; }

        [Display(Name = "Ngày tạo")]
        public DateTime? CreatedAt { get; set; } = DateTime.Now;

        // Navigation
        [NotMapped, ValidateNever]
        public PesticideRegistry? PesticideRegistry { get; set; }
        [NotMapped, ValidateNever]
        public ICollection<PesticideApplicationLog> PesticideApplicationLogs { get; set; } = new List<PesticideApplicationLog>();
    }
}