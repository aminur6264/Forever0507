using Forever0507App.Data;
using Forever0507App.Helpers;
using Forever0507App.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Forever0507App.Controllers;

[Authorize(Roles = AuthConstants.AdminRole)]
public class AdminController(AlumniDbContext db, EventOptionsHolder eventHolder) : Controller
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

    // POST /Admin/ApproveRegistration/5 — one-way. Approval also credits the payable amount
    // to the payment-method account the money was sent to, and gives the registrant a system
    // account: username = their phone, initial password = the phone itself (changed at first login).
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

            var account = await db.PaymentMethods.FirstOrDefaultAsync(p =>
                p.MfsName == registration.PaymentMedium && p.AccountNumber == registration.ToAccount);
            var creditNote = account is not null
                ? $"৳{registration.PayableAmount:N0} টাকা {account.MfsName} ({account.AccountNumber}) অ্যাকাউন্টে যোগ হয়েছে।"
                : $"{registration.PaymentMedium} এর {registration.ToAccount} নম্বরের সাথে মিলে যাওয়া কোনো পেমেন্ট অ্যাকাউন্ট নেই, তাই ব্যালেন্স যোগ হয়নি।";

            if (account is not null)
                account.Balance += registration.PayableAmount;

            // Registrant's login: username = phone, password = phone until they set their own.
            var phone = registration.Phone;
            var userNote = await db.AppUsers.AnyAsync(u => u.Phone == phone)
                ? "এই নম্বরে ব্যবহারকারী অ্যাকাউন্ট আগেই আছে, নতুন করে তৈরি হয়নি।"
                : null;
            if (userNote is null)
            {
                db.AppUsers.Add(new AppUser
                {
                    Phone = phone,
                    PasswordHash = PasswordHasher.Hash(phone),
                    MustChangePassword = true, // sets their own password at first login
                });
                userNote = $"{phone} নম্বরে ব্যবহারকারী অ্যাকাউন্ট তৈরি হয়েছে — প্রথম লগইনে পাসওয়ার্ড বদলাতে বাধ্য হবে।";
            }

            await db.SaveChangesAsync();
            TempData["Flash"] = $"{registration.RegistrationNo} অনুমোদিত — {creditNote} {userNote}";
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

    // GET /Admin/PaymentMethods — insert/update form on top, list below. ?edit=N prefills the form.
    public async Task<IActionResult> PaymentMethods(int? edit)
    {
        ViewBag.PaymentMethods = await db.PaymentMethods.OrderBy(p => p.Id).ToListAsync();
        ViewBag.Mediums = await db.PaymentMediumOptions.OrderBy(m => m.Id).ToListAsync();
        if (edit is int id)
            ViewBag.Editing = await db.PaymentMethods.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
        return View();
    }

    // POST /Admin/SavePaymentMethod — insert when Id is 0, update otherwise. Balance starts at 0 and is never editable.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SavePaymentMethod(PaymentMethodInputModel model)
    {
        if (model.Type is not (PaymentMethod.Personal or PaymentMethod.Merchant))
            ModelState.AddModelError(nameof(model.Type), "সঠিক টাইপ নির্বাচন করুন।");
        if (!string.IsNullOrEmpty(model.MfsName)
            && !await db.PaymentMediumOptions.AnyAsync(m => m.Name == model.MfsName))
            ModelState.AddModelError(nameof(model.MfsName), "সঠিক এমএফএস নাম নির্বাচন করুন।");

        if (!ModelState.IsValid)
        {
            TempData["FlashError"] = "ফর্মের তথ্য ঠিক নয় — আবার চেষ্টা করুন।";
            return RedirectToAction(nameof(PaymentMethods));
        }

        if (model.Id == 0)
        {
            db.PaymentMethods.Add(new PaymentMethod
            {
                AccountName = model.AccountName!.Trim(),
                AccountNumber = model.AccountNumber!.Trim(),
                Type = model.Type!,
                MfsName = model.MfsName!,
                Balance = 0,
                IsActive = true,
            });
            await db.SaveChangesAsync();
            TempData["Flash"] = $"{model.MfsName} অ্যাকাউন্ট যোগ হয়েছে।";
        }
        else
        {
            var paymentMethod = await db.PaymentMethods.FirstOrDefaultAsync(p => p.Id == model.Id);
            if (paymentMethod is null) return NotFound();

            paymentMethod.AccountName = model.AccountName!.Trim();
            paymentMethod.AccountNumber = model.AccountNumber!.Trim();
            paymentMethod.Type = model.Type!;
            paymentMethod.MfsName = model.MfsName!;
            await db.SaveChangesAsync(); // Balance deliberately untouched on update
            TempData["Flash"] = $"{paymentMethod.MfsName} অ্যাকাউন্ট আপডেট হয়েছে।";
        }
        return RedirectToAction(nameof(PaymentMethods));
    }

    // POST /Admin/TogglePaymentMethodStatus/5 — flip a payment method between active and inactive.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TogglePaymentMethodStatus(int id)
    {
        var paymentMethod = await db.PaymentMethods.FirstOrDefaultAsync(p => p.Id == id);
        if (paymentMethod is not null)
        {
            paymentMethod.IsActive = !paymentMethod.IsActive;
            await db.SaveChangesAsync();
            TempData["Flash"] = paymentMethod.IsActive
                ? $"{paymentMethod.MfsName} অ্যাকাউন্ট চালু করা হয়েছে।"
                : $"{paymentMethod.MfsName} অ্যাকাউন্ট বন্ধ করা হয়েছে।";
        }
        return RedirectToAction(nameof(PaymentMethods));
    }

    // GET /Admin/Event — settings are read-only until আপডেট is clicked (?edit=true). No row → add form.
    public async Task<IActionResult> Event(bool edit = false)
    {
        ViewBag.Settings = await db.EventSettings.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == EventSettings.SingleRowId);
        ViewBag.Editing = edit;
        return View();
    }

    // POST /Admin/SaveEvent — creates the single row when missing, updates it otherwise,
    // and refreshes the live EventOptions so the whole site reflects the change immediately.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveEvent(EventSettingsInputModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["FlashError"] = "ফর্মের তথ্য ঠিক নয় — আবার চেষ্টা করুন।";
            return RedirectToAction(nameof(Event));
        }

        var settings = await db.EventSettings.FirstOrDefaultAsync(s => s.Id == EventSettings.SingleRowId);
        var isNew = settings is null;
        if (isNew)
        {
            settings = new EventSettings { Id = EventSettings.SingleRowId };
            db.EventSettings.Add(settings);
        }
        settings!.UpdateFrom(model);
        await db.SaveChangesAsync();

        eventHolder.Current = settings.ToOptions();
        TempData["Flash"] = isNew ? "ইভেন্ট তথ্য তৈরি হয়েছে।" : "ইভেন্ট তথ্য আপডেট হয়েছে।";
        return RedirectToAction(nameof(Event));
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
