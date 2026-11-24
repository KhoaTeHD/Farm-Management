using System.Net;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Farm_Management.Data;
using Farm_Management.Models.ViewModels;
using Farm_Management.Services;
using Microsoft.EntityFrameworkCore;

internal static class AccountChecks
{
    public const string AdminName = "qa-admin";
    private const string Initial = "Initial-QA-Password-123";
    private const string Password = "Changed-QA-Password-456";
    public static async Task Integration(HttpClient admin, DbContextOptions<ApplicationDbContext> options, Action<bool, string> check)
    {
        await using (var db = new ApplicationDbContext(options))
        {
            var connection = db.Database.GetConnectionString()!;
            check((await Bootstrap("--check-admin", connection)).Code == 0, "Bootstrap CLI reports empty database before asking for credentials");
            check((await Bootstrap("--create-admin", connection, "short")).Code == 11 && !await db.UserAccounts.AnyAsync(u => u.Username == AdminName), "Bootstrap CLI rejects short password with explicit code and no account write");
            check((await Bootstrap("--create-admin", connection, Initial)).Code == 0, "Bootstrap CLI creates first administrator without interactive process output");
            check((await Bootstrap("--check-admin", connection)).Code == 10, "Bootstrap CLI detects existing administrator for installer skip");
            check((await Bootstrap("--create-admin", connection, Initial)).Code == 10, "Repeated bootstrap returns existing-admin code without resetting password");
            var user = await db.UserAccounts.SingleAsync(u => u.Username == AdminName);
            user.IsActive = false; await db.SaveChangesAsync();
            check((await Bootstrap("--check-admin", connection)).Code == 14, "Accounts without active administrator cannot be silently skipped");
            user.IsActive = true; await db.SaveChangesAsync();
            var unavailable = await Bootstrap("--check-admin", "Server=127.0.0.1,1;Database=Unused;User ID=qa;Password=NeverPrintThis123;Connect Timeout=1;Encrypt=True;TrustServerCertificate=True");
            check(unavailable.Code == 12 && !unavailable.Text.Contains("NeverPrintThis123") && unavailable.Text.Contains("migrations"), "Bootstrap CLI reports database failure without leaking credentials or raw stack trace");
            check(user.PasswordHash != Initial && user.PasswordHash!.Length > 50 && user.MustChangePassword == true, "Initial administrator uses a password hash and mandatory password change");
            try { await new UserAccountService(db).CreateInitialAdminAsync("other-admin", Initial); check(false, "Bootstrap must not replace existing accounts"); }
            catch (InvalidOperationException) { check(true, "Bootstrap cannot replace existing accounts"); }
        }
        foreach (var path in new[] { "/", "/Plants", "/Payroll", "/Accounts", "/HarvestTrace", "/RecordHistory", "/uploads/crops/test.jpg", "/uploads/evidence/test.pdf" })
            check(IsLogin(await admin.GetAsync(path)), "Anonymous request is blocked: " + path);
        check(IsLogin(await admin.PostAsync("/Workers/Create", new FormUrlEncodedContent(new Dictionary<string,string>()))), "Anonymous writes cannot bypass login");
        check((await admin.GetAsync("/Account/Login")).StatusCode == HttpStatusCode.OK, "Login page is anonymous");
        check((await admin.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string,string> { ["Username"] = AdminName, ["Password"] = Initial }))).StatusCode == HttpStatusCode.BadRequest, "Login requires antiforgery token");
        var login = await Login(admin, "  QA-ADMIN  ", Initial);
        check(login.Headers.Location?.OriginalString == "/Account/ChangePassword", "Login normalizes username and requires first password change");
        var cookie = login.Headers.GetValues("Set-Cookie").First(v => v.StartsWith("FarmManagement.Auth="));
        check(cookie.Contains("httponly", StringComparison.OrdinalIgnoreCase) && cookie.Contains("samesite=lax", StringComparison.OrdinalIgnoreCase), "Authentication cookie is HttpOnly and SameSite Lax");
        check((await admin.GetAsync("/Plants")).Headers.Location?.OriginalString == "/Account/ChangePassword", "Temporary password cannot access business screens");
        check((await admin.GetAsync("/uploads/evidence/test.pdf")).Headers.Location?.OriginalString == "/Account/ChangePassword", "Temporary password cannot access uploaded evidence");
        check((await admin.GetAsync("/Account/Logout")).StatusCode != HttpStatusCode.OK, "Logout cannot be invoked by GET");
        check((await Post(admin, "/Account/ChangePassword", new() { ["CurrentPassword"] = Initial, ["NewPassword"] = "short", ["ConfirmPassword"] = "short" })).StatusCode == HttpStatusCode.OK, "Weak password is rejected");
        using var stale = Client(admin.BaseAddress!);
        await Login(stale, AdminName, Initial);
        check((await Change(admin, Initial, Password)).StatusCode == HttpStatusCode.Redirect, "First password change succeeds");
        check(IsLogin(await stale.GetAsync("/Plants")), "Password change immediately revokes other sessions");
        check((await Login(admin, AdminName, Initial)).StatusCode == HttpStatusCode.OK, "Old password no longer works");
        check((await Login(admin, AdminName, Password, "https://evil.invalid/")).Headers.Location?.OriginalString == "/", "Login rejects external return URL");
        check((await admin.GetAsync("/Accounts")).StatusCode == HttpStatusCode.OK, "Administrator can manage accounts");
        var fields = Form("qa-staff");
        check((await Post(admin, "/Accounts/Create", fields)).StatusCode == HttpStatusCode.Redirect, "Administrator creates a staff account");
        var badRole = Form("qa-bad-role"); badRole["Role"] = "Owner";
        check((await Post(admin, "/Accounts/Create", badRole)).StatusCode == HttpStatusCode.OK, "Backend rejects undefined account role");
        var duplicate = Form("QA-STAFF");
        check((await Post(admin, "/Accounts/Create", duplicate)).StatusCode == HttpStatusCode.OK, "Account names are unique ignoring case without a database unique constraint");
        var races = await Task.WhenAll(Post(admin, "/Accounts/Create", Form("qa-race")), Post(admin, "/Accounts/Create", Form("qa-race")));
        check(races.Count(r => r.StatusCode == HttpStatusCode.Redirect) == 1, "Concurrent account creation inserts only one username");
        using var staff = Client(admin.BaseAddress!);
        await Login(staff, "qa-staff", Initial);
        await Change(staff, Initial, Password);
        await Login(staff, "qa-staff", Password);
        check((await staff.GetAsync("/Plants")).StatusCode == HttpStatusCode.OK, "Staff can use crop tracking");
        check((await staff.GetStringAsync("/")).Contains("Quản lý tài khoản") == false, "Staff navigation hides account administration");
        check((await staff.GetAsync("/Accounts")).Headers.Location?.OriginalString.Contains("/Account/AccessDenied") == true, "Staff is denied account administration GET");
        check((await staff.PostAsync("/Accounts/Create", new FormUrlEncodedContent(Form("hack")))).Headers.Location?.OriginalString.Contains("/Account/AccessDenied") == true, "Staff is denied account administration POST at backend");
        int staffId, adminId;
        await using (var db = new ApplicationDbContext(options))
        {
            staffId = (await db.UserAccounts.SingleAsync(u => u.Username == "qa-staff")).Id;
            adminId = (await db.UserAccounts.SingleAsync(u => u.Username == AdminName)).Id;
        }
        var promote = await EditForm(staffId); promote.Role = "Admin"; await Save(promote);
        check(IsLogin(await staff.GetAsync("/Accounts")), "Role change revokes existing session before granting new privileges");
        await Login(staff, "qa-staff", Password);
        check((await staff.GetAsync("/Accounts")).StatusCode == HttpStatusCode.OK, "New administrator role applies after a fresh login");
        var demote = await EditForm(staffId); demote.Role = "Staff"; await Save(demote);
        check(IsLogin(await staff.GetAsync("/Accounts")), "Demotion revokes administrator privileges on the next request");
        await Login(staff, "qa-staff", Password);
        var edit = await EditForm(staffId); edit.IsActive = false;
        check(await Save(edit) == null && IsLogin(await staff.GetAsync("/Plants")), "Locking an account revokes its session on the next request");
        check((await Login(staff, "qa-staff", Password)).StatusCode == HttpStatusCode.OK, "Locked account cannot log in");
        edit = await EditForm(staffId); edit.IsActive = true; await Save(edit);
        await Login(staff, "qa-staff", Password);
        edit = await EditForm(staffId); edit.NewPassword = "Reset-QA-Password-789"; await Save(edit);
        check(IsLogin(await staff.GetAsync("/Plants")), "Administrator password reset revokes existing session");
        check((await Login(staff, "qa-staff", "Reset-QA-Password-789")).Headers.Location?.OriginalString == "/Account/ChangePassword", "Reset password requires a new personal password");
        var staleEdit = await EditForm(staffId);
        edit = await EditForm(staffId); edit.DisplayName = "Changed name"; await Save(edit);
        check(await Save(staleEdit) != null, "Stale account edit is rejected");
        var own = await EditForm(adminId); own.IsActive = false;
        check(await Save(own) != null, "Administrator cannot lock their own account");
        own = await EditForm(adminId); own.Role = "Staff";
        check(await Save(own) != null, "Administrator cannot demote their own account");
        // Failed-password lockout is exercised through the same service as HTTP, without consuming the IP rate budget.
        await using (var db = new ApplicationDbContext(options))
        {
            var service = new UserAccountService(db);
            for (var i = 0; i < 5; i++) check(await service.LoginAsync("qa-race", "Wrong-password-123") == null, "Failed login remains rejected " + i);
            var locked = await db.UserAccounts.SingleAsync(u => u.Username == "qa-race");
            check(locked.LockoutUntil > DateTime.UtcNow && await service.LoginAsync("qa-race", Initial) == null, "Five wrong passwords lock login for 15 minutes");
            locked.LockoutUntil = DateTime.UtcNow.AddMinutes(-1); await db.SaveChangesAsync();
            check(await service.LoginAsync("qa-race", Initial) != null, "Expired lockout allows correct password");
            check(await service.LoginAsync("missing-user", Initial) == null, "Unknown username fails safely");
        }
        check((await admin.PostAsync("/Account/Logout", new FormUrlEncodedContent(new Dictionary<string,string>()))).StatusCode == HttpStatusCode.BadRequest, "Logout requires CSRF protection");
        check((await Post(admin, "/Account/Logout", new(), "/")).StatusCode == HttpStatusCode.Redirect && IsLogin(await admin.GetAsync("/Plants")), "Logout clears access");
        await Login(admin, AdminName, Password);

        async Task<AccountForm> EditForm(int id)
        {
            await using var db = new ApplicationDbContext(options);
            var u = await db.UserAccounts.FindAsync(id);
            return new() { Id = id, Revision = u!.Revision, Username = u.Username!, DisplayName = u.DisplayName!, Role = u.Role!, IsActive = u.IsActive == true };
        }
        async Task<string?> Save(AccountForm form)
        { await using var db = new ApplicationDbContext(options); return await new UserAccountService(db).SaveAsync(form, adminId); }
    }
    private static async Task<(int Code, string Text)> Bootstrap(string command, string connection, string? password = null)
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "Farm-Management.sln"))) root = root.Parent;
        var configuration = new DirectoryInfo(AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)).Parent!.Name;
        var start = new ProcessStartInfo("dotnet") { WorkingDirectory = Path.Combine(root!.FullName, "Farm-Management"), UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
        var appDll = Path.Combine(start.WorkingDirectory, "bin", configuration, "net8.0", "Farm-Management.dll");
        if (!File.Exists(appDll)) appDll = Path.Combine(start.WorkingDirectory, "bin", "Debug", "net8.0", "Farm-Management.dll");
        start.ArgumentList.Add(appDll); start.ArgumentList.Add(command);
        start.Environment["ConnectionStrings__DefaultConnection"] = connection;
        start.Environment["FARM_ADMIN_USERNAME"] = AdminName;
        if (password != null) start.Environment["FARM_ADMIN_PASSWORD"] = password;
        using var process = Process.Start(start)!;
        var output = process.StandardOutput.ReadToEndAsync(); var error = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        return (process.ExitCode, await output + await error);
    }
    public static async Task RateLimit(Uri address, Action<bool,string> check)
    {
        using var client = Client(address);
        var limited = false;
        for (var i = 0; i < 21; i++)
        {
            var response = await Login(client, "not-a-real-account", "Unknown-password-123");
            if ((int)response.StatusCode == 429) { limited = true; break; }
        }
        check(limited, "Repeated login requests are rate limited by IP");
    }
    private static Dictionary<string,string> Form(string name) => new() { ["Username"] = name, ["DisplayName"] = "Tài khoản kiểm thử", ["Role"] = "Staff", ["IsActive"] = "true", ["NewPassword"] = Initial };
    private static bool IsLogin(HttpResponseMessage r) => r.StatusCode == HttpStatusCode.Redirect && r.Headers.Location?.OriginalString.Contains("/Account/Login") == true;
    private static HttpClient Client(Uri baseAddress) => new(new HttpClientHandler { CookieContainer = new(), AllowAutoRedirect = false }) { BaseAddress = baseAddress };
    private static Task<HttpResponseMessage> Login(HttpClient c, string user, string password, string returnUrl = "/") => Post(c, "/Account/Login", new() { ["Username"] = user, ["Password"] = password, ["ReturnUrl"] = returnUrl });
    private static Task<HttpResponseMessage> Change(HttpClient c, string old, string next) => Post(c, "/Account/ChangePassword", new() { ["CurrentPassword"] = old, ["NewPassword"] = next, ["ConfirmPassword"] = next });
    private static async Task<HttpResponseMessage> Post(HttpClient c, string path, Dictionary<string,string> values, string? tokenPath = null)
    {
        var html = await c.GetStringAsync(tokenPath ?? path);
        values["__RequestVerificationToken"] = WebUtility.HtmlDecode(Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
        return await c.PostAsync(path, new FormUrlEncodedContent(values));
    }
}

