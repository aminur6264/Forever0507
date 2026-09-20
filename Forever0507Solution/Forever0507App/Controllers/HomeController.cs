using Forever0507App.Data;
using Forever0507App.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace Forever0507App.Controllers
{
    public class HomeController(AlumniDbContext db) : Controller
    {
        public async Task<IActionResult> Index()
        {
            ViewBag.WelcomeNotes = await db.WelcomeNotes.AsNoTracking()
                .Where(w => w.IsActive)
                .OrderBy(w => w.Id)
                .ToListAsync();
            ViewBag.WhyJoinItems = await db.WhyJoinItems.AsNoTracking()
                .Where(w => w.IsActive)
                .OrderBy(w => w.Id)
                .ToListAsync();
            ViewBag.GalleryImages = await db.GalleryImages.AsNoTracking()
                .Where(g => g.IsActive && g.ApprovalStatus == GalleryImage.ApprovalApproved)
                .OrderBy(g => g.Id)
                .ToListAsync();
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
