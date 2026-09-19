using System.ComponentModel.DataAnnotations;

namespace Forever0507App.Models;

/// <summary>A picture in the home-page gallery; only active ones are visible. Bytes live in the
/// database (shared-hosting safe) and are served via /Image/Gallery/{id}.</summary>
public class GalleryImage
{
    public int Id { get; set; }

    /// <summary>Caption shown under the picture on the home page.</summary>
    [MaxLength(120)]
    public string Title { get; set; } = "";

    public byte[] ImageData { get; set; } = [];

    [MaxLength(50)]
    public string ImageContentType { get; set; } = "";

    /// <summary>Inactive images stay listed in the admin but are hidden from the home page; new uploads start active.</summary>
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>Upload/edit surface — the file is required on create, optional on edit (keeping the old one).</summary>
public class GalleryImageInputModel
{
    /// <summary>0 = upload, greater than 0 = edit.</summary>
    public int Id { get; set; }

    [Required(ErrorMessage = "ছবির ক্যাপশন লিখুন।")]
    [StringLength(120, ErrorMessage = "ক্যাপশন সর্বোচ্চ ১২০ অক্ষরের হতে হবে।")]
    [Display(Name = "ক্যাপশন")]
    public string? Title { get; set; }
}
