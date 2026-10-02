using SwiftBets.Cashout.Domain;

namespace SwiftBets.Cashout.Application.Ports;

public enum CouponCashoutState
{
    Open,
    Settled,
    CashedOut,
}

public sealed record CouponLegForCashout(Guid LegId, string FixtureId, string MarketId, string SelectionId, decimal PlacedOdds, LegStatus Status);

/// <summary>Settlement's view of a coupon, as far as cashout needs it.</summary>
public sealed record CouponForCashout(Guid CouponId, Guid PunterId, long Stake, string Currency, CouponCashoutState State, bool SingleLine, IReadOnlyList<CouponLegForCashout> Legs);
