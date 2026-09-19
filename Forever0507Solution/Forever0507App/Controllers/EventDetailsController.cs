using Forever0507App.Data;
using Forever0507App.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Forever0507App.Controllers;

/// <summary>Post-login summary for regular users — approved registration counts overall and by
/// district/school. Each count links to the list of registrations behind it.</summary>
[Authorize]
public class EventDetailsController(AlumniDbContext db) : Controller
{
    private const int DefaultPageSize = 30;

    // GET /EventDetails — counts only; payment and approval workflow stay on the admin side.
    public async Task<IActionResult> Index()
    {
        var approved = db.Registrations.AsNoTracking()
            .Where(r => r.ApprovalStatus == Registration.ApprovalApproved);

        ViewBag.TotalApproved = await approved.CountAsync();
        ViewBag.DistrictTally = await approved
            .GroupBy(r => r.District)
            .Select(g => new NameTally { Name = g.Key, Total = g.Count() })
            .OrderByDescending(x => x.Total).ThenBy(x => x.Name)
            .ToListAsync();
        ViewBag.SchoolTally = await approved
            .GroupBy(r => r.SchoolName)
            .Select(g => new NameTally { Name = g.Key, Total = g.Count() })
            .OrderByDescending(x => x.Total).ThenBy(x => x.Name)
            .ToListAsync();
        return View();
    }

    // GET /EventDetails/List?district=&school=&page=&pageSize= — the approved registrations behind a
    // summary count: name, district, school name.
    public async Task<IActionResult> List(string? district, string? school, int page = 1, int pageSize = DefaultPageSize)
    {
        if (pageSize is not (10 or 30 or 50 or 100)) pageSize = DefaultPageSize;

        var rows = db.Registrations.AsNoTracking()
            .Where(r => r.ApprovalStatus == Registration.ApprovalApproved);
        if (!string.IsNullOrWhiteSpace(district)) rows = rows.Where(r => r.District == district);
        if (!string.IsNullOrWhiteSpace(school)) rows = rows.Where(r => r.SchoolName == school);

        var totalCount = await rows.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
        if (page < 1) page = 1;
        if (page > totalPages) page = totalPages;

        ViewBag.Rows = await rows.OrderBy(r => r.FullName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        ViewBag.District = district ?? "";
        ViewBag.School = school ?? "";
        ViewBag.Page = page;
        ViewBag.PageSize = pageSize;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalCount = totalCount;
        return View();
    }
}
