namespace Farm_Management.Models.ViewModels;

public class MaterialUsageRow
{
    public string? Kind { get; set; }
    public string? ProductName { get; set; }
    public DateTime? Date { get; set; }
    public string? QuantityText { get; set; }
    public string? PerformedBy { get; set; }
    public int? WithdrawalDays { get; set; }
    public bool IsLegacy { get; set; }
    public int? JournalEntryId { get; set; }
}

public class MaterialUsageTableViewModel
{
    public List<MaterialUsageRow> Rows { get; set; } = new();
    public bool CanEdit { get; set; }
    public bool IsSnapshot { get; set; }
    public bool OlderLegacySnapshot { get; set; }
}
