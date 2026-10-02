namespace SwiftBets.Cashout.Application.Ports;

public sealed record CashOutResult(bool Accepted, string? RefusalCode, bool WasApplied);

/// <summary>Settlement's cashout API: the coupon to price, and the final-state lock that settles it at the agreed amount.</summary>
public interface ISettlementCashout
{
    Task<CouponForCashout?> GetCouponAsync(Guid couponId, CancellationToken cancellationToken);

    Task<CashOutResult> CashOutAsync(Guid cashoutId, Guid couponId, Guid punterId, long amount, string currency, CancellationToken cancellationToken);
}
