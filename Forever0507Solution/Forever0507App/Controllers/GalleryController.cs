using Forever0507App.Data;
using Forever0507App.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Forever0507App.Controllers;

// Regular users' own gallery uploads — everything here starts pending and is published only
// after an admin approves it (Admin → গ্যালারি). Admins manage their uploads there instead,
// hence the Regular-only role: both admin kinds carry just the Admin role claim.
[Authorize(Roles = AuthConstants.RegularRole)]
public class GalleryController(AlumniDbContext db) : Controller
{
    // GET /Gallery?edit=N — my uploads with the upload/edit form on top. ?edit=N prefills.
    public async Task<IActionResult> Index(int? edit)
    {
        var me = User.Identity!.Name!;
        ViewBag.MyImages = await db.GalleryImages.AsNoTracking()
            .Where(g => g.UploadedBy == me)
            .OrderByDescending(g => g.Id)
            .ToListAsync();
        if (edit is int id)
            ViewBag.Editing = await OwnEditableAsync(id);
        return View();
    }

    // POST /Gallery/Save — upload when Id is 0 (pending until an admin approves), edit otherwise.
    // Editing is the uploader's own privilege and only before approval; resubmitting a rejected
    // upload clears the old decision so it rejoins the pending queue.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(GalleryImageInputModel model, IFormFile? image)
    {
        var backTo = RedirectToAction(nameof(Index));

        if (model.Id == 0 && image is null)
            ModelState.AddModelError(nameof(model.Id), "একটি ছবি নির্বাচন করুন।");

        byte[]? imageData = null;
        string? imageContentType = null;
        if (image is not null && image.Length > 0)
        {
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var ext = Path.GetExtension(image.FileName).ToLowerInvariant();
            if (!allowed.Contains(ext) || image.Length > 2 * 1024 * 1024)
            {
                TempData["FlashError"] = "ছবি jpg/png/webp হতে হবে এবং সর্বোচ্চ ২ এমবি হতে হবে।";
                return backTo;
            }
            using var ms = new MemoryStream();
            await image.CopyToAsync(ms);
            imageData = ms.ToArray();
            imageContentType = ext switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                _ => "image/webp",
            };
        }

        if (!ModelState.IsValid)
        {
            TempData["FlashError"] = "ক্যাপশন লিখুন — আবার চেষ্টা করুন।";
            return backTo;
        }

        var me = User.Identity!.Name!;
        if (model.Id == 0)
        {
            var myName = await db.AppUsers.AsNoTracking()
                .Where(u => u.Phone == me)
                .Select(u => u.FullName)
                .FirstOrDefaultAsync();

            db.GalleryImages.Add(new GalleryImage
            {
                Title = model.Title!.Trim(),
                ImageData = imageData!,
                ImageContentType = imageContentType!,
                IsActive = true, // takes effect once an admin approves
                UploadedBy = me,
                UploadedByName = !string.IsNullOrWhiteSpace(myName) ? myName.Trim() : me,
                ApprovalStatus = null, // pending — an admin decides from the admin gallery
            });
            await db.SaveChangesAsync();
            TempData["Flash"] = "ছবি জমা হয়েছে — অনুমোদনের পরে এটি ওয়েবসাইটে দেখা যাবে।";
        }
        else
        {
            var item = await db.GalleryImages.FirstOrDefaultAsync(g => g.Id == model.Id);
            if (item is null) return NotFound();
            // অনুমোদিত uploads are final; only the uploader may touch them before that.
            if (item.UploadedBy != me || item.ApprovalStatus == GalleryImage.ApprovalApproved)
            {
                TempData["FlashError"] = "শুধু নিজের ছবি, অনুমোদনের আগে সম্পাদনা করতে পারবেন।";
                return backTo;
            }

            var wasRejected = item.ApprovalStatus == GalleryImage.ApprovalRejected;
            item.Title = model.Title!.Trim();
            if (imageData is not null)
            {
                item.ImageData = imageData;
                item.ImageContentType = imageContentType!;
            }
            if (wasRejected)
            {
                // Resubmission — clear the old decision so the image goes back to pending.
                item.ApprovalStatus = null;
                item.ApprovalBy = null;
                item.ApprovalAt = null;
            }
            await db.SaveChangesAsync();
            TempData["Flash"] = wasRejected
                ? "ছবি আবার জমা হয়েছে — অনুমোদনের অপেক্ষায়।"
                : "ছবি আপডেট হয়েছে।";
        }

        return backTo;
    }

    // POST /Gallery/Delete/5 — own uploads only, and only before approval.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var me = User.Identity!.Name!;
        var item = await db.GalleryImages.FirstOrDefaultAsync(g => g.Id == id);
        if (item is not null)
        {
            if (item.UploadedBy != me || item.ApprovalStatus == GalleryImage.ApprovalApproved)
            {
                TempData["FlashError"] = "শুধু নিজের ছবি, অনুমোদনের আগে মুছতে পারবেন।";
            }
            else
            {
                db.GalleryImages.Remove(item);
                await db.SaveChangesAsync();
                TempData["Flash"] = $"{item.Title} মুছে ফেলা হয়েছে।";
            }
        }
        return RedirectToAction(nameof(Index));
    }

    // Loads an image for the edit prefill — only the uploader's own, and only while it can still
    // be edited (pending or rejected); otherwise null so the form stays blank.
    private async Task<GalleryImage?> OwnEditableAsync(int id)
    {
        var me = User.Identity!.Name!;
        var item = await db.GalleryImages.AsNoTracking().FirstOrDefaultAsync(g => g.Id == id);
        return item is not null && item.UploadedBy == me && item.ApprovalStatus != GalleryImage.ApprovalApproved
            ? item
            : null;
    }
}
