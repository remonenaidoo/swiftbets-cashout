using SwiftBets.Cashout.Application.Ports;
using SwiftBets.Cashout.Domain;
using SwiftBets.Contracts.Errors;
using SwiftBets.Contracts.Results;

namespace SwiftBets.Cashout.Application;

/// <summary>Prices a coupon at live odds; shared by quote and execute so both apply exactly the same rules.</summary>
public sealed class CashoutPricing(ISettlementCashout settlement, IOfferPrices offer)
{
    public sealed record Priced(CouponForCashout Coupon, long Amount);

    public async Task<Result<Priced>> PriceAsync(Guid couponId, Guid punterId, decimal margin, CancellationToken cancellationToken)
    {
        var coupon = await settlement.GetCouponAsync(couponId, cancellationToken);
        if (coupon is null || coupon.PunterId != punterId)
        {
            return Error.NotFound("coupon_not_found", "No such coupon.");
        }

        if (coupon.State != CouponCashoutState.Open || !coupon.SingleLine)
        {
            return Error.BusinessRule("coupon_not_cashable", "This coupon cannot be cashed out.");
        }

        // The cashout pricer values independent legs; a bet builder's correlated price is not one it can reproduce.
        if (coupon.Legs.Any(l => l.MarketId == Contracts.Offer.BetBuilderPricing.MarketId))
        {
            return Error.BusinessRule("bet_builder_not_cashable", "Cash out is not available on bet builder bets.");
        }

        var legs = new List<PricedLeg>();
        foreach (var leg in coupon.Legs)
        {
            decimal? current = null;
            if (leg.Status == LegStatus.Open)
            {
                current = (await offer.GetAsync(leg.FixtureId, leg.MarketId, leg.SelectionId, cancellationToken)).Odds;
                if (current is null)
                {
                    return Error.BusinessRule("market_suspended", "A market on this coupon is not open; cashout is unavailable for now.");
                }
            }

            legs.Add(new PricedLeg(leg.LegId, leg.PlacedOdds, leg.Status, current));
        }

        return CashoutPricer.Price(coupon.Stake, legs, margin) is { } amount
            ? Result<Priced>.Success(new Priced(coupon, amount))
            : Error.BusinessRule("coupon_not_cashable", "This coupon cannot be cashed out.");
    }
}
