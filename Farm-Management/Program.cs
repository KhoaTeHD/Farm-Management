using Farm_Management.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using System.Threading.RateLimiting;
using Farm_Management.Services;

var bootstrapCommand = args.Contains("--create-admin") || args.Contains("--check-admin");
var runtimeArgs = args.Where(arg => arg != "--create-admin" && arg != "--check-admin").ToArray();
var builder = WebApplication.CreateBuilder(runtimeArgs);
if (bootstrapCommand) builder.Logging.ClearProviders();

// Local credentials stay out of source control. Environment/CLI values still take priority.
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables()
    .AddCommandLine(runtimeArgs);

// Add services to the container.
builder.Services.AddControllersWithViews(options => options.Filters.Add<Farm_Management.Services.RecordChangeFilter>());
builder.Services.AddScoped<Farm_Management.Services.PayrollService>();
builder.Services.AddScoped<Farm_Management.Services.CropSeasonService>();
builder.Services.AddScoped<UserAccountService>();
builder.Services.AddScoped<AccountCookieEvents>();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
{
    options.Cookie.Name = "FarmManagement.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
    options.LoginPath = "/Account/Login"; options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8); options.SlidingExpiration = false;
    options.EventsType = typeof(AccountCookieEvents);
});
builder.Services.AddAuthorization(options => options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;
    options.AddPolicy("login", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions
        { PermitLimit = 20, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});

// Register DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Explicit, one-time console command. Credentials never appear in command-line arguments or output.
if (bootstrapCommand)
{
    using var scope = app.Services.CreateScope();
    try
    {
        var service = scope.ServiceProvider.GetRequiredService<UserAccountService>();
        if (args.Contains("--check-admin"))
        {
            Environment.ExitCode = await service.InitialAdminStatusAsync();
            Console.WriteLine(Environment.ExitCode switch { 10 => "An active administrator already exists; skip creation and use the existing login.", 14 => "Accounts exist but no usable administrator is active; administrator recovery is required.", _ => "No accounts exist. Create the first administrator." });
        }
        else
        {
            await service.CreateInitialAdminAsync(Environment.GetEnvironmentVariable("FARM_ADMIN_USERNAME") ?? "admin", Environment.GetEnvironmentVariable("FARM_ADMIN_PASSWORD") ?? "");
            Console.WriteLine("Administrator created. Change the temporary password at first login.");
        }
    }
    catch (AdminBootstrapException error) { Environment.ExitCode = error.ExitCode; Console.Error.WriteLine(error.Message); }
    catch (Exception)
    {
        Environment.ExitCode = 12;
        Console.Error.WriteLine("Cannot check/create administrator. Verify appsettings.Local.json, SQL connectivity/permissions and database-update.sql migrations. No account was reset. Credentials are not printed.");
    }
    return;
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStatusCodePagesWithReExecute("/Home/Status", "?code={0}");
app.UseRouting();
app.UseAuthentication();
app.Use(async (context, next) =>
{
    context.Response.Headers["Cache-Control"] = "no-store";
    var path = context.Request.Path;
    if (context.User.Identity?.IsAuthenticated == true && context.User.FindFirst("must_change_password")?.Value == "true"
        && !path.StartsWithSegments("/Account") && !path.StartsWithSegments("/css") && !path.StartsWithSegments("/js") && !path.StartsWithSegments("/lib"))
    { context.Response.Redirect("/Account/ChangePassword"); return; }
    if (path.StartsWithSegments("/uploads") && context.User.Identity?.IsAuthenticated != true)
    { await context.ChallengeAsync(); return; }
    await next();
});
app.UseStaticFiles();
app.UseRateLimiter();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
