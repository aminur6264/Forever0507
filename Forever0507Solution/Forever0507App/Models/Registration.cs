using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Forever0507App.Services;

namespace Forever0507App.Models;

/// <summary>A persisted reunion registration. Validation lives on <see cref="RegistrationInputModel"/>.</summary>
public class Registration
{
    public int Id { get; set; }

    /// <summary>REG-2027-#### — derived from the identity Id after the first save.</summary>
    [MaxLength(20)]
    public string RegistrationNo { get; set; } = "";

    [MaxLength(120)]
    public string FullName { get; set; } = "";

    /// <summary>11-digit BD mobile number; secondary lookup key for the card page.</summary>
    [MaxLength(11)]
    public string Phone { get; set; } = "";

    [MaxLength(150)]
    public string? Email { get; set; }

    [MaxLength(120)]
    public string? Occupation { get; set; }

    [MaxLength(200)]
    public string? PresentAddress { get; set; }

    public int BatchYear { get; set; }

    /// <summary>Spouse / adult family members joining (each adds a fee).</summary>
    public int ExtraMembers { get; set; }

    /// <summary>Children under 5 (free, capped at 2 by validation).</summary>
    public int ChildrenUnder5 { get; set; }

    [MaxLength(400)]
    public string? Note { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal FeeAmount { get; set; }

    public string PaymentStatus { get; set; } = "অপরিশোধিত";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [NotMapped]
    public bool IsSeniorBatch => BatchYear <= FeeCalculator.SeniorBatchCutoffYear;

    [NotMapped]
    public int TotalAttendees => 1 + ExtraMembers + ChildrenUnder5;
}
