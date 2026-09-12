using System.ComponentModel.DataAnnotations;

namespace Forever0507App.Models;

/// <summary>Lookup: jersey sizes offered on the form (value stored on the registration, label shown in UI).</summary>
public class JerseySizeOption
{
    public int Id { get; set; }

    /// <summary>Stored on the registration, e.g. "XL".</summary>
    [MaxLength(5)]
    public string Value { get; set; } = "";

    /// <summary>Bengali display label, e.g. "এক্সট্রা লার্জ (XL)".</summary>
    [MaxLength(60)]
    public string Label { get; set; } = "";

    public int DisplayOrder { get; set; }
}
