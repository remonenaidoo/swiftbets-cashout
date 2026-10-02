using SwiftBets.BuildingBlocks.Web;
using SwiftBets.Cashout.Application;
using SwiftBets.Contracts.Errors;
using SwiftBets.Contracts.Serialization;

namespace SwiftBets.Cashout.Api;

public static class CashoutEndpoints
{
    public sealed record QuoteRequest(Guid CouponId);

    public sealed record ExecuteRequest(string QuoteToken);

    public static IEndpointRouteBuilder MapCashoutEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var cashout = endpoints.MapGroup("/cashout").RequireAuthorization(Roles.Punter);

        cashout.MapPost("/quote", async (QuoteRequest request, QuoteCashoutHandler handler, HttpContext context, CancellationToken cancellationToken) =>
        {
            var offer = await handler.HandleAsync(request.CouponId, PunterId(context), cancellationToken);
            return offer.IsSuccess ? Results.Json(offer.Value, ContractJson.Options) : offer.ToHttpResult(context);
        });

        cashout.MapPost("/execute", async (ExecuteRequest request, ExecuteCashoutHandler handler, HttpContext context, CancellationToken cancellationToken) =>
        {
            var (done, refused) = await handler.HandleAsync(request.QuoteToken, PunterId(context), cancellationToken);
            if (done is not null)
            {
                return Results.Json(done, ContractJson.Options);
            }

            // A stale or moved quote comes back with a fresh one, so the customer can confirm the new amount.
            return refused!.FreshQuote is { } fresh ? Refused(context, refused.Error.Code, refused.Error.Message, fresh) : refused.Error.ToHttpResult(context);
        });

        return endpoints;
    }

    /// <summary>The standard error envelope (problem+json) carrying the fresh quote as an extension.</summary>
    private static IResult Refused(HttpContext context, string code, string message, CashoutOffer fresh)
    {
        var e = ErrorEnvelopes.Create(context, StatusCodes.Status409Conflict, code, message);
        return Results.Json(new { e.Type, e.Title, e.Status, e.Code, e.CorrelationId, e.Detail, e.Instance, freshQuote = fresh }, ContractJson.Options, ErrorEnvelope.MediaType, e.Status);
    }

    private static Guid PunterId(HttpContext context) =>
        Guid.TryParse(context.User.FindFirst("sub")?.Value, out var id) ? id : Guid.Empty;
}
