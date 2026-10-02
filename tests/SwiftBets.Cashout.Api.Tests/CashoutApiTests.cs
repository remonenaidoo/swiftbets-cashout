using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SwiftBets.BuildingBlocks.Testing;
using SwiftBets.Cashout.Application.Ports;
using SwiftBets.Cashout.Domain;

namespace SwiftBets.Cashout.Api.Tests;

public sealed class CashoutApiTests
{
    private static readonly Guid Punter = Guid.NewGuid();

    [Fact]
    public async Task A_quote_executes_at_its_amount()
    {
        await using var host = new Host();
        using var client = host.Punter();
        var quote = await QuoteAsync(client);

        using var executed = await client.PostAsJsonAsync("/cashout/execute", new { quoteToken = quote.GetProperty("quoteToken").GetString() }, TestContext.Current.CancellationToken);

        executed.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task A_moved_price_is_refused_as_a_standard_error_envelope_carrying_the_fresh_quote()
    {
        await using var host = new Host();
        using var client = host.Punter();
        var quote = await QuoteAsync(client);
        host.Offer.Odds = 4.00m;

        using var refused = await client.PostAsJsonAsync("/cashout/execute", new { quoteToken = quote.GetProperty("quoteToken").GetString() }, TestContext.Current.CancellationToken);

        refused.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        refused.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
        var body = await refused.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        body.GetProperty("code").GetString().ShouldBe("price_changed");
        body.GetProperty("freshQuote").GetProperty("amount").GetInt64().ShouldBeLessThan(quote.GetProperty("amount").GetInt64());
    }

    private static async Task<JsonElement> QuoteAsync(HttpClient client)
    {
        using var response = await client.PostAsJsonAsync("/cashout/quote", new { couponId = Guid.NewGuid() }, TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
    }

    private sealed class Host : WebApplicationFactory<Program>
    {
        public FakeOffer Offer { get; } = new();

        public HttpClient Punter()
        {
            var client = CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestJwt.Issue(CashoutApiTests.Punter.ToString(), "Punter"));
            return client;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("Jwt:Authority", TestJwt.Issuer);
            builder.UseSetting("Cashout:SigningKey", Convert.ToBase64String(new byte[33]));
            builder.UseSetting("Clients:SettlementGrpcAddress", "http://127.0.0.1:1");
            builder.UseSetting("Clients:OfferAddress", "http://127.0.0.1:1");
            builder.UseSetting("ServiceIdentity:TokenEndpoint", "http://127.0.0.1:1/auth/token");
            builder.UseSetting("ServiceIdentity:ClientId", "cashout");
            builder.UseSetting("ServiceIdentity:ClientSecret", "test");
            builder.ConfigureServices(services =>
            {
                services.UseTestJwt();
                services.RemoveAll<IOfferPrices>();
                services.RemoveAll<ISettlementCashout>();
                services.AddSingleton<IOfferPrices>(Offer);
                services.AddSingleton<ISettlementCashout, FakeSettlement>();
            });
        }
    }

    private sealed class FakeOffer : IOfferPrices
    {
        public decimal Odds { get; set; } = 2.50m;

        public Task<LivePrice> GetAsync(string fixtureId, string marketId, string selectionId, CancellationToken cancellationToken) => Task.FromResult(new LivePrice(Odds));
    }

    private sealed class FakeSettlement : ISettlementCashout
    {
        public Task<CouponForCashout?> GetCouponAsync(Guid couponId, CancellationToken cancellationToken) =>
            Task.FromResult<CouponForCashout?>(new CouponForCashout(couponId, Punter, 1_000, "ZAR", CouponCashoutState.Open, true,
                [new(Guid.NewGuid(), "fx", "fx-1x2", "home", 3.00m, LegStatus.Open)]));

        public Task<CashOutResult> CashOutAsync(Guid cashoutId, Guid couponId, Guid punterId, long amount, string currency, CancellationToken cancellationToken) =>
            Task.FromResult(new CashOutResult(true, null, true));
    }
}
