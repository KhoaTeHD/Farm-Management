using System.ComponentModel.DataAnnotations;

namespace Farm_Management.Models.ViewModels;

public class PayrollMonthForm
{
    [Required, Range(2, 9998)] public int? Year { get; set; }
    [Required, Range(1, 12)] public int? Month { get; set; }
    [Required, StringLength(64, MinimumLength = 64)] public string? ReviewToken { get; set; }
}

public record PayrollIssue(string Message, int? WorkerId = null, DateTime? Date = null, bool Advance = false);

public class PayrollRow
{
    public int WorkerId { get; set; }
    public string? Code { get; set; }
    public string? Name { get; set; }
    public decimal? DailySalary { get; set; }
    public int FullDays { get; set; }
    public int HalfDays { get; set; }
    public decimal OddHours { get; set; }
    public decimal TotalHours { get; set; }
    public int AttendanceCount { get; set; }
    public decimal? Amount { get; set; }
    public bool AdvancesInvalid { get; set; }
    public decimal? AdvanceAmount { get; set; }
    public decimal? NetAmount { get; set; }
    public List<PayrollAdvanceSnapshot> Advances { get; set; } = new();
    public decimal WorkDays => FullDays + HalfDays * 0.5m;
    public List<PayrollDay> Days { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}

public class PayrollDay
{
    public required int AttendanceId { get; set; }
    public required DateTime Date { get; set; }
    public required decimal Hours { get; set; }
}

public class PayrollMonthViewModel
{
    public int Year { get; set; }
    public int Month { get; set; }
    public List<PayrollRow> Rows { get; set; } = new();
    public List<PayrollIssue> Issues { get; set; } = new();
    public List<PayrollRun> History { get; set; } = new();
    public PayrollRun? Finalized { get; set; }
    public bool SourceChanged { get; set; }
    public bool SnapshotInvalid { get; set; }
    public bool ConflictingRuns { get; set; }
    public bool IsArchive { get; set; }
    public string ReviewToken { get; set; } = "";
    public string AttendanceHash { get; set; } = "";
    public string AdvanceHash { get; set; } = "";
    public int AdvanceRecordCount { get; set; }
    public bool CanFinalize => !IsArchive && Finalized == null && !ConflictingRuns && Issues.Count == 0 && Rows.Count > 0;
    public decimal Total => Rows.Where(r => r.NetAmount.HasValue && r.Errors.Count == 0).Sum(r => r.Amount!.Value);
    public decimal TotalAdvances => Rows.Where(r => r.NetAmount.HasValue && r.Errors.Count == 0).Sum(r => r.AdvanceAmount!.Value);
    public decimal NetTotal => Rows.Where(r => r.NetAmount.HasValue && r.Errors.Count == 0).Sum(r => r.NetAmount!.Value);
    public int ValidWorkerCount => Rows.Count(r => r.NetAmount.HasValue && r.Errors.Count == 0);
}
