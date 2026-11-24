using System.ComponentModel.DataAnnotations;

namespace Farm_Management.Models;

/// <summary>
/// Phân công 1 hoặc nhiều công nhân cho một bản ghi công việc hằng ngày.
/// </summary>
public class CropJournalWorker
{
    public int Id { get; set; }
    public int? CropJournalEntryId { get; set; }
    public int? WorkerId { get; set; }

    [StringLength(100)]
    public string? WorkerName { get; set; }
}
