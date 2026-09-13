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
        ViewBag.Users = await db.AppUsers.OrderBy(u => u.Id).ToListAsync();
        ViewBag.Registrations = await db.Registrations.AsNoTracking()
            .OrderByDescending(r => r.Id).Take(50).ToListAsync();
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
            return RedirectToAction(nameof(Index));
        }

        if (await db.AppUsers.AnyAsync(u => u.Phone == model.Phone))
        {
            TempData["FlashError"] = "এই ফোন নম্বরে ইতিমধ্যে একটি অ্যাকাউন্ট আছে।";
            return RedirectToAction(nameof(Index));
        }

        db.AppUsers.Add(new AppUser
        {
            Phone = model.Phone!,
            PasswordHash = PasswordHasher.Hash(model.Password!),
            MustChangePassword = true, // user sets their own password at first login
        });
        await db.SaveChangesAsync();

        TempData["Flash"] = $"{model.Phone} নম্বরে নতুন অ্যাকাউন্ট তৈরি হয়েছে। প্রথম লগইনে সে পাসওয়ার্ড বদলাতে বাধ্য হবে।";
        return RedirectToAction(nameof(Index));
    }

    // POST /Admin/ResetPassword
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordInputModel model)
    {
        if (string.IsNullOrWhiteSpace(model.NewPassword))
        {
            TempData["FlashError"] = "রিসেট করতে একটি নতুন পাসওয়ার্ড দিন।";
            return RedirectToAction(nameof(Index));
        }

        var user = await db.AppUsers.FirstOrDefaultAsync(u => u.Id == model.Id);
        if (user is null) return NotFound();

        user.PasswordHash = PasswordHasher.Hash(model.NewPassword);
        user.MustChangePassword = true;
        await db.SaveChangesAsync();

        TempData["Flash"] = $"{user.Phone} এর পাসওয়ার্ড রিসেট হয়েছে। পরের লগইনে সে নতুন পাসওয়ার্ড সেট করতে বাধ্য হবে।";
        return RedirectToAction(nameof(Index));
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
        return RedirectToAction(nameof(Index));
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
