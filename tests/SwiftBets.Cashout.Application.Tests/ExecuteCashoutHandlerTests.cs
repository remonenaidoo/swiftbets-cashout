using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using SwiftBets.Cashout.Application.Ports;
using SwiftBets.Cashout.Domain;

namespace SwiftBets.Cashout.Application.Tests;

public sealed class ExecuteCashoutHandlerTests
{
    private static readonly Guid Punter = Guid.NewGuid();
    private static readonly Guid Coupon = Guid.NewGuid();

    [Fact]
    public async Task A_fresh_quote_still_worth_its_amount_is_paid_once_under_the_quote_id()
    {
        var (quote, execute, settlement, _) = await ArrangeAsync();

        var (done, refused) = await execute.HandleAsync(quote.QuoteToken, Punter, CancellationToken.None);
        await execute.HandleAsync(quote.QuoteToken, Punter, CancellationToken.None);

        refused.ShouldBeNull();
        done!.Amount.ShouldBe(quote.Amount);
        settlement.CashoutIds.Distinct().ShouldHaveSingleItem();
    }

    [Fact]
    public async Task A_quote_whose_value_fell_beyond_the_leeway_is_refused_with_a_fresh_quote()
    {
        var (quote, execute, settlement, offer) = await ArrangeAsync();
        offer.Odds = 4.00m;

        var (done, refused) = await execute.HandleAsync(quote.QuoteToken, Punter, CancellationToken.None);

        done.ShouldBeNull();
        refused!.Error.Code.ShouldBe("price_changed");
        refused.FreshQuote!.Amount.ShouldBeLessThan(quote.Amount);
        settlement.CashoutIds.ShouldBeEmpty();
    }

    [Fact]
    public async Task A_bet_builder_coupon_is_not_offered_cash_out()
    {
        var settlement = new FakeSettlement { MarketId = "bet-builder" };
        var quotes = new QuoteCashoutHandler(new CashoutPricing(settlement, new FakeOffer()), new QuoteSigner(new byte[32], TimeSpan.FromSeconds(10)),
            Options.Create(new CashoutOptions { SigningKey = Convert.ToBase64String(new byte[32]) }), TimeProvider.System);

        (await quotes.HandleAsync(Coupon, Punter, CancellationToken.None)).Error!.Code.ShouldBe("bet_builder_not_cashable");
    }

    private static async Task<(CashoutOffer Quote, ExecuteCashoutHandler Execute, FakeSettlement Settlement, FakeOffer Offer)> ArrangeAsync()
    {
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 10, 2, 12, 0, 0, TimeSpan.Zero));
        var options = Options.Create(new CashoutOptions { SigningKey = Convert.ToBase64String(new byte[32]) });
        var signer = new QuoteSigner(new byte[32], TimeSpan.FromSeconds(10));
        var settlement = new FakeSettlement();
        var offer = new FakeOffer();
        var pricing = new CashoutPricing(settlement, offer);
        var quotes = new QuoteCashoutHandler(pricing, signer, options, time);
        var quote = (await quotes.HandleAsync(Coupon, Punter, CancellationToken.None)).Value;
        time.Advance(TimeSpan.FromSeconds(2));
        return (quote, new ExecuteCashoutHandler(pricing, signer, quotes, settlement, options, time), settlement, offer);
    }

    private sealed class FakeOffer : IOfferPrices
    {
        public decimal Odds { get; set; } = 2.50m;

        public Task<LivePrice> GetAsync(string fixtureId, string marketId, string selectionId, CancellationToken cancellationToken) => Task.FromResult(new LivePrice(Odds));
    }

    private sealed class FakeSettlement : ISettlementCashout
    {
        public List<Guid> CashoutIds { get; } = [];

        public string MarketId { get; init; } = "fx-1x2";

        public Task<CouponForCashout?> GetCouponAsync(Guid couponId, CancellationToken cancellationToken) =>
            Task.FromResult<CouponForCashout?>(new CouponForCashout(couponId, Punter, 1_000, "ZAR", CouponCashoutState.Open, true,
                [new(Guid.NewGuid(), "fx", MarketId, "home", 3.00m, LegStatus.Open)]));

        public Task<CashOutResult> CashOutAsync(Guid cashoutId, Guid couponId, Guid punterId, long amount, string currency, CancellationToken cancellationToken)
        {
            CashoutIds.Add(cashoutId);
            return Task.FromResult(new CashOutResult(true, null, CashoutIds.Count == 1));
        }
    }
}
