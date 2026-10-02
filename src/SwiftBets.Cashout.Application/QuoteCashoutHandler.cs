using Microsoft.Extensions.Options;
using SwiftBets.Cashout.Domain;
using SwiftBets.Contracts.Results;

namespace SwiftBets.Cashout.Application;

public sealed record CashoutOffer(Guid CouponId, long Amount, string Currency, DateTimeOffset ExpiresAt, string QuoteToken);

/// <summary>Quotes a coupon's cashout value and signs it; nothing is stored, the token is the quote.</summary>
public sealed class QuoteCashoutHandler(CashoutPricing pricing, QuoteSigner signer, IOptions<CashoutOptions> options, TimeProvider time)
{
    public async Task<Result<CashoutOffer>> HandleAsync(Guid couponId, Guid punterId, CancellationToken cancellationToken)
    {
        var priced = await pricing.PriceAsync(couponId, punterId, options.Value.Margin, cancellationToken);
        if (priced.Error is { } error)
        {
            return error;
        }

        var now = time.GetUtcNow();
        var quote = new CashoutQuote(Guid.CreateVersion7(now), couponId, punterId, priced.Value.Amount, priced.Value.Coupon.Currency, now);
        return Result<CashoutOffer>.Success(new CashoutOffer(couponId, quote.Amount, quote.Currency, now.AddSeconds(options.Value.QuoteMaxAgeSeconds), signer.Sign(quote)));
    }
}
