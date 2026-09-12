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
    public IActionResult Register() => View(new RegistrationInputModel());

    // POST /Registration/Register
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegistrationInputModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var registration = new Registration
        {
            FullName = model.FullName.Trim(),
            Phone = model.Phone.Trim(),
            Email = model.Email?.Trim(),
            Occupation = model.Occupation?.Trim(),
            PresentAddress = model.PresentAddress?.Trim(),
            BatchYear = model.BatchYear,
            ExtraMembers = model.ExtraMembers,
            ChildrenUnder5 = model.ChildrenUnder5,
            Note = model.Note?.Trim(),
            FeeAmount = FeeCalculator.Calculate(model)
        };

        db.Add(registration);
        await db.SaveChangesAsync(); // identity Id assigned here

        // Reg no derived from the identity Id — cannot collide, no locking needed.
        registration.RegistrationNo = $"REG-{eventOptions.StartTime.Year}-{registration.Id:D4}";
        await db.SaveChangesAsync();

        logger.LogInformation("New registration {RegistrationNo} for {Name}, fee {Fee}", registration.RegistrationNo, registration.FullName, registration.FeeAmount);
        return RedirectToAction(nameof(Success), new { id = registration.RegistrationNo });
    }

    // GET /Registration/Success/REG-2027-0001
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
    // GET /Registration/Card/REG-2027-0001        → card directly
    // GET /Registration/Card?q=<reg no or phone>  → lookup form submitted
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
                    .FirstOrDefaultAsync(r => r.Phone == query);
        }

        ViewBag.Searched = q is not null;
        return registration is null ? View("CardLookup") : View("Card", registration);
    }
}
