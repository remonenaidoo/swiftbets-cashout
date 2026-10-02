using System.Net;
using System.Net.Http.Json;
using SwiftBets.Cashout.Application.Ports;
using SwiftBets.Contracts.Offer;
using SwiftBets.Contracts.Serialization;

namespace SwiftBets.Cashout.Infrastructure;

/// <summary>Live odds from offer's read API: a selection prices only while its fixture is scheduled and its market open.</summary>
public sealed class HttpOfferPrices(HttpClient http) : IOfferPrices
{
    public async Task<LivePrice> GetAsync(string fixtureId, string marketId, string selectionId, CancellationToken cancellationToken)
    {
        using var response = await http.GetAsync(new Uri($"fixtures/{Uri.EscapeDataString(fixtureId)}", UriKind.Relative), cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return new LivePrice(null);
        }

        response.EnsureSuccessStatusCode();
        var fixture = await response.Content.ReadFromJsonAsync<FixtureChangedV1>(ContractJson.Options, cancellationToken);
        var market = fixture?.Markets.FirstOrDefault(m => m.MarketId == marketId);
        var selection = market?.Selections.FirstOrDefault(s => s.SelectionId == selectionId);
        return fixture?.Status == FixtureStatus.Scheduled && market?.Status == MarketStatus.Open && selection is not null
            ? new LivePrice(selection.Odds)
            : new LivePrice(null);
    }
}
