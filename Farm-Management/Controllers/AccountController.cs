using System.Security.Claims;
using Farm_Management.Models.ViewModels;
using Farm_Management.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Farm_Management.Controllers;

[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class AccountController(UserAccountService accounts) : Controller
{
    [AllowAnonymous, HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Home");
        return View(new LoginForm { ReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : null });
    }
    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken, EnableRateLimiting("login")]
    public async Task<IActionResult> Login(LoginForm form)
    {
        if (ModelState.IsValid)
        {
            var user = await accounts.LoginAsync(form.Username, form.Password);
            if (user != null)
            {
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, UserAccountService.Principal(user),
                    new AuthenticationProperties { IsPersistent = false });
                if (user.MustChangePassword == true) return RedirectToAction(nameof(ChangePassword));
                return LocalRedirect(Url.IsLocalUrl(form.ReturnUrl) ? form.ReturnUrl! : "/");
            }
            ModelState.AddModelError("", "Không đăng nhập được. Kiểm tra tên và mật khẩu; nếu nhập sai nhiều lần, chờ 15 phút hoặc liên hệ quản trị.");
        }
        ModelState.Remove(nameof(form.Password)); form.Password = "";
        return View(form);
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }
    [HttpGet] public IActionResult ChangePassword() => View(new ChangePasswordForm());
    [HttpPost, ValidateAntiForgeryToken, EnableRateLimiting("login")]
    public async Task<IActionResult> ChangePassword(ChangePasswordForm form)
    {
        if (ModelState.IsValid)
        {
            var error = await accounts.ChangePasswordAsync(int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!), form);
            if (error == null)
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                TempData["Success"] = "Đã đổi mật khẩu. Đăng nhập lại bằng mật khẩu mới.";
                return RedirectToAction(nameof(Login));
            }
            ModelState.AddModelError("", error);
        }
        ModelState.Remove(nameof(form.CurrentPassword)); ModelState.Remove(nameof(form.NewPassword)); ModelState.Remove(nameof(form.ConfirmPassword));
        return View(new ChangePasswordForm());
    }
    [HttpGet] public IActionResult AccessDenied() { Response.StatusCode = 403; return View(); }
}
