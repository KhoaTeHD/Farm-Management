using System.ComponentModel.DataAnnotations;

namespace Farm_Management.Models.ViewModels;

public class LoginForm
{
    [Required(ErrorMessage = "Nhập tên đăng nhập."), StringLength(50)]
    [Display(Name = "Tên đăng nhập")] public string Username { get; set; } = "";
    [Required(ErrorMessage = "Nhập mật khẩu."), StringLength(128), DataType(DataType.Password)]
    [Display(Name = "Mật khẩu")] public string Password { get; set; } = "";
    public string? ReturnUrl { get; set; }
}

public class AccountForm
{
    public int Id { get; set; }
    public int? Revision { get; set; }
    [Required, StringLength(50), RegularExpression(@"[a-zA-Z0-9._-]{3,50}", ErrorMessage = "Tên đăng nhập gồm 3–50 chữ không dấu, số, dấu chấm, gạch ngang hoặc gạch dưới.")]
    [Display(Name = "Tên đăng nhập")] public string Username { get; set; } = "";
    [Required, StringLength(100)]
    [Display(Name = "Họ tên")] public string DisplayName { get; set; } = "";
    [Required, RegularExpression("Admin|Staff")]
    [Display(Name = "Quyền truy cập")] public string Role { get; set; } = "Staff";
    [Display(Name = "Cho phép đăng nhập")] public bool IsActive { get; set; } = true;
    [StringLength(128), DataType(DataType.Password)]
    [Display(Name = "Mật khẩu mới")] public string? NewPassword { get; set; }
}

public class ChangePasswordForm
{
    [Required, StringLength(128), DataType(DataType.Password)]
    [Display(Name = "Mật khẩu hiện tại")] public string CurrentPassword { get; set; } = "";
    [Required, StringLength(128), DataType(DataType.Password)]
    [Display(Name = "Mật khẩu mới")] public string NewPassword { get; set; } = "";
    [Required, Compare(nameof(NewPassword), ErrorMessage = "Hai mật khẩu mới chưa giống nhau."), DataType(DataType.Password)]
    [Display(Name = "Nhập lại mật khẩu mới")] public string ConfirmPassword { get; set; } = "";
}
