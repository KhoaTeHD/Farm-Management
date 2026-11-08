using System.ComponentModel.DataAnnotations;

namespace Farm_Management.Models;

public class WorkerAdvance
{
    public int Id { get; set; }
    public int? WorkerId { get; set; }
    [StringLength(50)] public string? Reference { get; set; }
    public DateTime? PaidDate { get; set; }
    public int? PayrollYear { get; set; }
    public int? PayrollMonth { get; set; }
    public decimal? Amount { get; set; }
    [StringLength(500)] public string? Notes { get; set; }
    public DateTime? CreatedAt { get; set; }
}
