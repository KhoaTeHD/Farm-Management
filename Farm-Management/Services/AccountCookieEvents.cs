using System.Security.Claims;
using Farm_Management.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace Farm_Management.Services;

public class AccountCookieEvents(ApplicationDbContext db) : CookieAuthenticationEvents
{
    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        var validId = int.TryParse(context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id);
        var user = validId ? await db.UserAccounts.AsNoTracking().SingleOrDefaultAsync(u => u.Id == id) : null;
        if (user == null || !UserAccountService.Usable(user)
            || user.SecurityStamp != context.Principal?.FindFirstValue("security_stamp")
            || user.Role != context.Principal?.FindFirstValue(ClaimTypes.Role))
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
    }
}
