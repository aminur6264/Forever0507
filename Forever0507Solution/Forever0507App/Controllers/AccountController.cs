using System.Security.Claims;
using Forever0507App.Data;
using Forever0507App.Helpers;
using Forever0507App.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Forever0507App.Controllers;

public class AccountController(AlumniDbContext db) : Controller
{
    // GET /Account/Login
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectAfterLogin();
        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginInputModel());
    }

    // POST /Account/Login
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginInputModel model, string? returnUrl = null)
    {
        if (!ModelState.IsValid) return View(model);

        // The static admin may sign in through either form on the page.
        if (model.Phone == AuthConstants.AdminLoginName && model.Password == AuthConstants.AdminPassword)
        {
            await SignInAdminAsync();
            return RedirectToAction("Index", "Admin");
        }

        var user = await db.AppUsers.FirstOrDefaultAsync(u => u.Phone == model.Phone);
        if (user is null || !PasswordHasher.Verify(model.Password!, user.PasswordHash))
        {
            ModelState.AddModelError(string.Empty, "ফোন নম্বর বা পাসওয়ার্ড সঠিক নয়।");
            return View(model);
        }

        await SignInRegularAsync(user);
        if (user.MustChangePassword) return RedirectToAction(nameof(ChangePassword));
        return LocalRedirect(returnUrl ?? "/");
    }

    // POST /Account/AdminLogin — the static system admin signs in with the fixed credentials in AuthConstants.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AdminLogin(AdminLoginInputModel model)
    {
        if (model.Username?.Trim() != AuthConstants.AdminLoginName || model.Password != AuthConstants.AdminPassword)
        {
            TempData["FlashError"] = "অ্যাডমিন ইউজারনেম বা পাসওয়ার্ড সঠিক নয়।";
            return RedirectToAction(nameof(Login));
        }

        await SignInAdminAsync();
        return RedirectToAction("Index", "Admin");
    }

    // GET /Account/ChangePassword — required on first login / after admin reset; voluntary otherwise.
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> ChangePassword()
    {
        if (User.IsInRole(AuthConstants.AdminRole)) return RedirectToAction("Index", "Admin");
        ViewBag.MustChange = User.HasClaim(AuthConstants.MustChangePasswordClaim, "true");
        return View(new ChangePasswordInputModel());
    }

    // POST /Account/ChangePassword
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordInputModel model)
    {
        if (User.IsInRole(AuthConstants.AdminRole)) return RedirectToAction("Index", "Admin");

        var phone = User.Identity!.Name!;
        var user = await db.AppUsers.FirstOrDefaultAsync(u => u.Phone == phone);
        if (user is null)
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }

        if (!ModelState.IsValid)
        {
            ViewBag.MustChange = User.HasClaim(AuthConstants.MustChangePasswordClaim, "true");
            return View(model);
        }

        if (!PasswordHasher.Verify(model.CurrentPassword!, user.PasswordHash))
        {
            ModelState.AddModelError(nameof(model.CurrentPassword), "বর্তমান পাসওয়ার্ড সঠিক নয়।");
            ViewBag.MustChange = User.HasClaim(AuthConstants.MustChangePasswordClaim, "true");
            return View(model);
        }

        user.PasswordHash = PasswordHasher.Hash(model.NewPassword!);
        user.MustChangePassword = false;
        await db.SaveChangesAsync();

        await SignInRegularAsync(user); // re-issue cookie without the must-change claim
        TempData["Flash"] = "পাসওয়ার্ড সফলভাবে পরিবর্তন হয়েছে।";
        return RedirectToAction(nameof(MyProfile));
    }

    // GET /Account/MyProfile
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> MyProfile()
    {
        var phone = User.Identity!.Name!;
        ViewBag.IsAdmin = User.IsInRole(AuthConstants.AdminRole);
        ViewBag.Registrations = await db.Registrations.AsNoTracking()
            .Where(r => r.Phone == phone)
            .OrderByDescending(r => r.Id)
            .ToListAsync();
        return View();
    }

    // POST /Account/Logout
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    // GET /Account/AccessDenied
    [HttpGet]
    public IActionResult AccessDenied() => View();

    private async Task SignInAdminAsync()
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, AuthConstants.AdminLoginName),
            new(ClaimTypes.Name, AuthConstants.AdminDisplayName),
            new(ClaimTypes.Role, AuthConstants.AdminRole),
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)));
    }

    private async Task SignInRegularAsync(AppUser user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Phone),
            new(ClaimTypes.Role, AuthConstants.RegularRole),
        };
        if (user.MustChangePassword)
            claims.Add(new Claim(AuthConstants.MustChangePasswordClaim, "true"));

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)));
    }

    private IActionResult RedirectAfterLogin()
        => User.IsInRole(AuthConstants.AdminRole)
            ? RedirectToAction("Index", "Admin")
            : RedirectToAction(nameof(MyProfile));
}
