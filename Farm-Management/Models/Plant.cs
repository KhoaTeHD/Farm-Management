using System.ComponentModel.DataAnnotations;

namespace Farm_Management.Models
{
    public class Plant
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Mã lô")]
        public string BatchCode { get; set; } = string.Empty; // Auto-generated

        [Required]
        [Display(Name = "Vùng trồng")]
        public int AreaId { get; set; }

        [Required]
        [Display(Name = "Loại cây")]
        public int PlantTypeId { get; set; }

        [Required]
        [Display(Name = "Giống")]
        public int SeedId { get; set; }

        [Display(Name = "Số lượng cây")]
        public int? Quantity { get; set; }

        [Required(ErrorMessage = "Ngày trồng là bắt buộc")]
        [Display(Name = "Ngày trồng")]
        [DataType(DataType.Date)]
        public DateTime PlantingDate { get; set; }

        [Display(Name = "Ngày thu hoạch dự kiến")]
        [DataType(DataType.Date)]
        public DateTime? ExpectedHarvestDate { get; set; } // Auto: PlantingDate + GrowthCycleDays

        [Display(Name = "Ngày an toàn thu hoạch (PHI)")]
        [DataType(DataType.Date)]
        public DateTime? ExpectedSafeHarvestDate { get; set; } // Auto: Last pesticide + PHI

        [Required]
        [StringLength(20)]
        [Display(Name = "Trạng thái")]
        public string Status { get; set; } = "Growing"; // Growing, ReadyToHarvest, Harvested, Destroyed

        [Display(Name = "Ghi chú")]
        public string? Notes { get; set; }

        [Display(Name = "Ngày tạo")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation
        public Area Area { get; set; } = null!;
        public PlantType PlantType { get; set; } = null!;
        public Seed Seed { get; set; } = null!;
        public ICollection<PesticideApplicationLog> PesticideApplicationLogs { get; set; } = new List<PesticideApplicationLog>();
        public ICollection<FertilizationLog> FertilizationLogs { get; set; } = new List<FertilizationLog>();
        public ICollection<WaterIrrigationLog> WaterIrrigationLogs { get; set; } = new List<WaterIrrigationLog>();
        public ICollection<HarvestBatch> HarvestBatches { get; set; } = new List<HarvestBatch>();
    }
}