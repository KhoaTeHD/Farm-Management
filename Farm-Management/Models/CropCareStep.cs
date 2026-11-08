using System.ComponentModel.DataAnnotations;

namespace Farm_Management.Models;

public class CropCareStep : ICultivationRecord
{
    public int Id { get; set; }
        public int? Revision { get; set; }
        public bool? IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
    public int? PlantId { get; set; }
    [StringLength(150)] public string? Title { get; set; }
    [StringLength(30)] public string? Kind { get; set; }
    public int? PlannedDay { get; set; }
    [StringLength(2000)] public string? Instructions { get; set; }
    public DateTime? CreatedAt { get; set; }
}
