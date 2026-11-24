namespace Farm_Management.Models.ViewModels;

public class DashboardViewModel
{
    public int SeasonCount { get; set; }
    public int GrowingSeasons { get; set; }
    public int JournalCount { get; set; }
    public int TodayWorkCount { get; set; }
    public int MaterialCount { get; set; }
    public int ActiveWorkerCount { get; set; }
    public List<CropSeasonViewModel> Seasons { get; set; } = new();
    public List<DailyWorkRow> RecentWorks { get; set; } = new();
}
