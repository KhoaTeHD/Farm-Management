namespace Farm_Management.Models.ViewModels;

public class DashboardViewModel
{
    public int SeasonCount { get; set; }
    public int GrowingSeasons { get; set; }
    public int JournalCount { get; set; }
    public List<CropSeasonViewModel> Seasons { get; set; } = new();
}
