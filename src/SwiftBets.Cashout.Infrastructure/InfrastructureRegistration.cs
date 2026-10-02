using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SwiftBets.BuildingBlocks.Core;
using SwiftBets.BuildingBlocks.Resilience;
using SwiftBets.BuildingBlocks.Web;
using SwiftBets.Cashout.Application;
using SwiftBets.Cashout.Application.Ports;
using CashoutClient = SwiftBets.Contracts.Grpc.Settlement.Cashout.V1.Cashout.CashoutClient;

namespace SwiftBets.Cashout.Infrastructure;

public static class InfrastructureRegistration
{
    public static IServiceCollection AddCashoutInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddValidatedOptions<CashoutOptions>(configuration, CashoutOptions.SectionName);
        services.AddValidatedOptions<CashoutClients>(configuration, CashoutClients.SectionName);
        services.AddClientCredentials(configuration);

        services.AddGrpcClient<CashoutClient>((sp, grpc) => grpc.Address = new Uri(sp.GetRequiredService<IOptions<CashoutClients>>().Value.SettlementGrpcAddress))
            .ConfigureChannel(channel => channel.UnsafeUseInsecureChannelCallCredentials = true)
            .AddCallCredentials(async (context, metadata, sp) =>
                metadata.Add("Authorization", $"Bearer {await sp.GetRequiredService<ClientCredentialsTokenProvider>().GetTokenAsync(context.CancellationToken)}"))
            .AddKeyedGrpcResilience();
        services.AddScoped<ISettlementCashout, GrpcSettlementCashout>();

        services.AddHttpClient<IOfferPrices, HttpOfferPrices>((sp, http) =>
                http.BaseAddress = new Uri(sp.GetRequiredService<IOptions<CashoutClients>>().Value.OfferAddress.TrimEnd('/') + "/"))
            .AddIdempotentResilience();
        return services;
    }
}
