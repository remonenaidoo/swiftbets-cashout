using System.Globalization;
using Grpc.Core;
using SwiftBets.BuildingBlocks.Web;
using SwiftBets.Cashout.Application.Ports;
using SwiftBets.Cashout.Domain;
using SwiftBets.Contracts.Grpc.Settlement.Cashout.V1;
using CashoutClient = SwiftBets.Contracts.Grpc.Settlement.Cashout.V1.Cashout.CashoutClient;

namespace SwiftBets.Cashout.Infrastructure;

/// <summary>Settlement's cashout gRPC. A rejected service token is dropped, so the next call fetches a fresh one (D84).</summary>
public sealed class GrpcSettlementCashout(CashoutClient client, ClientCredentialsTokenProvider tokens) : ISettlementCashout
{
    public async Task<CouponForCashout?> GetCouponAsync(Guid couponId, CancellationToken cancellationToken)
    {
        CashoutState state;
        try
        {
            state = await client.GetCashoutStateAsync(new GetCashoutStateRequest { CouponId = couponId.ToString() }, cancellationToken: cancellationToken);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            return null;
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Unauthenticated)
        {
            tokens.Invalidate(await tokens.GetTokenAsync(cancellationToken));
            throw;
        }

        return new CouponForCashout(Guid.Parse(state.CouponId), Guid.Parse(state.PunterId), state.Stake.MinorUnits, state.Stake.Currency,
            state.State switch
            {
                CouponState.Open => CouponCashoutState.Open,
                CouponState.CashedOut => CouponCashoutState.CashedOut,
                _ => CouponCashoutState.Settled,
            },
            state.SingleLine,
            [.. state.Legs.Select(l => new CouponLegForCashout(Guid.Parse(l.LegId), l.FixtureId, l.MarketId, l.SelectionId,
                decimal.Parse(l.PlacedOdds, NumberStyles.Number, CultureInfo.InvariantCulture),
                l.State switch
                {
                    LegState.Won => LegStatus.Won,
                    LegState.Lost => LegStatus.Lost,
                    LegState.Void => LegStatus.Void,
                    _ => LegStatus.Open,
                }))]);
    }

    public async Task<CashOutResult> CashOutAsync(Guid cashoutId, Guid couponId, Guid punterId, long amount, string currency, CancellationToken cancellationToken)
    {
        CashOutReply reply;
        try
        {
            reply = await client.CashOutAsync(new CashOutRequest
            {
                CashoutId = cashoutId.ToString(),
                CouponId = couponId.ToString(),
                PunterId = punterId.ToString(),
                Amount = new Money { MinorUnits = amount, Currency = currency },
            }, cancellationToken: cancellationToken);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Unauthenticated)
        {
            tokens.Invalidate(await tokens.GetTokenAsync(cancellationToken));
            throw;
        }

        return new CashOutResult(reply.Status == CashOutStatus.Accepted, string.IsNullOrEmpty(reply.RefusalCode) ? null : reply.RefusalCode, reply.WasApplied);
    }
}
