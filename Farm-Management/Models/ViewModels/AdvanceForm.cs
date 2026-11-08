using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Farm_Management.Models.ViewModels;

public class AdvanceForm : IValidatableObject
{
    public int Id { get; set; }
    [Required, Range(1, int.MaxValue), Display(Name = "Công nhân")] public int? WorkerId { get; set; }
    [Required, StringLength(50), Display(Name = "Mã phiếu tạm ứng")] public string? Reference { get; set; }
    [Required, DataType(DataType.Date), Display(Name = "Ngày ứng")] public DateTime? PaidDate { get; set; }
    [Required, Range(2, 9998), Display(Name = "Năm khấu trừ")] public int? PayrollYear { get; set; }
    [Required, Range(1, 12), Display(Name = "Tháng khấu trừ")] public int? PayrollMonth { get; set; }
    [Required, Range(typeof(decimal), "1", "1000000000"), Display(Name = "Số tiền ứng (đ)")] public decimal? Amount { get; set; }
    [StringLength(500), Display(Name = "Ghi chú")] public string? Notes { get; set; }
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (PaidDate.HasValue && (PaidDate.Value.Year < 2 || PaidDate.Value.Date > DateTime.Today))
            yield return new("Ngày ứng phải là ngày đã phát sinh, không ở tương lai.", new[] { nameof(PaidDate) });
        if (Amount.HasValue && decimal.Truncate(Amount.Value) != Amount)
            yield return new("Nhập số tiền nguyên đồng.", new[] { nameof(Amount) });
    }
    public static AdvanceForm FromEntity(WorkerAdvance a) => new() { Id = a.Id, WorkerId = a.WorkerId, Reference = a.Reference, PaidDate = a.PaidDate, PayrollYear = a.PayrollYear, PayrollMonth = a.PayrollMonth, Amount = a.Amount, Notes = a.Notes };
}

public class AdvanceListViewModel
{
    public int Year { get; set; }
    public int Month { get; set; }
    public bool Incomplete { get; set; }
    public bool Locked { get; set; }
    public List<WorkerAdvance> Items { get; set; } = new();
    public Dictionary<int, Worker> Workers { get; set; } = new();
}

public class PayrollAdvanceSnapshot
{
    public required int AdvanceId { get; set; }
    public required string Reference { get; set; }
    public required DateTime PaidDate { get; set; }
    public required decimal Amount { get; set; }
}
