using System.ComponentModel.DataAnnotations;

namespace SwiftBets.Cashout.Application;

public sealed class CashoutOptions
{
    public const string SectionName = "Cashout";

    /// <summary>The house's share of the fair buy-back value.</summary>
    [Range(0, 0.5)]
    public decimal Margin { get; set; } = 0.05m;

    /// <summary>How far the recomputed value may fall below the quote before execute refuses with a fresh quote.</summary>
    [Range(0, 0.2)]
    public decimal Leeway { get; set; } = 0.02m;

    [Range(1, 120)]
    public int QuoteMaxAgeSeconds { get; set; } = 10;

    /// <summary>HMAC key for quote tokens (base64, at least 32 bytes). From the environment or secret store only.</summary>
    [Required]
    [MinLength(44)]
    public string SigningKey { get; set; } = string.Empty;

    /// <summary>Keys being rotated out (base64): quotes they signed still verify; nothing new is signed with them.</summary>
    public List<string> PreviousSigningKeys { get; set; } = [];
}
