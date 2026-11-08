using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace Farm_Management.Models
{
    public class Plant : ICultivationRecord
    {
        public int Id { get; set; }
        public int? Revision { get; set; }
        public bool? IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }

        [StringLength(100)] public string? SeasonName { get; set; }
        [StringLength(2000)] public string? CareProtocol { get; set; }
        [StringLength(3000)] public string? SeasonReview { get; set; }
        [StringLength(3000)] public string? NextSeasonChanges { get; set; }

        [StringLength(50)]
        [Display(Name = "Mã lô")]
        public string? BatchCode { get; set; } // Auto-generated

        [Display(Name = "Vùng trồng")]
        public int? AreaId { get; set; }

        [Display(Name = "Loại cây")]
        public int? PlantTypeId { get; set; }

        [Display(Name = "Giống")]
        public int? SeedId { get; set; }

        [Display(Name = "Số lượng cây")]
        public int? Quantity { get; set; }

        [Display(Name = "Ngày trồng")]
        [DataType(DataType.Date)]
        public DateTime? PlantingDate { get; set; }

        [Display(Name = "Ngày thu hoạch dự kiến")]
        [DataType(DataType.Date)]
        public DateTime? ExpectedHarvestDate { get; set; } // Auto: PlantingDate + GrowthCycleDays

        [Display(Name = "Ngày an toàn thu hoạch (PHI)")]
        [DataType(DataType.Date)]
        public DateTime? ExpectedSafeHarvestDate { get; set; } // Auto: Last pesticide + PHI

        [StringLength(20)]
        [Display(Name = "Trạng thái")]
        public string? Status { get; set; } = "Growing"; // Growing, ReadyToHarvest, Harvested, Destroyed

        [Display(Name = "Ghi chú")]
        public string? Notes { get; set; }

        [Display(Name = "Ngày tạo")]
        public DateTime? CreatedAt { get; set; } = DateTime.Now;

        // Navigation
        [NotMapped, ValidateNever]
        public Area? Area { get; set; }
        [NotMapped, ValidateNever]
        public PlantType? PlantType { get; set; }
        [NotMapped, ValidateNever]
        public Seed? Seed { get; set; }
        [NotMapped, ValidateNever]
        public ICollection<PesticideApplicationLog> PesticideApplicationLogs { get; set; } = new List<PesticideApplicationLog>();
        [NotMapped, ValidateNever]
        public ICollection<FertilizationLog> FertilizationLogs { get; set; } = new List<FertilizationLog>();
        [NotMapped, ValidateNever]
        public ICollection<WaterIrrigationLog> WaterIrrigationLogs { get; set; } = new List<WaterIrrigationLog>();
        [NotMapped, ValidateNever]
        public ICollection<HarvestBatch> HarvestBatches { get; set; } = new List<HarvestBatch>();
    }
}
