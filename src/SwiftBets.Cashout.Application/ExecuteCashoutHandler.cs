using Microsoft.Extensions.Options;
using SwiftBets.Cashout.Application.Ports;
using SwiftBets.Cashout.Domain;
using SwiftBets.Contracts.Errors;

namespace SwiftBets.Cashout.Application;

public sealed record CashoutDone(Guid CouponId, long Amount, string Currency);

/// <summary>
/// Executes a signed quote: verified and in date, owned by the caller, and still worth at least the quote less the
/// leeway at live odds. The quoted amount is what is paid. The quote id is the cashout id, so retries pay once.
/// </summary>
public sealed class ExecuteCashoutHandler(CashoutPricing pricing, QuoteSigner signer, QuoteCashoutHandler quotes, ISettlementCashout settlement,
    IOptions<CashoutOptions> options, TimeProvider time)
{
    public sealed record Refusal(Error Error, CashoutOffer? FreshQuote);

    public async Task<(CashoutDone? Done, Refusal? Refused)> HandleAsync(string quoteToken, Guid punterId, CancellationToken cancellationToken)
    {
        var check = signer.Verify(quoteToken, time.GetUtcNow());
        if (check.Result == QuoteCheckResult.Invalid || check.Quote!.PunterId != punterId)
        {
            return (null, new Refusal(Error.Validation("quote_invalid", "That quote is not valid."), null));
        }

        var quote = check.Quote;
        if (check.Result == QuoteCheckResult.Expired)
        {
            return (null, await RequoteAsync(Error.Conflict("quote_expired", "The quote expired; here is a fresh one."), quote, cancellationToken));
        }

        var priced = await pricing.PriceAsync(quote.CouponId, punterId, options.Value.Margin, cancellationToken);
        if (priced.Error is { } error)
        {
            return (null, new Refusal(error, null));
        }

        if (priced.Value.Amount < quote.Amount * (1 - options.Value.Leeway))
        {
            return (null, await RequoteAsync(Error.Conflict("price_changed", "The cashout value moved; here is a fresh quote."), quote, cancellationToken));
        }

        var result = await settlement.CashOutAsync(quote.QuoteId, quote.CouponId, punterId, quote.Amount, quote.Currency, cancellationToken);
        return result.Accepted
            ? (new CashoutDone(quote.CouponId, quote.Amount, quote.Currency), null)
            : (null, new Refusal(Error.BusinessRule(result.RefusalCode ?? "cashout_refused", "Settlement refused the cashout."), null));
    }

    private async Task<Refusal> RequoteAsync(Error error, CashoutQuote quote, CancellationToken cancellationToken)
    {
        var fresh = await quotes.HandleAsync(quote.CouponId, quote.PunterId, cancellationToken);
        return new Refusal(error, fresh.Error is null ? fresh.Value : null);
    }
}
