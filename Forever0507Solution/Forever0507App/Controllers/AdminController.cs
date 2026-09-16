using Forever0507App.Data;
using Forever0507App.Helpers;
using Forever0507App.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Forever0507App.Controllers;

[Authorize(Roles = AuthConstants.AdminRole)]
public class AdminController(AlumniDbContext db, EventOptionsHolder eventHolder, IWebHostEnvironment env, Services.EmailSender emailSender) : Controller
{
    public const string PaymentVerified = "নিশ্চিত";
    public const string PaymentPending = "যাচাই অপেক্ষমান";

    // GET /Admin
    public async Task<IActionResult> Index()
    {

        ViewBag.TotalRegistrations = await db.Registrations.CountAsync(r => r.ApprovalStatus == Registration.ApprovalApproved);
        ViewBag.VerifiedCount = await db.Registrations.CountAsync(r => r.PaymentStatus == PaymentVerified);
        ViewBag.TotalPayable = await db.PaymentMethods.SumAsync(p => (decimal?)p.Balance) ?? 0;
        ViewBag.UserCount = await db.AppUsers.CountAsync();

        // District / school tallies — total and approved, busiest first.
        ViewBag.DistrictTally = await db.Registrations.AsNoTracking()
            .GroupBy(r => r.District)
            .Select(g => new NameTally
            {
                Name = g.Key,
                Total = g.Count(),
                Approved = g.Count(x => x.ApprovalStatus == Registration.ApprovalApproved),
            })
            .OrderByDescending(x => x.Total).ThenBy(x => x.Name)
            .ToListAsync();
        ViewBag.SchoolTally = await db.Registrations.AsNoTracking()
            .GroupBy(r => r.SchoolName)
            .Select(g => new NameTally
            {
                Name = g.Key,
                Total = g.Count(),
                Approved = g.Count(x => x.ApprovalStatus == Registration.ApprovalApproved),
            })
            .OrderByDescending(x => x.Total).ThenBy(x => x.Name)
            .ToListAsync();

        return View();
    }

    // GET /Admin/Registrations?phone=&medium=&from=&to=&status=&approver= — every registration
    // with optional filters (partial phone/account search, medium/status/approver dropdowns)
    // plus approve/reject controls.
    public async Task<IActionResult> Registrations(string? phone, string? medium, string? from, string? to, string? status, string? approver)
    {
        var registrations = db.Registrations.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(phone))
        {
            var term = phone.Trim();
            registrations = registrations.Where(r => r.Phone.Contains(term));
        }
        if (!string.IsNullOrWhiteSpace(medium))
            registrations = registrations.Where(r => r.PaymentMedium == medium);
        if (!string.IsNullOrWhiteSpace(from))
        {
            var term = from.Trim();
            registrations = registrations.Where(r => r.FromAccount.Contains(term));
        }
        if (!string.IsNullOrWhiteSpace(to))
        {
            var term = to.Trim();
            registrations = registrations.Where(r => r.ToAccount.Contains(term));
        }
        if (status == "pending") registrations = registrations.Where(r => r.ApprovalStatus == null);
        else if (status == "approved") registrations = registrations.Where(r => r.ApprovalStatus == Registration.ApprovalApproved);
        else if (status == "rejected") registrations = registrations.Where(r => r.ApprovalStatus == Registration.ApprovalRejected);
        if (!string.IsNullOrWhiteSpace(approver))
            registrations = registrations.Where(r => r.ApprovalBy == approver);

        ViewBag.Registrations = await registrations.OrderByDescending(r => r.Id).ToListAsync();
        // Approver display names, keyed by phone — regular-admin logins store the phone in ApprovalBy,
        // while the static admin stores its display name already (no row here, falls back to it).
        ViewBag.ApproverNames = await db.AppUsers.AsNoTracking()
            .Where(u => u.FullName != "")
            .ToDictionaryAsync(u => u.Phone, u => u.FullName);
        // Filter dropdown choices: mediums and deciders actually present in the registrations.
        ViewBag.Mediums = await db.Registrations.AsNoTracking()
            .Select(r => r.PaymentMedium).Distinct().OrderBy(m => m).ToListAsync();
        ViewBag.Approvers = await db.Registrations.AsNoTracking()
            .Where(r => r.ApprovalBy != null && r.ApprovalBy != "")
            .Select(r => r.ApprovalBy!).Distinct().ToListAsync();

