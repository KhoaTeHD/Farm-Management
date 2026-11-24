namespace Farm_Management.Services;

public static class MaterialRules
{
    public static IReadOnlyDictionary<string, string> Categories { get; } = new Dictionary<string, string>
    {
        ["Fertilizer"] = "Phân bón",
        ["Pesticide"] = "Thuốc BVTV / trừ sâu",
        ["Other"] = "Vật liệu khác"
    };

    public static string[] SuggestedUnits { get; } = ["kg", "g", "l", "ml", "bao", "chai", "gói", "bình", "m3"];

    public static string CategoryLabel(string? category) =>
        category != null && Categories.TryGetValue(category, out var label) ? label : "Chưa phân nhóm";

    public static bool AllowsKind(string? category, string? journalKind) => journalKind switch
    {
        "Fertilize" => category is "Fertilizer" or "Other",
        "Pesticide" => category is "Pesticide" or "Other",
        "Care" => Categories.ContainsKey(category ?? ""),
        _ => false
    };
}
