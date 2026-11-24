namespace Farm_Management.Models.ViewModels;

public class DailyWorkRow
{
    public required CropJournalEntry Entry { get; set; }
    public string? PlantName { get; set; }
    public string? PlantBatchCode { get; set; }
    public string? AreaName { get; set; }
    public List<string> WorkerNames { get; set; } = new();
    public string? MaterialDisplay { get; set; }
}

public class DailyWorkIndexViewModel
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int? PlantId { get; set; }
    public int? WorkerId { get; set; }
    public string? Kind { get; set; }
    public int? MaterialItemId { get; set; }

    public List<Plant> Plants { get; set; } = new();
    public List<Worker> Workers { get; set; } = new();
    public List<MaterialItem> Materials { get; set; } = new();
    public List<DailyWorkRow> Items { get; set; } = new();
}
