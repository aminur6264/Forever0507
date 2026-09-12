using System.Globalization;
using Forever0507App.Data;
using Forever0507App.Helpers;
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
        ViewBag.Schools = await GetSchoolListAsync();
        ViewBag.Districts = BdDistricts.List;
        ViewBag.JerseySizes = JerseySize.Options;
        ViewBag.PaymentMediums = PaymentMedium.All;
        return View(new RegistrationInputModel());
    }

    // POST /Registration/Register
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegistrationInputModel model)
    {
        ViewBag.Schools = await GetSchoolListAsync();
        ViewBag.Districts = BdDistricts.List;
        ViewBag.JerseySizes = JerseySize.Options;
        ViewBag.PaymentMediums = PaymentMedium.All;

        if (!ModelState.IsValid) return View(model);

        var registration = new Registration
        {
            FullName = model.FullName.Trim(),
            District = model.District!.Trim(),
            SchoolName = model.ResolvedSchoolName!,
            SscYear = eventOptions.SscYear,
            PaymentMedium = model.PaymentMedium!,
            Amount = model.ResolvedAmount!.Value,
            PayableAmount = FeeCalculator.CalculatePayable(model.ResolvedAmount.Value),
            TransactionId = model.TransactionId!.Trim().ToUpperInvariant(),
            JerseySize = model.JerseySize!,
            Phone = model.Phone!.Trim()
        };

        db.Add(registration);
        await db.SaveChangesAsync(); // identity Id assigned here

        // Reg no derived from the identity Id — cannot collide, no locking needed.
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
        return View(registration);
    }

    // GET /Registration/Card                      → lookup form
    // GET /Registration/Card/REG-2026-0001        → card directly
    // GET /Registration/Card?q=<reg no / phone / txn id>
    [HttpGet]
    public async Task<IActionResult> Card(string? id, string? q)
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
        return registration is null ? View("CardLookup") : View("Card", registration);
    }

    /// <summary>Curated list from config, enriched with school names earlier registrants added themselves.</summary>
    private async Task<List<string>> GetSchoolListAsync()
    {
        var added = await db.Registrations.AsNoTracking()
            .Select(r => r.SchoolName)
            .Distinct()
            .ToListAsync();

        return eventOptions.Schools
            .Concat(added)
            .Distinct()
            .OrderBy(s => s, StringComparer.Create(new CultureInfo("bn-BD"), false))
            .ToList();
    }
}
