using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace Farm_Management.Models
{
    public class Seed : ICultivationRecord
    {
        public int Id { get; set; }
        public int? Revision { get; set; }
        public bool? IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }

        [Display(Name = "Loại cây")]
        public int? PlantTypeId { get; set; }
        public int? InputLotId { get; set; }

        [StringLength(100)]
        [Display(Name = "Tên giống")]
        public string? Name { get; set; }

        [StringLength(200)]
        [Display(Name = "Nguồn gốc / Nhà cung cấp")]
        public string? Origin { get; set; }

        [StringLength(50)]
        [Display(Name = "Số lô giống")]
        public string? BatchNumber { get; set; }

        [StringLength(50)]
        [Display(Name = "Số hóa đơn")]
        public string? InvoiceNumber { get; set; }

        [Display(Name = "Có chứng nhận")]
        public bool? IsCertified { get; set; }

        [Display(Name = "Ghi chú")]
        public string? Notes { get; set; }

        [Display(Name = "Ngày tạo")]
        public DateTime? CreatedAt { get; set; } = DateTime.Now;

        // Navigation
        [NotMapped, ValidateNever]
        public PlantType? PlantType { get; set; }
        [NotMapped, ValidateNever]
        public ICollection<Plant> Plants { get; set; } = new List<Plant>();
    }
}