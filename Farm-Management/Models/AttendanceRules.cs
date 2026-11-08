namespace Farm_Management.Models;

public static class AttendanceRules
{
    public static bool IsValidHours(decimal? hours) => hours.HasValue && hours >= 0 && hours <= 24 && hours % 0.5m == 0;

    public static string? Classify(decimal? hours) => !IsValidHours(hours) ? null : hours switch
    {
        null or < 0 or > 24 => null,
        >= 8 => "FullDay",
        4 => "HalfDay",
        _ => "OddHours"
    };
}
