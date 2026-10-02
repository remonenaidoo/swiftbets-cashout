namespace SwiftBets.Cashout.Domain;

/// <summary>An offer to cash a coupon out for <c>Amount</c>, valid only briefly after <c>IssuedAt</c>.</summary>
public sealed record CashoutQuote(Guid QuoteId, Guid CouponId, Guid PunterId, long Amount, string Currency, DateTimeOffset IssuedAt);
