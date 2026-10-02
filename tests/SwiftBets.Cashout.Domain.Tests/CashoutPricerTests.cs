namespace SwiftBets.Cashout.Domain.Tests;

public sealed class CashoutPricerTests
{
    [Fact]
    public void An_acca_with_a_won_leg_buys_back_the_open_leg_at_live_odds_less_the_margin()
    {
        // R10 at 2.00 x 3.00: the 2.00 leg has won; the open leg drifted from 3.00 to 2.50. 10 * 2 * (3 / 2.5) * 0.95 = R22.80.
        var value = CashoutPricer.Price(1_000, [new(Guid.NewGuid(), 2.00m, LegStatus.Won, null), new(Guid.NewGuid(), 3.00m, LegStatus.Open, 2.50m)], 0.05m);

        value.ShouldBe(2_280);
    }

    [Fact]
    public void A_coupon_with_a_lost_leg_has_no_cashout()
    {
        var value = CashoutPricer.Price(1_000, [new(Guid.NewGuid(), 2.00m, LegStatus.Lost, null), new(Guid.NewGuid(), 3.00m, LegStatus.Open, 2.50m)], 0.05m);

        value.ShouldBeNull();
    }
}
