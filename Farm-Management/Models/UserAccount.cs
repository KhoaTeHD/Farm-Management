using System.ComponentModel.DataAnnotations;

namespace Farm_Management.Models;

// Business fields stay nullable; validation and uniqueness are enforced by the service.
public class UserAccount
{
    public int Id { get; set; }
    [StringLength(50)] public string? Username { get; set; }
    [StringLength(50)] public string? NormalizedUsername { get; set; }
    [StringLength(100)] public string? DisplayName { get; set; }
    public string? PasswordHash { get; set; }
    [StringLength(20)] public string? Role { get; set; }
    public bool? IsActive { get; set; }
    public bool? MustChangePassword { get; set; }
    [StringLength(64)] public string? SecurityStamp { get; set; }
    public int? FailedLoginCount { get; set; }
    public DateTime? LockoutUntil { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? Revision { get; set; }
}
