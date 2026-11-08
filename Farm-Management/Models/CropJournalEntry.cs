using System.ComponentModel.DataAnnotations;

namespace Farm_Management.Models;

// A Plant is one lot in one season. References deliberately have no EF relationships.
public class CropJournalEntry : ICultivationRecord
{
    public int Id { get; set; }
        public int? Revision { get; set; }
        public bool? IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
    public int? PlantId { get; set; }
    public int? InputLotId { get; set; }
    public int? WaterResourceId { get; set; }
    public int? CareStepId { get; set; }
    [StringLength(40)] public string? RecordCode { get; set; }
    public DateTime? Date { get; set; }
    [StringLength(30)] public string? Kind { get; set; }
    [StringLength(100)] public string? Recorder { get; set; }
    [StringLength(2000)] public string? Summary { get; set; }
    [StringLength(200)] public string? Material { get; set; }
    public decimal? Amount { get; set; }
    [StringLength(10)] public string? Unit { get; set; }
    [StringLength(200)] public string? Method { get; set; }
    [StringLength(200)] public string? Weather { get; set; }
    public int? WithdrawalDays { get; set; }
    [StringLength(30)] public string? GrowthStage { get; set; }
    [StringLength(20)] public string? Condition { get; set; }
    public decimal? HeightCm { get; set; }
    public decimal? SurvivalPercent { get; set; }
    public decimal? AffectedPercent { get; set; }
    [StringLength(50)] public string? Quality { get; set; }
    [StringLength(100)] public string? PhotoPath { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
