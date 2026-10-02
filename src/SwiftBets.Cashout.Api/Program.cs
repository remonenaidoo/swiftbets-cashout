using SwiftBets.BuildingBlocks.Observability;
using SwiftBets.BuildingBlocks.Web;
using SwiftBets.Cashout.Api;
using SwiftBets.Cashout.Application;
using SwiftBets.Cashout.Infrastructure;

if (HealthProbe.TryRun(args) is { } probeExitCode)
{
    return probeExitCode;
}

var builder = WebApplication.CreateBuilder(args);
builder.AddSwiftBetsObservability("swiftbets-cashout");
builder.Services.AddSwiftBetsWeb();
builder.Services.AddSwiftBetsJwtBearer(builder.Configuration);
builder.Services.AddCashoutInfrastructure(builder.Configuration);
builder.Services.AddCashoutApplication();

var app = builder.Build();
app.UseSwiftBetsObservability();
app.UseSwiftBetsWeb();
app.UseAuthentication();
app.UseAuthorization();
app.MapSwiftBetsOperationalEndpoints();
app.MapCashoutEndpoints();

await app.RunAsync();
return 0;

public partial class Program;
