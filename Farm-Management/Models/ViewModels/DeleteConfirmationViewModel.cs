namespace Farm_Management.Models.ViewModels;

public class DeleteConfirmationViewModel
{
    public bool CanDelete { get; set; } = true;
    public int Id { get; set; }
    public string Title { get; set; } = "Xóa bản ghi";
    public string Description { get; set; } = "";
    public string? Notice { get; set; }
    public DateTime? Date { get; set; }
    public List<(string Label, string Value)> Items { get; set; } = new();
}
