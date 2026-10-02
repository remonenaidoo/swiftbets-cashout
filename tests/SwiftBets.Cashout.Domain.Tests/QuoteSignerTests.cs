namespace SwiftBets.Cashout.Domain.Tests;

public sealed class QuoteSignerTests
{
    private static readonly QuoteSigner Signer = new(new byte[32], TimeSpan.FromSeconds(10));
    private static readonly DateTimeOffset Now = new(2026, 10, 2, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void A_signed_quote_verifies_and_carries_its_amount()
    {
        var quote = new CashoutQuote(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 2_280, "ZAR", Now);

        var check = Signer.Verify(Signer.Sign(quote), Now.AddSeconds(3));

        check.Result.ShouldBe(QuoteCheckResult.Valid);
        check.Quote.ShouldBe(quote);
    }

    [Fact]
    public void A_quote_with_a_raised_amount_or_past_its_max_age_is_not_honoured()
    {
        var token = Signer.Sign(new CashoutQuote(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 2_280, "ZAR", Now));
        var forged = Signer.Sign(new CashoutQuote(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 9_999, "ZAR", Now)).Split('.')[0] + "." + token.Split('.')[1];

        Signer.Verify(forged, Now).Result.ShouldBe(QuoteCheckResult.Invalid);
        Signer.Verify(token, Now.AddSeconds(11)).Result.ShouldBe(QuoteCheckResult.Expired);
    }
}
