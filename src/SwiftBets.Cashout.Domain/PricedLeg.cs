namespace SwiftBets.Cashout.Domain;

/// <summary>A leg at its placed odds, with the live odds of its selection while it is still open.</summary>
public sealed record PricedLeg(Guid LegId, decimal PlacedOdds, LegStatus Status, decimal? CurrentOdds);
