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
        return View();
    }

    // GET /Admin/Registrations — every registration with approve/reject controls.
    public async Task<IActionResult> Registrations()
    {
        ViewBag.Registrations = await db.Registrations.AsNoTracking()
            .OrderByDescending(r => r.Id).ToListAsync();
        return View();
    }

    // POST /Admin/ApproveRegistration/5 — one-way: only an undecided (null) registration can be approved.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveRegistration(int id)
    {
        var registration = await db.Registrations.FirstOrDefaultAsync(r => r.Id == id);
        if (registration is not null && registration.ApprovalStatus is null)
        {
            registration.ApprovalStatus = Registration.ApprovalApproved;
            registration.ApprovalBy = User.Identity!.Name; // the deciding admin's username (phone)
            registration.ApprovalAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            TempData["Flash"] = $"{registration.RegistrationNo} অনুমোদিত হয়েছে — কার্ড এখন দেখা যাবে।";
        }
        return RedirectToAction(nameof(Registrations));
    }

    // POST /Admin/RejectRegistration/5 — one-way: only an undecided (null) registration can be rejected.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectRegistration(int id)
    {
        var registration = await db.Registrations.FirstOrDefaultAsync(r => r.Id == id);
        if (registration is not null && registration.ApprovalStatus is null)
        {
            registration.ApprovalStatus = Registration.ApprovalRejected;
            registration.ApprovalBy = User.Identity!.Name; // the deciding admin's username (phone)
            registration.ApprovalAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            TempData["Flash"] = $"{registration.RegistrationNo} প্রত্যাখ্যাত হয়েছে — কার্ড আর দেখা যাবে না।";
        }
        return RedirectToAction(nameof(Registrations));
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
            // Regular users set their own password at first login; admins already chose it here,
            // and the change-password page redirects admins away — a pending flag would loop.
            MustChangePassword = !model.IsAdmin,
            IsAdmin = model.IsAdmin,
        });
        await db.SaveChangesAsync();

        TempData["Flash"] = model.IsAdmin
            ? $"{model.Phone} নম্বরে নতুন অ্যাডমিন অ্যাকাউন্ট তৈরি হয়েছে।"
            : $"{model.Phone} নম্বরে নতুন অ্যাকাউন্ট তৈরি হয়েছে। প্রথম লগইনে সে পাসওয়ার্ড বদলাতে বাধ্য হবে।";
        return RedirectToAction(nameof(Users));
    }

    // POST /Admin/ToggleRole/5 — flip a user between regular and admin.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleRole(int id)
    {
        var user = await db.AppUsers.FirstOrDefaultAsync(u => u.Id == id);
        if (user is not null)
        {
            user.IsAdmin = !user.IsAdmin;
            if (user.IsAdmin) user.MustChangePassword = false; // same loop-avoidance as CreateUser
            await db.SaveChangesAsync();

            TempData["Flash"] = user.IsAdmin
                ? $"{user.Phone} এখন অ্যাডমিন।"
                : $"{user.Phone} এখন রেগুলার ব্যবহারকারী।";
        }
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
