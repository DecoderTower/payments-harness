using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Payments.Api.Contracts;
using Payments.Api.Persistence;
using Payments.Api.Tests.TestInfrastructure;

namespace Payments.Api.Tests.Persistence;

/// <summary>
/// ADR-008: the schema is owned by EF Core migrations applied at startup, and a model
/// change without a migration fails the build's tests.
/// </summary>
public class MigrationTests(PaymentsApiFactory factory) : PaymentsApiTestBase(factory)
{
    [Fact]
    public void Model_has_no_changes_without_a_migration()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();

        Assert.False(
            db.Database.HasPendingModelChanges(),
            "The EF model has changes no migration covers. Add one: dotnet ef migrations add <Name> --project src/Payments.Api --output-dir Persistence/Migrations");
    }

    [Fact]
    public void Fresh_database_has_every_migration_applied()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();

        Assert.Empty(db.Database.GetPendingMigrations());
        Assert.Contains(db.Database.GetAppliedMigrations(), id => id.EndsWith("_Baseline", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Restarting_against_a_migrated_database_is_a_no_op()
    {
        var created = await CreateDepositAsync(accountReference: "acct-restart");

        // A second host over the same database file runs startup migration again.
        using var restarted = Factory.WithWebHostBuilder(_ => { });
        using var client = restarted.CreateClient();

        var reloaded = await client.GetFromJsonAsync<DepositResponse>($"/deposits/{created.DepositId}");
        Assert.NotNull(reloaded);
        Assert.Equal(created.Status, reloaded.Status);
        Assert.Equal("acct-restart", reloaded.AccountReference);

        using var scope = restarted.Services.CreateScope();
        Assert.Empty(scope.ServiceProvider.GetRequiredService<PaymentsDbContext>().Database.GetPendingMigrations());
    }
}
