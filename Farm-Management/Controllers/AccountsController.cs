using System.Security.Claims;
using Farm_Management.Data;
using Farm_Management.Models.ViewModels;
using Farm_Management.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Farm_Management.Controllers;

[Authorize(Roles = "Admin")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class AccountsController(ApplicationDbContext db, UserAccountService accounts) : Controller
{
    public async Task<IActionResult> Index() => View(await db.UserAccounts.AsNoTracking().OrderBy(u => u.Username)
        .Select(u => new AccountForm { Id = u.Id, Username = u.Username ?? "(thiếu tên)", DisplayName = u.DisplayName ?? "(thiếu họ tên)", Role = u.Role ?? "", IsActive = u.IsActive == true, Revision = u.Revision }).ToListAsync());
    [HttpGet] public IActionResult Create() => View("Edit", new AccountForm());
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var u = await db.UserAccounts.AsNoTracking().SingleOrDefaultAsync(u => u.Id == id);
        if (u == null) return NotFound();
        return View(new AccountForm { Id = u.Id, Username = u.Username ?? "", DisplayName = u.DisplayName ?? "", Role = u.Role ?? "", IsActive = u.IsActive == true, Revision = u.Revision });
    }
    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Create(AccountForm form)
    {
        if (form.Id != 0) return Task.FromResult<IActionResult>(BadRequest());
        return Save(form);
    }
    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Edit(int id, AccountForm form) => id != form.Id || id <= 0 ? Task.FromResult<IActionResult>(BadRequest()) : Save(form);
    private async Task<IActionResult> Save(AccountForm form)
    {
        if (ModelState.IsValid)
        {
            var error = await accounts.SaveAsync(form, int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!));
            if (error == null) { TempData["Success"] = "Đã lưu tài khoản. Phiên đăng nhập cũ của tài khoản này đã hết hiệu lực."; return RedirectToAction(nameof(Index)); }
            ModelState.AddModelError("", error);
        }
        ModelState.Remove(nameof(form.NewPassword)); form.NewPassword = null;
        return View("Edit", form);
    }
}
