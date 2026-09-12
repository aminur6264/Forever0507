using Forever0507App.Models;
using Microsoft.EntityFrameworkCore;

namespace Forever0507App.Data;

/// <summary>
/// Seeds the lookup tables (districts, schools, jersey sizes, payment mediums) on first run.
/// Each table is only filled while empty, so registrant-added schools are never overwritten.
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(AlumniDbContext db, EventOptions eventOptions)
    {
        if (!await db.Districts.AnyAsync())
        {
            db.Districts.AddRange(DistrictNames.Select((name, i) => new District { Name = name, DisplayOrder = i }));
            await db.SaveChangesAsync();
        }

        if (!await db.Schools.AnyAsync())
        {
            db.Schools.AddRange(eventOptions.Schools.Select(name => new School { Name = name, IsUserAdded = false }));
            await db.SaveChangesAsync();
        }

        if (!await db.JerseySizeOptions.AnyAsync())
        {
            db.JerseySizeOptions.AddRange(
            [
                new() { Value = "S", Label = "স্মল (S)", DisplayOrder = 1 },
                new() { Value = "M", Label = "মিডিয়াম (M)", DisplayOrder = 2 },
                new() { Value = "L", Label = "লার্জ (L)", DisplayOrder = 3 },
                new() { Value = "XL", Label = "এক্সট্রা লার্জ (XL)", DisplayOrder = 4 },
                new() { Value = "XXL", Label = "ডাবল এক্সট্রা লার্জ (XXL)", DisplayOrder = 5 },
            ]);
            await db.SaveChangesAsync();
        }

        if (!await db.PaymentMediumOptions.AnyAsync())
        {
            db.PaymentMediumOptions.AddRange(
            [
                new() { Name = "বিকাশ", DisplayOrder = 1 },
                new() { Name = "নগদ", DisplayOrder = 2 },
                new() { Name = "রকেট", DisplayOrder = 3 },
            ]);
            await db.SaveChangesAsync();
        }
    }

    /// <summary>Bangladesh's 64 districts, grouped by division.</summary>
    private static readonly string[] DistrictNames =
    [
        // ঢাকা বিভাগ
        "ঢাকা", "গাজীপুর", "গোপালগঞ্জ", "কিশোরগঞ্জ", "ফরিদপুর", "মাদারীপুর", "মানিকগঞ্জ",
        "মুন্সিগঞ্জ", "নারায়ণগঞ্জ", "নরসিংদী", "রাজবাড়ী", "শরীয়তপুর", "টাঙ্গাইল",
        // চট্টগ্রাম বিভাগ
        "চট্টগ্রাম", "কক্সবাজার", "কুমিল্লা", "ব্রাহ্মণবাড়িয়া", "চাঁদপুর", "ফেনী", "নোয়াখালী",
        "লক্ষ্মীপুর", "খাগড়াছড়ি", "রাঙামাটি", "বান্দরবান",
        // রাজশাহী বিভাগ
        "রাজশাহী", "বগুড়া", "চাঁপাইনবাবগঞ্জ", "জয়পুরহাট", "পাবনা", "নাটোর", "নওগাঁ", "সিরাজগঞ্জ",
        // রংপুর বিভাগ
        "রংপুর", "নীলফামারী", "দিনাজপুর", "কুড়িগ্রাম", "লালমনিরহাট", "গাইবান্ধা", "ঠাকুরগাঁও", "পঞ্চগড়",
        // খুলনা বিভাগ
        "খুলনা", "যশোর", "সাতক্ষীরা", "বাগেরহাট", "ঝিনাইদহ", "কুষ্টিয়া", "মাগুরা", "নড়াইল",
        "চুয়াডাঙ্গা", "মেহেরপুর",
        // বরিশাল বিভাগ
        "বরিশাল", "ঝালকাঠি", "পিরোজপুর", "ভোলা", "পটুয়াখালী", "বরগুনা",
        // সিলেট বিভাগ
        "সিলেট", "মৌলভীবাজার", "হবিগঞ্জ", "সুনামগঞ্জ",
        // ময়মনসিংহ বিভাগ
        "ময়মনসিংহ", "জামালপুর", "নেত্রকোণা", "শেরপুর",
    ];
}
