using System.ComponentModel.DataAnnotations;

namespace Farm_Management.Models;

public interface ICultivationRecord
{
    int Id { get; set; }
    int? Revision { get; set; }
    bool? IsDeleted { get; set; }
    DateTime? DeletedAt { get; set; }
}

public class CultivationAudit
{
    public int Id { get; set; }
    [StringLength(60)] public string? RecordType { get; set; }
    public int? RecordId { get; set; }
    public int? PlantId { get; set; }
    [StringLength(20)] public string? Action { get; set; }
    public DateTime? OccurredAt { get; set; }
    [StringLength(100)] public string? Actor { get; set; }
    [StringLength(1000)] public string? Reason { get; set; }
    public string? BeforeJson { get; set; }
    public string? AfterJson { get; set; }
}

public class InputLot : ICultivationRecord
{
    public int Id { get; set; }
    public int? Revision { get; set; }
    public bool? IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    [StringLength(50)] public string? Code { get; set; }
    [StringLength(20)] public string? Kind { get; set; }
    [StringLength(200)] public string? ProductName { get; set; }
    [StringLength(100)] public string? ManufacturerBatch { get; set; }
    [StringLength(300)] public string? Supplier { get; set; }
    [StringLength(100)] public string? InvoiceNumber { get; set; }
    public DateTime? ReceivedDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public decimal? Quantity { get; set; }
    [StringLength(10)] public string? Unit { get; set; }
    [StringLength(100)] public string? RegistrationNumber { get; set; }
    [StringLength(20)] public string? Status { get; set; }
    [StringLength(2000)] public string? Notes { get; set; }
}

public class FieldResource : ICultivationRecord
{
    public int Id { get; set; }
    public int? Revision { get; set; }
    public bool? IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public int? AreaId { get; set; }
    [StringLength(20)] public string? Kind { get; set; }
    [StringLength(200)] public string? Name { get; set; }
    [StringLength(500)] public string? Location { get; set; }
    [StringLength(20)] public string? Status { get; set; }
    [StringLength(2000)] public string? RiskAssessment { get; set; }
    [StringLength(2000)] public string? Treatment { get; set; }
}

public class InspectionReport : ICultivationRecord
{
    public int Id { get; set; }
    public int? Revision { get; set; }
    public bool? IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    [StringLength(20)] public string? TargetType { get; set; }
    public int? TargetId { get; set; }
    [StringLength(100)] public string? ReportNumber { get; set; }
    [StringLength(200)] public string? Laboratory { get; set; }
    public DateTime? SampleDate { get; set; }
    public DateTime? ReportDate { get; set; }
    [StringLength(200)] public string? Indicator { get; set; }
    [StringLength(2000)] public string? Result { get; set; }
    [StringLength(1000)] public string? LimitReference { get; set; }
    [StringLength(20)] public string? Conclusion { get; set; }
    [StringLength(100)] public string? EvidencePath { get; set; }
}

public class HarvestTrace : ICultivationRecord
{
    public int Id { get; set; }
    public int? Revision { get; set; }
    public bool? IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public int? PlantId { get; set; }
    public int? JournalEntryId { get; set; }
    public int? LegacyHarvestId { get; set; }
    [StringLength(50)] public string? Code { get; set; }
    public DateTime? HarvestDate { get; set; }
    public decimal? WeightKg { get; set; }
    [StringLength(50)] public string? Quality { get; set; }
    public string? SnapshotJson { get; set; }
    [StringLength(64)] public string? SourceHash { get; set; }
    public bool? IsVoided { get; set; }
}
