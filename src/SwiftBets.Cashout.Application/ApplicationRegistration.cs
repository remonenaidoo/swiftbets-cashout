using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SwiftBets.Cashout.Domain;

namespace SwiftBets.Cashout.Application;

public static class ApplicationRegistration
{
    public static IServiceCollection AddCashoutApplication(this IServiceCollection services)
    {
        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<CashoutOptions>>().Value;
            return new QuoteSigner(Convert.FromBase64String(options.SigningKey), TimeSpan.FromSeconds(options.QuoteMaxAgeSeconds));
        });
        services.AddScoped<CashoutPricing>();
        services.AddScoped<QuoteCashoutHandler>();
        services.AddScoped<ExecuteCashoutHandler>();
        return services;
    }
}
