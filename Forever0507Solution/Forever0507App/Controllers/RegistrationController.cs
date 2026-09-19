using Forever0507App.Data;
using Forever0507App.Models;
using Forever0507App.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Forever0507App.Controllers;

public class RegistrationController(AlumniDbContext db, EventOptions eventOptions, ILogger<RegistrationController> logger) : Controller
{
    // GET /Registration/Register
    [HttpGet]
    public async Task<IActionResult> Register()
    {
        await PopulateLookupsAsync();
        return View(new RegistrationInputModel());
    }

    // POST /Registration/Register
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegistrationInputModel model)
    {
        await PopulateLookupsAsync();

        // Lookup values must exist in the seeded tables (client can only post what we offered).
        if (!string.IsNullOrEmpty(model.District) && !await db.Districts.AnyAsync(d => d.Name == model.District))
            ModelState.AddModelError(nameof(model.District), "সঠিক জেলা নির্বাচন করুন।");
        // The medium must be one of the MFS names currently offered by an active payment method.
        if (!string.IsNullOrEmpty(model.PaymentMedium)
            && !await db.PaymentMethods.AnyAsync(p => p.IsActive && p.MfsName == model.PaymentMedium))
            ModelState.AddModelError(nameof(model.PaymentMedium), "সঠিক ট্রানজেকশন মাধ্যম নির্বাচন করুন।");
        if (!string.IsNullOrEmpty(model.JerseySize) && !await db.JerseySizeOptions.AnyAsync(j => j.IsActive && j.Value == model.JerseySize))
            ModelState.AddModelError(nameof(model.JerseySize), "সঠিক জার্সি সাইজ নির্বাচন করুন।");

        if (!ModelState.IsValid) return View(model);

        var schoolName = model.ResolvedSchoolName!;

        db.Add(new Registration
        {
            FullName = model.FullName.Trim(),
            District = model.District!.Trim(),
            SchoolName = schoolName,
            SscYear = eventOptions.SscYear,
            PaymentMedium = model.PaymentMedium!,
            Amount = model.ResolvedAmount!.Value,
            PayableAmount = model.ResolvedAmount.Value, // no service charge — payable is the entered amount
            TransactionId = model.TransactionId!.Trim().ToUpperInvariant(),
            FromAccount = model.FromAccount!.Trim(),
            ToAccount = model.ToAccount!.Trim(),
            JerseySize = model.JerseySize!,
            NameOnJersey = model.NameOnJersey!.Trim(),
            Phone = model.Phone!.Trim(),
            Email = (model.Email ?? "").Trim()
        });

        // A school the registrant typed in themselves joins the dropdown for everyone.
        if (!await db.Schools.AnyAsync(s => s.Name == schoolName))
            db.Schools.Add(new School { Name = schoolName, IsUserAdded = true });

        await db.SaveChangesAsync(); // identity Id assigned here

        // Reg no derived from the identity Id — cannot collide, no locking needed.
        var registration = await db.Registrations.OrderBy(r => r.Id).LastAsync();
        registration.RegistrationNo = $"REG-{eventOptions.RegistrationYear}-{registration.Id:D4}";
        await db.SaveChangesAsync();

        logger.LogInformation("New registration {RegistrationNo} for {Name}, payable {Payable}",
            registration.RegistrationNo, registration.FullName, registration.PayableAmount);
        return RedirectToAction(nameof(Success), new { id = registration.RegistrationNo });
    }

    // GET /Registration/Success/REG-2026-0001
    [HttpGet]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> Success(string id)
    {
        var registration = await db.Registrations.AsNoTracking()
            .FirstOrDefaultAsync(r => r.RegistrationNo == id);
        if (registration is null) return RedirectToAction(nameof(Register));

        ViewBag.JerseyLabel = await GetJerseyLabelAsync(registration.JerseySize);
        return View(registration);
    }

    // GET /Registration/Card                      → lookup form
    // GET /Registration/Card/REG-2026-0001        → card directly
    // GET /Registration/Card?q=<reg no / phone / txn id>
    // GET /Registration/Card/REG-2026-0001?print=true → print/PDF layout (auto-opens the print dialog)
    [HttpGet]
    public async Task<IActionResult> Card(string? id, string? q, bool print = false)
    {
        Registration? registration = null;
        var query = (id ?? q)?.Trim();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var normalized = query.ToUpperInvariant().Replace('－', '-'); // tolerate fullwidth dash
            registration = await db.Registrations.AsNoTracking()
                    .FirstOrDefaultAsync(r => r.RegistrationNo == normalized)
                ?? await db.Registrations.AsNoTracking()
                    .FirstOrDefaultAsync(r => r.Phone == query)
                ?? await db.Registrations.AsNoTracking()
                    .FirstOrDefaultAsync(r => r.TransactionId == normalized);
        }

        ViewBag.Searched = q is not null;
        if (registration is null) return View("CardLookup");

        // Only approved registrations have a viewable card — undecided and rejected never do.
        if (registration.ApprovalStatus != Registration.ApprovalApproved)
        {
            ViewBag.Rejected = registration.ApprovalStatus == Registration.ApprovalRejected;
            ViewBag.Pending = registration.ApprovalStatus is null;
            return View("CardLookup");
        }

        ViewBag.JerseyLabel = await GetJerseyLabelAsync(registration.JerseySize);
        return print ? View("CardPrint", registration) : View("Card", registration);
    }

    /// <summary>Dropdown data, all read from the seeded SQL Server lookup tables.</summary>
    private async Task PopulateLookupsAsync()
    {
        ViewBag.Schools = await db.Schools.OrderBy(s => s.Id).ToListAsync();
        ViewBag.Districts = await db.Districts.OrderBy(d => d.DisplayOrder).ToListAsync();
        ViewBag.JerseySizes = await db.JerseySizeOptions
            .Where(j => j.IsActive)
            .OrderBy(j => j.DisplayOrder).ToListAsync();

        // Live committee accounts replace the old static config list — active ones only.
        var activePaymentMethods = await db.PaymentMethods.AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Id)
            .ToListAsync();
        ViewBag.ActivePaymentMethods = activePaymentMethods;

        // The transaction-medium choices mirror those same active accounts (one radio per MFS name).
        ViewBag.PaymentMediums = activePaymentMethods
            .Select(p => p.MfsName)
            .Distinct()
            .ToList();
    }

    private async Task<string> GetJerseyLabelAsync(string value)
        => await db.JerseySizeOptions.AsNoTracking()
               .Where(j => j.Value == value)
               .Select(j => j.Label)
               .FirstOrDefaultAsync()
           ?? value;
}
