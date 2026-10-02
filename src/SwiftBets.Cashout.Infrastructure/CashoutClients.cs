using System.ComponentModel.DataAnnotations;

namespace SwiftBets.Cashout.Infrastructure;

public sealed class CashoutClients
{
    public const string SectionName = "Clients";

    [Required]
    [Url]
    public string SettlementGrpcAddress { get; set; } = string.Empty;

    [Required]
    [Url]
    public string OfferAddress { get; set; } = string.Empty;
}