        ViewBag.Phone = phone ?? "";
        ViewBag.Medium = medium ?? "";
        ViewBag.From = from ?? "";
        ViewBag.To = to ?? "";
        ViewBag.Status = status ?? "";
        ViewBag.Approver = approver ?? "";
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
            var createdAccount = !await db.AppUsers.AnyAsync(u => u.Phone == phone);
            var userNote = createdAccount
                ? null
                : "এই নম্বরে ব্যবহারকারী অ্যাকাউন্ট আগেই আছে, নতুন করে তৈরি হয়নি।";
            if (createdAccount)
            {
                db.AppUsers.Add(new AppUser
                {
                    FullName = registration.FullName.Trim(),
                    Phone = phone.Trim(),
                    PasswordHash = PasswordHasher.Hash(phone.Trim()),
                    MustChangePassword = true, // sets their own password at first login
                });
                userNote = $"{phone} নম্বরে ব্যবহারকারী অ্যাকাউন্ট তৈরি হয়েছে — প্রথম লগইনে পাসওয়ার্ড বদলাতে বাধ্য হবে।";
            }

            await db.SaveChangesAsync();
            TempData["Flash"] = $"{registration.RegistrationNo} অনুমোদিত — {creditNote} {userNote}";

            // Credentials only go out with a genuinely new account — an existing user's
            // password is theirs, never resent. Whatever happens, the flash says so.
            if (createdAccount)
            {
                var emailNote = await SendLoginEmailAsync(registration, eventHolder.Current.CommunityName);
                TempData["Flash"] += $" {emailNote}";
            }
        }
        return RedirectToAction(nameof(Registrations));
    }

    // ---------------- Khoroch (খরচ) — committee expense invoices ----------------

    // Username → display-name map for showing who approved/rejected; the static admin's name
    // is stored directly in ActionBy/CreatedBy, so lookups fall back to the raw value.
    private async Task<Dictionary<string, string>> AdminNamesAsync()
        => await db.AppUsers.AsNoTracking()
            .Where(u => u.FullName != "")
            .ToDictionaryAsync(u => u.Phone, u => u.FullName);

    // GET /Admin/Khoroch — expense invoice list with approve/reject (never by the creator),
    // edit-until-decided, and a details view.
    public async Task<IActionResult> Khoroch()
    {
        ViewBag.Khorochs = await db.Khorochs.AsNoTracking()
            .OrderByDescending(k => k.Id).ToListAsync();
        ViewBag.AdminNames = await AdminNamesAsync();
        ViewBag.CurrentUser = User.Identity!.Name;
        return View();
    }

    // GET /Admin/KhorochForm?edit=5 — blank form, or the creator's own pending invoice.
    public async Task<IActionResult> KhorochForm(int? edit)
    {
        if (edit is int id)
        {
            var khoroch = await db.Khorochs.AsNoTracking()
                .Include(k => k.Items)
                .FirstOrDefaultAsync(k => k.Id == id);
            if (khoroch is null) return NotFound();

            // Editing is the creator's privilege, and only until a decision is made.
            if (khoroch.Status is not null || khoroch.CreatedBy != User.Identity!.Name)
            {
                TempData["FlashError"] = "শুধু নির্মাতা নিজেই, অনুমোদন/প্রত্যাখ্যানের আগে সম্পাদনা করতে পারবেন।";
                return RedirectToAction(nameof(AdminController.Khoroch));
            }
            ViewBag.Editing = khoroch;
        }
        return View();
    }

    // POST /Admin/KhorochSave — create or update. The amount is always recomputed from the
    // item rows on the server; the invoice code is stamped once from the identity Id.
    // An optional receipt photo (jpg/png/webp, ≤ 2 MB) is stored under wwwroot/uploads/khoroch
    // and replaces any previous one.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> KhorochSave(KhorochInputModel model, IFormFile? image)
    {
        var backTo = model.Id == 0
            ? RedirectToAction(nameof(KhorochForm))
            : RedirectToAction(nameof(KhorochForm), new { edit = model.Id });

        var items = new List<KhorochItem>();
        foreach (var raw in model.Items)
        {
            // Fully blank rows (left by row-removal) are skipped, not rejected.
            if (string.IsNullOrWhiteSpace(raw.ProductName) && string.IsNullOrWhiteSpace(raw.Quantity) && string.IsNullOrWhiteSpace(raw.UnitPrice))
                continue;

            decimal quantity = 0, unitPrice = 0;
            var ok = decimal.TryParse(raw.Quantity, System.Globalization.CultureInfo.InvariantCulture, out quantity) && quantity > 0
                && decimal.TryParse(raw.UnitPrice, System.Globalization.CultureInfo.InvariantCulture, out unitPrice) && unitPrice >= 0;
            if (!ok || string.IsNullOrWhiteSpace(raw.ProductName))
            {
                TempData["FlashError"] = "প্রতিটি সারিতে পণ্যের নাম, পরিমাণ (০-র বেশি) ও একক দাম দিন।";
                return backTo;
            }
            items.Add(new KhorochItem
            {
                ProductName = raw.ProductName.Trim(),
                Quantity = quantity,
                UnitPrice = unitPrice,
            });
        }

        if (items.Count == 0)
        {
            TempData["FlashError"] = "অন্তত একটি খরচের সারি দিন।";
            return backTo;
        }

        string? imageUrl = null;
        if (image is { Length: > 0 })
        {
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var ext = Path.GetExtension(image.FileName).ToLowerInvariant();
            if (!allowed.Contains(ext) || image.Length > 2 * 1024 * 1024)
            {
                TempData["FlashError"] = "ছবি হতে হবে jpg/png/webp এবং সর্বোচ্চ ২ এমবি।";
                return backTo;
            }

            var folder = Path.Combine(env.WebRootPath, "uploads", "khoroch");
            Directory.CreateDirectory(folder);
            var fileName = $"{Guid.NewGuid():N}{ext}";
            await using var stream = System.IO.File.Create(Path.Combine(folder, fileName));
            await image.CopyToAsync(stream);
            imageUrl = $"/uploads/khoroch/{fileName}";
        }

        var me = User.Identity!.Name;
        var meName = await db.AppUsers.AsNoTracking()
            .Where(u => u.Phone == me)
            .Select(u => u.FullName)
            .FirstOrDefaultAsync();

        Khoroch khoroch;
        var isNew = model.Id == 0;
        if (isNew)
        {
            khoroch = new Khoroch();
            db.Khorochs.Add(khoroch);
        }
        else
        {
            khoroch = await db.Khorochs.Include(k => k.Items)
                .FirstOrDefaultAsync(k => k.Id == model.Id);
            if (khoroch is null) return NotFound();
            if (khoroch.Status is not null || khoroch.CreatedBy != me)
            {
                TempData["FlashError"] = "শুধু নির্মাতা নিজেই, অনুমোদন/প্রত্যাখ্যানের আগে সম্পাদনা করতে পারবেন।";
                return RedirectToAction(nameof(AdminController.Khoroch));
            }
            khoroch.Items.Clear(); // replace the old rows wholesale
        }

        khoroch.Date = model.Date;
        khoroch.Description = (model.Description ?? "").Trim();
        khoroch.Items.AddRange(items);
        khoroch.Amount = items.Sum(i => i.LineTotal);
        if (imageUrl is not null)
        {
            DeleteKhorochImage(khoroch.ImageUrl); // replace the old file only after a new one is in hand
            khoroch.ImageUrl = imageUrl;
        }
        if (isNew)
        {
            khoroch.CreatedBy = me!;
            khoroch.CreatedByName = !string.IsNullOrWhiteSpace(meName) ? meName.Trim() : me!;
        }
        await db.SaveChangesAsync(); // identity Id assigned here for new invoices

        if (isNew)
        {
            // Invoice code: date + creator's name initials + auto-increment (the identity Id) —
            // e.g. KH-20260917-AMI-0014. Same derive-after-save trick as RegistrationNo.
            var source = !string.IsNullOrWhiteSpace(meName) ? meName : me!;
            var initials = new string(source.Where(char.IsLetterOrDigit).Take(3).ToArray()).ToUpperInvariant();
            if (initials.Length == 0) initials = "USR";
            khoroch.InvoiceCode = $"KH-{khoroch.Date:yyyyMMdd}-{initials}-{khoroch.Id:D4}";
            await db.SaveChangesAsync();
        }

        TempData["Flash"] = isNew
            ? $"{khoroch.InvoiceCode} খরচ যোগ হয়েছে — ৳{khoroch.Amount:N0}।"
            : $"{khoroch.InvoiceCode} আপডেট হয়েছে — ৳{khoroch.Amount:N0}।";
        return RedirectToAction(nameof(AdminController.Khoroch));
    }

    // POST /Admin/KhorochApprove/5 — pending only; the creator can never decide their own invoice.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> KhorochApprove(int id)
        => await DecideKhorochAsync(id, Models.Khoroch.Approved, "অনুমোদিত");

    // POST /Admin/KhorochReject/5 — same rules as approval.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> KhorochReject(int id)
        => await DecideKhorochAsync(id, Models.Khoroch.Rejected, "প্রত্যাখ্যাত");

    private async Task<IActionResult> DecideKhorochAsync(int id, string status, string verb)
    {
        var khoroch = await db.Khorochs.FirstOrDefaultAsync(k => k.Id == id);
        if (khoroch is null) return NotFound();

        if (khoroch.CreatedBy == User.Identity!.Name)
            TempData["FlashError"] = "নিজের তৈরি খরচ নিজে অনুমোদন বা প্রত্যাখ্যান করা যাবে না।";
        else if (khoroch.Status is not null)
            TempData["FlashError"] = $"{khoroch.InvoiceCode} আগেই সিদ্ধান্ত হয়ে গেছে।";
        else
        {
            khoroch.Status = status;
            khoroch.ActionBy = User.Identity!.Name;
            khoroch.ActionAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            TempData["Flash"] = $"{khoroch.InvoiceCode} {verb} হয়েছে — ৳{khoroch.Amount:N0}।";
        }
        return RedirectToAction(nameof(AdminController.Khoroch));
    }

    // GET /Admin/KhorochDetails/5 — invoice header + line items.
    public async Task<IActionResult> KhorochDetails(int id)
    {
        var khoroch = await db.Khorochs.AsNoTracking()
            .Include(k => k.Items)
            .FirstOrDefaultAsync(k => k.Id == id);
        if (khoroch is null) return NotFound();
        ViewBag.AdminNames = await AdminNamesAsync();
        return View(khoroch);
    }

    private void DeleteKhorochImage(string? imageUrl)
    {
        if (string.IsNullOrEmpty(imageUrl)) return;
        try
        {
            var full = Path.Combine(env.WebRootPath, imageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (System.IO.File.Exists(full)) System.IO.File.Delete(full);
        }
        catch (IOException)
        {
            // best effort — a stale file in uploads is harmless
        }
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

    // GET /Admin/Users?search=&role= — user list with an optional name/phone search and role filter.
    public async Task<IActionResult> Users(string? search, string? role)
    {
        var users = db.AppUsers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            users = users.Where(u => u.FullName.Contains(term) || u.Phone.Contains(term));
        }

        if (role == "admin") users = users.Where(u => u.IsAdmin);
        else if (role == "regular") users = users.Where(u => !u.IsAdmin);

        ViewBag.Users = await users.OrderBy(u => u.Id).ToListAsync();
        ViewBag.Search = search ?? "";
        ViewBag.Role = role ?? "";
        return View();
    }

    // POST /Admin/CreateUser
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateUser(CreateUserInputModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["FlashError"] = "নাম, ফোন নম্বর বা পাসওয়ার্ড ঠিক নয় — আবার চেষ্টা করুন।";
            return RedirectToAction(nameof(Users));
        }

        if (await db.AppUsers.AnyAsync(u => u.Phone == model.Phone))
        {
            TempData["FlashError"] = "এই ফোন নম্বরে ইতিমধ্যে একটি অ্যাকাউন্ট আছে।";
            return RedirectToAction(nameof(Users));
        }

        db.AppUsers.Add(new AppUser
        {
            FullName = model.FullName!.Trim(),
            Phone = model.Phone!,
            PasswordHash = PasswordHasher.Hash(model.Password!),
            // Regular users set their own password at first login; admins already chose it here,
            // and the change-password page redirects admins away — a pending flag would loop.
            MustChangePassword = !model.IsAdmin,
            IsAdmin = model.IsAdmin,
        });
        await db.SaveChangesAsync();

        TempData["Flash"] = model.IsAdmin
            ? $"{model.FullName!.Trim()} ({model.Phone}) নম্বরে নতুন অ্যাডমিন অ্যাকাউন্ট তৈরি হয়েছে।"
            : $"{model.FullName!.Trim()} ({model.Phone}) নম্বরে নতুন অ্যাকাউন্ট তৈরি হয়েছে। প্রথম লগইনে সে পাসওয়ার্ড বদলাতে বাধ্য হবে।";
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
    // An optional logo (jpg/png/webp, ≤ 2 MB) is stored under wwwroot/uploads/logo and
    // replaces any previous one; no file → the current logo is kept.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveEvent(EventSettingsInputModel model, IFormFile? logo)
    {
        if (!ModelState.IsValid)
        {
            TempData["FlashError"] = "ফর্মের তথ্য ঠিক নয় — আবার চেষ্টা করুন।";
            return RedirectToAction(nameof(Event));
        }

        string? logoUrl = null;
        if (logo is { Length: > 0 })
        {
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var ext = Path.GetExtension(logo.FileName).ToLowerInvariant();
            if (!allowed.Contains(ext) || logo.Length > 2 * 1024 * 1024)
            {
                TempData["FlashError"] = "লোগো হতে হবে jpg/png/webp এবং সর্বোচ্চ ২ এমবি।";
                return RedirectToAction(nameof(Event));
            }

            var folder = Path.Combine(env.WebRootPath, "uploads", "logo");
            Directory.CreateDirectory(folder);
            var fileName = $"{Guid.NewGuid():N}{ext}";
            await using var stream = System.IO.File.Create(Path.Combine(folder, fileName));
            await logo.CopyToAsync(stream);
            logoUrl = $"/uploads/logo/{fileName}";
        }

        var settings = await db.EventSettings.FirstOrDefaultAsync(s => s.Id == EventSettings.SingleRowId);
        var isNew = settings is null;
        if (isNew)
        {
            settings = new EventSettings { Id = EventSettings.SingleRowId };
            db.EventSettings.Add(settings);
        }
        settings!.UpdateFrom(model);
        if (logoUrl is not null)
        {
            DeleteEventLogo(settings.LogoUrl); // replace the old file only after a new one is in hand
            settings.LogoUrl = logoUrl;
        }
        await db.SaveChangesAsync();

        eventHolder.Current = settings.ToOptions();
        TempData["Flash"] = isNew ? "ইভেন্ট তথ্য তৈরি হয়েছে।" : "ইভেন্ট তথ্য আপডেট হয়েছে।";
        return RedirectToAction(nameof(Event));
    }

    private void DeleteEventLogo(string? logoUrl)
    {
        if (string.IsNullOrEmpty(logoUrl)) return;
        try
        {
            var full = Path.Combine(env.WebRootPath, logoUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (System.IO.File.Exists(full)) System.IO.File.Delete(full);
        }
        catch (IOException)
        {
            // best effort — a stale file in uploads is harmless
        }
    }

    // Emails the new account's credentials after approval. Returns the Bengali flash note
    // describing the outcome — sent, skipped (no address / no SMTP), or failed.
    private async Task<string> SendLoginEmailAsync(Registration registration, string communityName)
    {
        if (string.IsNullOrWhiteSpace(registration.Email))
            return "রেজিস্ট্রেন্টের ইমেইল নেই, তাই লগইন তথ্য ইমেইল করা যায়নি।";
        if (!emailSender.IsConfigured)
            return "SMTP কনফিগার করা নেই, তাই লগইন তথ্য ইমেইল করা হয়নি।";

        var name = System.Net.WebUtility.HtmlEncode(registration.FullName.Trim());
        var loginUrl = $"{Request.Scheme}://{Request.Host}/Account/Login";
        var body = $"""
            <p>সম্মানিত {name},</p>
            <p>অভিনন্দন! আপনার রেজিস্ট্রেশন (<b>{registration.RegistrationNo}</b>) অনুমোদিত হয়েছে। এখন আপনি সাইটে লগইন করে আপনার কার্ড দেখতে পারবেন।</p>
            <p>আপনার লগইন তথ্য:</p>
            <ul>
                <li>ইউজারনেম: <b>{registration.Phone}</b></li>
                <li>প্রাথমিক পাসওয়ার্ড: <b>{registration.Phone}</b></li>
            </ul>
            <p>প্রথম লগইনের সময় নিজের পছন্দের পাসওয়ার্ড সেট করে নিতে হবে।</p>
            <p><a href="{loginUrl}">এখানে লগইন করুন</a></p>
            <p>— {System.Net.WebUtility.HtmlEncode(communityName)}</p>
            """;

        var to = registration.Email.Trim();
        var sent = await emailSender.SendAsync(
            to,
            $"{communityName} — রেজিস্ট্রেশন অনুমোদিত, লগইন তথ্য",
            body);
        return sent
            ? $"লগইন তথ্য {to} ইমেইলে পাঠানো হয়েছে।"
            : "লগইন তথ্য ইমেইল করা ব্যর্থ হয়েছে — অনুগ্রহ করে নিজে জানিয়ে দিন।";
    }

    // GET /Admin/Setup — hub page listing the setup sections.
    public IActionResult Setup() => View();

    // GET /Admin/WelcomeNotes — add/edit form on top, list with status switches below. ?edit=N prefills.
    public async Task<IActionResult> WelcomeNotes(int? edit)
    {
        ViewBag.WelcomeNotes = await db.WelcomeNotes.OrderBy(w => w.Id).ToListAsync();
        if (edit is int id)
            ViewBag.Editing = await db.WelcomeNotes.AsNoTracking().FirstOrDefaultAsync(w => w.Id == id);
        return View();
    }

    // POST /Admin/SaveWelcomeNote — insert when Id is 0, update otherwise. New notes start active;
    // the status itself is only changed by the switch in the list. An optional profile photo
    // (jpg/png/webp, ≤ 2 MB) is stored under wwwroot/uploads/welcome and replaces any previous one.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveWelcomeNote(WelcomeNoteInputModel model, IFormFile? photo)
    {
        if (!ModelState.IsValid)
        {
            TempData["FlashError"] = "ফর্মের তথ্য ঠিক নয় — আবার চেষ্টা করুন।";
            return RedirectToAction(nameof(WelcomeNotes));
        }

        string? photoUrl = null;
        if (photo is { Length: > 0 })
        {
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var ext = Path.GetExtension(photo.FileName).ToLowerInvariant();
            if (!allowed.Contains(ext) || photo.Length > 2 * 1024 * 1024)
            {
                TempData["FlashError"] = "ছবি হতে হবে jpg/png/webp এবং সর্বোচ্চ ২ এমবি।";
                return RedirectToAction(nameof(WelcomeNotes));
            }

            var folder = Path.Combine(env.WebRootPath, "uploads", "welcome");
            Directory.CreateDirectory(folder);
            var fileName = $"{Guid.NewGuid():N}{ext}";
            await using var stream = System.IO.File.Create(Path.Combine(folder, fileName));
            await photo.CopyToAsync(stream);
            photoUrl = $"/uploads/welcome/{fileName}";
        }

        WelcomeNote note;
        if (model.Id == 0)
        {
            note = new WelcomeNote { IsActive = true };
            db.WelcomeNotes.Add(note);
        }
        else
        {
            note = await db.WelcomeNotes.FirstOrDefaultAsync(w => w.Id == model.Id);
            if (note is null) return NotFound();
        }

        note.Message = model.Message!.Trim();
        note.Name = model.Name!.Trim();
        note.Designation = model.Designation!.Trim();
        if (photoUrl is not null)
        {
            DeleteWelcomePhoto(note.PhotoUrl); // replace the old file only after a new one is in hand
            note.PhotoUrl = photoUrl;
        }
        await db.SaveChangesAsync();

        TempData["Flash"] = model.Id == 0 ? "ওয়েলকাম নোট যোগ হয়েছে।" : "ওয়েলকাম নোট আপডেট হয়েছে।";
        return RedirectToAction(nameof(WelcomeNotes));
    }

    private void DeleteWelcomePhoto(string? photoUrl)
    {
        if (string.IsNullOrEmpty(photoUrl)) return;
        try
        {
            var full = Path.Combine(env.WebRootPath, photoUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (System.IO.File.Exists(full)) System.IO.File.Delete(full);
        }
        catch (IOException)
        {
            // best effort — a stale file in uploads is harmless
        }
    }

    // POST /Admin/ToggleWelcomeNoteStatus/5 — flip a note between active (home page) and inactive.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleWelcomeNoteStatus(int id)
    {
        var note = await db.WelcomeNotes.FirstOrDefaultAsync(w => w.Id == id);
        if (note is not null)
        {
            note.IsActive = !note.IsActive;
            await db.SaveChangesAsync();
            TempData["Flash"] = note.IsActive
                ? $"{note.Name} এর নোট চালু করা হয়েছে।"
                : $"{note.Name} এর নোট বন্ধ করা হয়েছে।";
        }
        return RedirectToAction(nameof(WelcomeNotes));
    }

    // GET /Admin/WhyJoin — add/edit form on top, list with status switches below. ?edit=N prefills.
    public async Task<IActionResult> WhyJoin(int? edit)
    {
        ViewBag.WhyJoinItems = await db.WhyJoinItems.OrderBy(w => w.Id).ToListAsync();
        ViewBag.AvailableIcons = WhyJoinItem.AvailableIcons;
        if (edit is int id)
            ViewBag.Editing = await db.WhyJoinItems.AsNoTracking().FirstOrDefaultAsync(w => w.Id == id);
        return View();
    }

    // POST /Admin/SaveWhyJoin — insert when Id is 0, update otherwise. The icon must come from the
    // curated select; status is only changed by the switch in the list.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveWhyJoin(WhyJoinItemInputModel model)
    {
        if (!WhyJoinItem.AvailableIcons.Contains(model.Icon))
            ModelState.AddModelError(nameof(model.Icon), "তালিকা থেকে আইকন নির্বাচন করুন।");

        if (!ModelState.IsValid)
        {
            TempData["FlashError"] = "ফর্মের তথ্য ঠিক নয় — আবার চেষ্টা করুন।";
            return RedirectToAction(nameof(WhyJoin));
        }

        WhyJoinItem item;
        if (model.Id == 0)
        {
            item = new WhyJoinItem { IsActive = true };
            db.WhyJoinItems.Add(item);
        }
        else
        {
            item = await db.WhyJoinItems.FirstOrDefaultAsync(w => w.Id == model.Id);
            if (item is null) return NotFound();
        }

        item.Title = model.Title!.Trim();
        item.Description = model.Description!.Trim();
        item.Icon = model.Icon!;
        await db.SaveChangesAsync();

        TempData["Flash"] = model.Id == 0 ? "কার্ড যোগ হয়েছে।" : "কার্ড আপডেট হয়েছে।";
        return RedirectToAction(nameof(WhyJoin));
    }

    // POST /Admin/ToggleWhyJoinStatus/5 — flip a benefit card between active (home page) and inactive.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleWhyJoinStatus(int id)
    {
        var item = await db.WhyJoinItems.FirstOrDefaultAsync(w => w.Id == id);
        if (item is not null)
        {
            item.IsActive = !item.IsActive;
            await db.SaveChangesAsync();
            TempData["Flash"] = item.IsActive
                ? $"{item.Title} চালু করা হয়েছে।"
                : $"{item.Title} বন্ধ করা হয়েছে।";
        }
        return RedirectToAction(nameof(WhyJoin));
    }

    // GET /Admin/JerseySizes — add/edit form on top, list with status switches below. ?edit=N prefills.
    public async Task<IActionResult> JerseySizes(int? edit)
    {
        ViewBag.JerseySizes = await db.JerseySizeOptions.OrderBy(j => j.DisplayOrder).ToListAsync();
        if (edit is int id)
            ViewBag.Editing = await db.JerseySizeOptions.AsNoTracking().FirstOrDefaultAsync(j => j.Id == id);
        return View();
    }

    // POST /Admin/SaveJerseySize — insert when Id is 0, update otherwise. Duplicate codes are rejected;
    // status is only changed by the switch in the list.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveJerseySize(JerseySizeInputModel model)
    {
        var value = model.Value?.Trim().ToUpperInvariant();

        if (!ModelState.IsValid)
        {
            TempData["FlashError"] = "ফর্মের তথ্য ঠিক নয় — আবার চেষ্টা করুন।";
            return RedirectToAction(nameof(JerseySizes));
        }

        if (await db.JerseySizeOptions.AnyAsync(j => j.Value == value && j.Id != model.Id))
        {
            TempData["FlashError"] = "এই সাইজ কোড আগেই আছে।";
            return RedirectToAction(nameof(JerseySizes));
        }

        if (model.Id == 0)
        {
            var maxOrder = await db.JerseySizeOptions.AnyAsync()
                ? await db.JerseySizeOptions.MaxAsync(j => j.DisplayOrder)
                : 0;
            db.JerseySizeOptions.Add(new JerseySizeOption
            {
                Value = value!,
                Label = model.Label!.Trim(),
                DisplayOrder = maxOrder + 1,
                IsActive = true,
            });
        }
        else
        {
            var size = await db.JerseySizeOptions.FirstOrDefaultAsync(j => j.Id == model.Id);
            if (size is null) return NotFound();

            size.Value = value!;
            size.Label = model.Label!.Trim();
        }
        await db.SaveChangesAsync();

        TempData["Flash"] = model.Id == 0 ? "জার্সি সাইজ যোগ হয়েছে।" : "জার্সি সাইজ আপডেট হয়েছে।";
        return RedirectToAction(nameof(JerseySizes));
    }

    // POST /Admin/ToggleJerseySizeStatus/5 — disabled sizes leave the registration form but old
    // registrations keep rendering their label.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleJerseySizeStatus(int id)
    {
        var size = await db.JerseySizeOptions.FirstOrDefaultAsync(j => j.Id == id);
        if (size is not null)
        {
            size.IsActive = !size.IsActive;
            await db.SaveChangesAsync();
            TempData["Flash"] = size.IsActive
                ? $"{size.Label} চালু করা হয়েছে।"
                : $"{size.Label} বন্ধ করা হয়েছে।";
        }
        return RedirectToAction(nameof(JerseySizes));
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
