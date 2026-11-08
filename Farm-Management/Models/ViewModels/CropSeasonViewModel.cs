using Farm_Management.Services;

namespace Farm_Management.Models.ViewModels;

public record CropTimelineItem(DateTime? Date, string Kind, string Title, string? Description, int? EntryId = null);

public class CropSeasonViewModel
{
    public required Plant Plant { get; set; }
    public List<CropJournalEntry> Entries { get; set; } = new();
    public List<CropCareStep> Steps { get; set; } = new();
    public List<CropTimelineItem> Timeline { get; set; } = new();
    public List<MaterialUsageRow> MaterialUses { get; set; } = new();
    public DateTime? SafeDate { get; set; }
    public bool SafetyIncomplete { get; set; }
    public int IncompleteEntries { get; set; }
    public int LegacyHarvestCount { get; set; }
    public decimal? HarvestKg { get; set; }
    public bool HarvestIncomplete { get; set; }
    public CropJournalEntry? LatestObservation => Entries.Where(e => e.Kind == "Observation" && CropSeasonRules.Complete(e, Plant)).OrderByDescending(e => e.Date).ThenByDescending(e => e.Id).FirstOrDefault();
    public bool PlanComplete(CropCareStep step) => CropSeasonRules.PlanDate(Plant.PlantingDate, step.PlannedDay).HasValue && CropSeasonRules.Kinds.ContainsKey(step.Kind ?? "") && !string.IsNullOrWhiteSpace(step.Title) && !string.IsNullOrWhiteSpace(step.Instructions);
    public int IncompleteSteps => Steps.Count(s => !PlanComplete(s));
    public bool StepDone(CropCareStep step) => PlanComplete(step) && Entries.Any(e => e.CareStepId == step.Id && e.PlantId == step.PlantId && e.Kind == step.Kind && CropSeasonRules.Complete(e, Plant));
    public int DoneSteps => Steps.Count(StepDone);
    public CropJournalEntry? FirstActual(CropCareStep step) => Entries.Where(e => e.CareStepId == step.Id && e.PlantId == step.PlantId && e.Kind == step.Kind && CropSeasonRules.Complete(e, Plant)).OrderBy(e => e.Date).ThenBy(e => e.Id).FirstOrDefault();
    public int DueSteps => Steps.Count(s => !StepDone(s) && CropSeasonRules.PlanDate(Plant.PlantingDate, s.PlannedDay) <= DateTime.Today);
}

public class CropComparisonViewModel
{
    public int? LeftId { get; set; }
    public int? RightId { get; set; }
    public List<Plant> Choices { get; set; } = new();
    public CropSeasonViewModel? Left { get; set; }
    public CropSeasonViewModel? Right { get; set; }
}
