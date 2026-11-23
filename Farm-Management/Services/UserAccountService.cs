using System.Security.Claims;
using System.Text.RegularExpressions;
using Farm_Management.Data;
using Farm_Management.Models;
using Farm_Management.Models.ViewModels;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Farm_Management.Services;

public sealed class AdminBootstrapException(int exitCode, string message) : InvalidOperationException(message)
{
    public int ExitCode { get; } = exitCode;
}

public class UserAccountService(ApplicationDbContext db)
{
    private static readonly PasswordHasher<UserAccount> Hasher = new();
    private static readonly UserAccount Dummy = new();
    private static readonly string DummyHash = Hasher.HashPassword(Dummy, Guid.NewGuid().ToString());
    public static string Normalize(string username) => username.Trim().ToUpperInvariant();
    public static string? PasswordError(string? password) => password == null || password.Length is < 12 or > 128
        || !password.Any(char.IsLetter) || !password.Any(char.IsDigit)
        ? "Mật khẩu cần 12–128 ký tự, có cả chữ và số." : null;
    public static bool Usable(UserAccount u) => u.IsActive == true && u.Role is "Admin" or "Staff"
        && !string.IsNullOrWhiteSpace(u.Username) && !string.IsNullOrWhiteSpace(u.DisplayName)
        && !string.IsNullOrWhiteSpace(u.PasswordHash) && !string.IsNullOrWhiteSpace(u.SecurityStamp)
        && u.MustChangePassword.HasValue;
    public static ClaimsPrincipal Principal(UserAccount u) => new(new ClaimsIdentity(new[]
    {
        new Claim(ClaimTypes.NameIdentifier, u.Id.ToString()), new Claim(ClaimTypes.Name, u.Username!),
        new Claim("display_name", u.DisplayName!), new Claim(ClaimTypes.Role, u.Role!),
        new Claim("security_stamp", u.SecurityStamp!), new Claim("must_change_password", u.MustChangePassword == true ? "true" : "false")
    }, CookieAuthenticationDefaults.AuthenticationScheme));
    private static bool Verify(UserAccount u, string password)
    {
        try { return Hasher.VerifyHashedPassword(u, u.PasswordHash!, password) != PasswordVerificationResult.Failed; }
        catch (FormatException) { return false; }
        catch (ArgumentException) { return false; }
    }
    private static void Touch(UserAccount u)
    {
        u.SecurityStamp = Guid.NewGuid().ToString("N");
        u.UpdatedAt = DateTime.UtcNow; u.Revision = (u.Revision ?? 0) + 1;
    }
    public async Task<UserAccount?> LoginAsync(string username, string password)
    {
        await using var tx = await db.BeginBusinessWriteAsync();
        var matches = await db.UserAccounts.Where(u => u.NormalizedUsername == Normalize(username)).Take(2).ToListAsync();
        var user = matches.Count == 1 ? matches[0] : null;
        if (user == null || !Usable(user) || user.LockoutUntil > DateTime.UtcNow)
        {
            Hasher.VerifyHashedPassword(Dummy, DummyHash, password);
            return null;
        }
        if (!Verify(user, password))
        {
            user.FailedLoginCount = (user.FailedLoginCount ?? 0) + 1;
            if (user.FailedLoginCount >= 5) { user.LockoutUntil = DateTime.UtcNow.AddMinutes(15); user.FailedLoginCount = 0; }
            await db.SaveChangesAsync(); await tx.CommitAsync(); return null;
        }
        user.FailedLoginCount = 0; user.LockoutUntil = null;
        await db.SaveChangesAsync(); await tx.CommitAsync(); return user;
    }
    public async Task<string?> SaveAsync(AccountForm form, int actorId)
    {
        var name = form.Username.Trim(); var display = form.DisplayName.Trim();
        if (!Regex.IsMatch(name, @"\A[a-zA-Z0-9._-]{3,50}\z") || display.Length is < 1 or > 100 || form.Role is not ("Admin" or "Staff"))
            return "Kiểm tra lại tên đăng nhập, họ tên và quyền truy cập.";
        if ((form.Id == 0 || !string.IsNullOrEmpty(form.NewPassword)) && PasswordError(form.NewPassword) is string error) return error;
        await using var tx = await db.BeginBusinessWriteAsync();
        var actor = await db.UserAccounts.FindAsync(actorId);
        if (actor == null || !Usable(actor) || actor.Role != "Admin") return "Tài khoản hiện tại không còn quyền quản trị.";
        if (await db.UserAccounts.AnyAsync(u => u.NormalizedUsername == Normalize(name) && u.Id != form.Id)) return "Tên đăng nhập đã được sử dụng.";
        UserAccount user;
        if (form.Id == 0) { user = new() { CreatedAt = DateTime.UtcNow, MustChangePassword = true, FailedLoginCount = 0 }; db.UserAccounts.Add(user); }
        else
        {
            user = (await db.UserAccounts.FindAsync(form.Id))!;
            if (user == null) return "Tài khoản không tồn tại.";
            if (form.Revision != user.Revision) return "Tài khoản đã thay đổi. Mở lại trang rồi thử lại.";
            if (user.Id == actorId && (!form.IsActive || form.Role != "Admin")) return "Không thể tự khóa hoặc tự bỏ quyền quản trị của mình.";
            if (user.Role == "Admin" && user.IsActive == true && (!form.IsActive || form.Role != "Admin")
                && !await db.UserAccounts.AnyAsync(u => u.Id != user.Id && u.Role == "Admin" && u.IsActive == true && u.PasswordHash != null && u.SecurityStamp != null && u.MustChangePassword != null))
                return "Cần giữ ít nhất một tài khoản quản trị hoạt động.";
        }
        user.Username = name; user.NormalizedUsername = Normalize(name); user.DisplayName = display;
        user.Role = form.Role; user.IsActive = form.IsActive;
        if (!string.IsNullOrEmpty(form.NewPassword))
        {
            user.PasswordHash = Hasher.HashPassword(user, form.NewPassword); user.MustChangePassword = true;
            user.FailedLoginCount = 0; user.LockoutUntil = null;
        }
        Touch(user); await db.SaveChangesAsync(); await tx.CommitAsync(); return null;
    }
    public async Task<string?> ChangePasswordAsync(int id, ChangePasswordForm form)
    {
        if (PasswordError(form.NewPassword) is string error) return error;
        if (form.NewPassword != form.ConfirmPassword) return "Hai mật khẩu mới chưa giống nhau.";
        await using var tx = await db.BeginBusinessWriteAsync();
        var user = await db.UserAccounts.FindAsync(id);
        if (user == null || !Usable(user) || user.LockoutUntil > DateTime.UtcNow) return "Không thể đổi mật khẩu. Thử lại sau hoặc liên hệ quản trị.";
        if (!Verify(user, form.CurrentPassword))
        {
            user.FailedLoginCount = (user.FailedLoginCount ?? 0) + 1;
            if (user.FailedLoginCount >= 5) { user.LockoutUntil = DateTime.UtcNow.AddMinutes(15); user.FailedLoginCount = 0; }
            await db.SaveChangesAsync(); await tx.CommitAsync(); return "Mật khẩu hiện tại chưa đúng.";
        }
        if (Verify(user, form.NewPassword)) return "Mật khẩu mới phải khác mật khẩu hiện tại.";
        user.PasswordHash = Hasher.HashPassword(user, form.NewPassword); user.MustChangePassword = false;
        user.FailedLoginCount = 0; user.LockoutUntil = null; Touch(user);
        await db.SaveChangesAsync(); await tx.CommitAsync(); return null;
    }
    public async Task<int> InitialAdminStatusAsync()
    {
        var admins = await db.UserAccounts.AsNoTracking().Where(u => u.Role == "Admin" && u.IsActive == true).ToListAsync();
        if (admins.Any(Usable)) return 10;
        return await db.UserAccounts.AnyAsync(u => u.NormalizedUsername != null || u.PasswordHash != null) ? 14 : 0;
    }
    public async Task CreateInitialAdminAsync(string username, string password)
    {
        if (!Regex.IsMatch(username, @"\A[a-zA-Z0-9._-]{3,50}\z") || PasswordError(password) != null)
            throw new AdminBootstrapException(11, "Username must be 3-50 ASCII letters/numbers/dot/underscore/dash; password must be 12-128 characters including letters and numbers.");
        await using var tx = await db.BeginBusinessWriteAsync();
        var status = await InitialAdminStatusAsync();
        if (status != 0) throw new AdminBootstrapException(status, status == 10
            ? "An active administrator already exists. Existing accounts and passwords are unchanged."
            : "Accounts exist but no usable active administrator was found. Bootstrap cannot reset or replace existing accounts.");
        var user = new UserAccount { Username = username, NormalizedUsername = Normalize(username), DisplayName = "Quản trị trang trại", Role = "Admin", IsActive = true, MustChangePassword = true, FailedLoginCount = 0, CreatedAt = DateTime.UtcNow };
        user.PasswordHash = Hasher.HashPassword(user, password); Touch(user);
        db.UserAccounts.Add(user); await db.SaveChangesAsync(); await tx.CommitAsync();
    }
}
