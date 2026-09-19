using Forever0507App.Data;
using Forever0507App.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Forever0507App.Controllers;

/// <summary>
/// Serves images stored in the database — no filesystem writes needed, which keeps
/// uploads working on locked-down shared hosting (Plesk etc.).
/// </summary>
[Route("[controller]")]
public class ImageController(AlumniDbContext db) : Controller
{
    private const string FallbackContentType = "image/png";

    // GET /Image/Logo — public; the navbar shows it on the login page too.
    [HttpGet("Logo")]
    public async Task<IActionResult> Logo()
    {
        var logo = await db.EventSettings.AsNoTracking()
            .Where(s => s.Id == EventSettings.SingleRowId)
            .Select(s => new { s.LogoData, s.LogoContentType })
            .SingleOrDefaultAsync();
        if (logo?.LogoData is not { Length: > 0 }) return NotFound();

        // Short cache — a freshly uploaded logo appears within minutes, no stale browser copies.
        Response.Headers.CacheControl = "public,max-age=300";
        return File(logo.LogoData, NullOrEmpty(logo.LogoContentType) ? FallbackContentType : logo.LogoContentType!);
    }

    // GET /Image/Welcome/5 — public; profile photos appear on the home page.
    [HttpGet("Welcome/{id:int}")]
    public async Task<IActionResult> Welcome(int id)
    {
        var photo = await db.WelcomeNotes.AsNoTracking()
            .Where(n => n.Id == id)
            .Select(n => new { n.PhotoData, n.PhotoContentType })
            .FirstOrDefaultAsync();
        if (photo?.PhotoData is not { Length: > 0 }) return NotFound();

        Response.Headers.CacheControl = "public,max-age=300";
        return File(photo.PhotoData, NullOrEmpty(photo.PhotoContentType) ? FallbackContentType : photo.PhotoContentType!);
    }

    // GET /Image/Gallery/5 — public; home-page gallery pictures.
    [HttpGet("Gallery/{id:int}")]
    public async Task<IActionResult> Gallery(int id)
    {
        var image = await db.GalleryImages.AsNoTracking()
            .Where(g => g.Id == id)
            .Select(g => new { g.ImageData, g.ImageContentType })
            .FirstOrDefaultAsync();
        if (image?.ImageData is not { Length: > 0 }) return NotFound();

        Response.Headers.CacheControl = "public,max-age=300";
        return File(image.ImageData, NullOrEmpty(image.ImageContentType) ? FallbackContentType : image.ImageContentType!);
    }

    // GET /Image/Khoroch/5 — admins only; receipts are financial documents.
    [Authorize(Roles = AuthConstants.AdminRole)]
    [HttpGet("Khoroch/{id:int}")]
    public async Task<IActionResult> Khoroch(int id)
    {
        var image = await db.Khorochs.AsNoTracking()
            .Where(k => k.Id == id)
            .Select(k => new { k.ImageData, k.ImageContentType })
            .FirstOrDefaultAsync();
        if (image?.ImageData is not { Length: > 0 }) return NotFound();

        Response.Headers.CacheControl = "private,max-age=300";
        return File(image.ImageData, NullOrEmpty(image.ImageContentType) ? FallbackContentType : image.ImageContentType!);
    }

    private static bool NullOrEmpty(string? value) => string.IsNullOrWhiteSpace(value);
}
