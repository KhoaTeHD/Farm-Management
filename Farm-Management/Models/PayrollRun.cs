using System.ComponentModel.DataAnnotations;

namespace Farm_Management.Models;

// All business fields are nullable; lifecycle and uniqueness are enforced by the backend.
public class PayrollRun
{
    public int Id { get; set; }
    public int? Year { get; set; }
    public int? Month { get; set; }
    [StringLength(20)] public string? Status { get; set; }
    public DateTime? FinalizedAt { get; set; }
    public DateTime? ReopenedAt { get; set; }
    public int? WorkerCount { get; set; }
    public decimal? TotalAmount { get; set; }
    [StringLength(64)] public string? AttendanceHash { get; set; }
    public int? CalculationVersion { get; set; }
    public decimal? TotalAdvances { get; set; }
    public decimal? NetTotal { get; set; }
    [StringLength(64)] public string? AdvanceHash { get; set; }
}
