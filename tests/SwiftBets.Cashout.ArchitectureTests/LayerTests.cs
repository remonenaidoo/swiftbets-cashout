using System.Reflection;
using NetArchTest.Rules;

namespace SwiftBets.Cashout.ArchitectureTests;

public sealed class LayerTests
{
    private static readonly Assembly Domain = typeof(SwiftBets.Cashout.Domain.CashoutPricer).Assembly;
    private static readonly Assembly Application = typeof(SwiftBets.Cashout.Application.ApplicationRegistration).Assembly;

    [Fact]
    public void Domain_depends_on_nothing_else_in_the_solution() =>
        Types.InAssembly(Domain).ShouldNot().HaveDependencyOnAny("SwiftBets.Cashout.Application", "SwiftBets.Cashout.Infrastructure", "SwiftBets.BuildingBlocks", "Microsoft.AspNetCore", "Grpc")
            .GetResult().IsSuccessful.ShouldBeTrue();

    [Fact]
    public void Application_does_not_depend_on_infrastructure() =>
        Types.InAssembly(Application).ShouldNot().HaveDependencyOnAny("SwiftBets.Cashout.Infrastructure", "Grpc", "System.Net.Http")
            .GetResult().IsSuccessful.ShouldBeTrue();
}
