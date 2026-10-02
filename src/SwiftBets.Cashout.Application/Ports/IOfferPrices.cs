namespace SwiftBets.Cashout.Application.Ports;

/// <summary>A selection's live odds, or null when its market is not open for betting.</summary>
public sealed record LivePrice(decimal? Odds);

public interface IOfferPrices
{
    Task<LivePrice> GetAsync(string fixtureId, string marketId, string selectionId, CancellationToken cancellationToken);
}
