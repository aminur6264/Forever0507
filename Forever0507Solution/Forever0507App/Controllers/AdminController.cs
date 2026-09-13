using Forever0507App.Data;
using Forever0507App.Helpers;
using Forever0507App.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Forever0507App.Controllers;

[Authorize(Roles = AuthConstants.AdminRole)]
public class AdminController(AlumniDbContext db) : Controller
{
    public const string PaymentVerified = "নিশ্চিত";
    public const string PaymentPending = "যাচাই অপেক্ষমান";

    // GET /Admin
    public async Task<IActionResult> Index()
    {
        ViewBag.TotalRegistrations = await db.Registrations.CountAsync();
        ViewBag.VerifiedCount = await db.Registrations.CountAsync(r => r.PaymentStatus == PaymentVerified);
        ViewBag.TotalPayable = await db.Registrations.SumAsync(r => (decimal?)r.PayableAmount) ?? 0;
        ViewBag.UserCount = await db.AppUsers.CountAsync();
        ViewBag.Registrations = await db.Registrations.AsNoTracking()
            .OrderByDescending(r => r.Id).Take(50).ToListAsync();
        return View();
    }

    // GET /Admin/Users — user management lives on its own page.
    public async Task<IActionResult> Users()
    {
        ViewBag.Users = await db.AppUsers.OrderBy(u => u.Id).ToListAsync();
        return View();
    }

    // POST /Admin/CreateUser
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateUser(CreateUserInputModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["FlashError"] = "ফোন নম্বর বা পাসওয়ার্ড ঠিক নয় — আবার চেষ্টা করুন।";
            return RedirectToAction(nameof(Users));
        }

        if (await db.AppUsers.AnyAsync(u => u.Phone == model.Phone))
        {
            TempData["FlashError"] = "এই ফোন নম্বরে ইতিমধ্যে একটি অ্যাকাউন্ট আছে।";
            return RedirectToAction(nameof(Users));
        }

        db.AppUsers.Add(new AppUser
        {
            Phone = model.Phone!,
            PasswordHash = PasswordHasher.Hash(model.Password!),
            MustChangePassword = true, // user sets their own password at first login
        });
        await db.SaveChangesAsync();

        TempData["Flash"] = $"{model.Phone} নম্বরে নতুন অ্যাকাউন্ট তৈরি হয়েছে। প্রথম লগইনে সে পাসওয়ার্ড বদলাতে বাধ্য হবে।";
        return RedirectToAction(nameof(Users));
    }

    // POST /Admin/ResetPassword/5 — one click: the username (phone) itself becomes the new password.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(int id)
    {
        var user = await db.AppUsers.FirstOrDefaultAsync(u => u.Id == id);
        if (user is null) return NotFound();

        user.PasswordHash = PasswordHasher.Hash(user.Phone);
        user.MustChangePassword = true;
        await db.SaveChangesAsync();

        TempData["Flash"] = $"{user.Phone} এর পাসওয়ার্ড রিসেট হয়েছে। নতুন পাসওয়ার্ড তার ইউজারনেম (ফোন নম্বর)ই — পরের লগইনে সে নতুন পাসওয়ার্ড সেট করতে বাধ্য হবে।";
        return RedirectToAction(nameof(Users));
    }

    // POST /Admin/DeleteUser
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await db.AppUsers.FirstOrDefaultAsync(u => u.Id == id);
        if (user is not null)
        {
            db.AppUsers.Remove(user);
            await db.SaveChangesAsync();
            TempData["Flash"] = $"{user.Phone} অ্যাকাউন্ট মুছে ফেলা হয়েছে।";
        }
        return RedirectToAction(nameof(Users));
    }

    // POST /Admin/TogglePayment/REG-2026-0001
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TogglePayment(string id)
    {
        var registration = await db.Registrations.FirstOrDefaultAsync(r => r.RegistrationNo == id);
        if (registration is not null)
        {
            registration.PaymentStatus = registration.PaymentStatus == PaymentVerified ? PaymentPending : PaymentVerified;
            await db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }
}
