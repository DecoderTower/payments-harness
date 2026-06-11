using ArchUnitNET.Domain;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnit;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace Payments.Api.ArchitectureTests;

/// <summary>
/// Enforces the dependency rules defined in docs/Architecture.md (ADR-004).
/// Each test corresponds to one numbered rule in that document.
/// </summary>
public class ArchitectureRulesTests
{
    private static readonly Architecture Architecture =
        new ArchLoader().LoadAssemblies(typeof(Program).Assembly).Build();

    private static IObjectProvider<IType> Layer(string name) =>
        Types().That().ResideInNamespaceMatching($@"^Payments\.Api\.{name}$").As(name);

    private static readonly IObjectProvider<IType> Domain = Layer("Domain");
    private static readonly IObjectProvider<IType> Contracts = Layer("Contracts");
    private static readonly IObjectProvider<IType> Endpoints = Layer("Endpoints");
    private static readonly IObjectProvider<IType> Middleware = Layer("Middleware");
    private static readonly IObjectProvider<IType> Persistence = Layer("Persistence");
    private static readonly IObjectProvider<IType> Providers = Layer("Providers");
    private static readonly IObjectProvider<IType> Services = Layer("Services");

    // External frameworks are referenced but not loaded into the Architecture, so they
    // must be matched as dependency targets by namespace pattern, not via Types().
    private const string StripeSdk = @"^Stripe(\.|$)";
    private const string EfCore = @"^Microsoft\.EntityFrameworkCore(\.|$)";
    private const string AspNetCore = @"^Microsoft\.AspNetCore(\.|$)";

    [Fact]
    public void Rule1_Domain_DependsOnNoOtherLayerAndNoInfrastructure()
    {
        Types().That().Are(Domain)
            .Should().NotDependOnAny(Contracts)
            .AndShould().NotDependOnAny(Endpoints)
            .AndShould().NotDependOnAny(Middleware)
            .AndShould().NotDependOnAny(Persistence)
            .AndShould().NotDependOnAny(Providers)
            .AndShould().NotDependOnAny(Services)
            .AndShould().NotDependOnAnyTypesThat().ResideInNamespaceMatching(EfCore)
            .AndShould().NotDependOnAnyTypesThat().ResideInNamespaceMatching(AspNetCore)
            .AndShould().NotDependOnAnyTypesThat().ResideInNamespaceMatching(StripeSdk)
            .Check(Architecture);
    }

    [Fact]
    public void Rule2_Contracts_DependOnlyOnDomain()
    {
        Types().That().Are(Contracts)
            .Should().NotDependOnAny(Endpoints)
            .AndShould().NotDependOnAny(Middleware)
            .AndShould().NotDependOnAny(Persistence)
            .AndShould().NotDependOnAny(Providers)
            .AndShould().NotDependOnAny(Services)
            .Check(Architecture);
    }

    [Fact]
    public void Rule3_Endpoints_ReachPersistenceAndProvidersOnlyThroughServices()
    {
        Types().That().Are(Endpoints)
            .Should().NotDependOnAny(Persistence)
            .AndShould().NotDependOnAny(Providers)
            .AndShould().NotDependOnAny(Middleware)
            .Check(Architecture);
    }

    [Fact]
    public void Rule4_Services_DoNotDependOnTheHttpEdge()
    {
        Types().That().Are(Services)
            .Should().NotDependOnAny(Endpoints)
            .AndShould().NotDependOnAny(Middleware)
            .AndShould().NotDependOnAny(Contracts)
            .Check(Architecture);
    }

    [Fact]
    public void Rule5_Persistence_DependsOnlyOnDomain()
    {
        Types().That().Are(Persistence)
            .Should().NotDependOnAny(Contracts)
            .AndShould().NotDependOnAny(Endpoints)
            .AndShould().NotDependOnAny(Middleware)
            .AndShould().NotDependOnAny(Providers)
            .AndShould().NotDependOnAny(Services)
            .Check(Architecture);
    }

    [Fact]
    public void Rule6_Providers_AreSelfContained()
    {
        Types().That().Are(Providers)
            .Should().NotDependOnAny(Contracts)
            .AndShould().NotDependOnAny(Domain)
            .AndShould().NotDependOnAny(Endpoints)
            .AndShould().NotDependOnAny(Middleware)
            .AndShould().NotDependOnAny(Persistence)
            .AndShould().NotDependOnAny(Services)
            .Check(Architecture);
    }

    [Fact]
    public void Rule7_OnlyProviders_DependOnTheStripeSdk()
    {
        Types().That().ResideInNamespaceMatching(@"^Payments\.Api\.(?!Providers$)")
            .Should().NotDependOnAnyTypesThat().ResideInNamespaceMatching(StripeSdk)
            .Check(Architecture);
    }

    [Fact]
    public void Rule8_EfCore_StaysBehindTheServiceBoundary()
    {
        Types().That().ResideInNamespaceMatching(@"^Payments\.Api\.(?!Services$|Persistence$)")
            .Should().NotDependOnAnyTypesThat().ResideInNamespaceMatching(EfCore)
            .Check(Architecture);
    }
}
