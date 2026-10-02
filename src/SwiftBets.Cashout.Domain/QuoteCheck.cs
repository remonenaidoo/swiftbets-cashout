namespace SwiftBets.Cashout.Domain;

public sealed record QuoteCheck(QuoteCheckResult Result, CashoutQuote? Quote)
{
    public static QuoteCheck Invalid { get; } = new(QuoteCheckResult.Invalid, null);

    public static QuoteCheck Expired(CashoutQuote quote) => new(QuoteCheckResult.Expired, quote);

    public static QuoteCheck Valid(CashoutQuote quote) => new(QuoteCheckResult.Valid, quote);
}
