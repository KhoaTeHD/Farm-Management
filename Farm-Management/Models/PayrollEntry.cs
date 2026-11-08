using System.ComponentModel.DataAnnotations;

namespace Farm_Management.Models;

public class PayrollEntry
{
    public int Id { get; set; }
    public int? PayrollRunId { get; set; }
    public int? WorkerId { get; set; }
    [StringLength(20)] public string? WorkerCode { get; set; }
    [StringLength(100)] public string? WorkerName { get; set; }
    public decimal? DailySalary { get; set; }
    public int? FullDays { get; set; }
    public int? HalfDays { get; set; }
    public decimal? OddHours { get; set; }
    public decimal? TotalHours { get; set; }
    public int? AttendanceCount { get; set; }
    public decimal? Amount { get; set; }
    // Immutable source used to explain a finalized month's calculation, even if soft data changes.
    public string? AttendanceJson { get; set; }
    public decimal? AdvanceAmount { get; set; }
    public decimal? NetAmount { get; set; }
    public string? AdvancesJson { get; set; }
}
