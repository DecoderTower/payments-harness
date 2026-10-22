using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Payments.Api.Providers;

namespace Payments.Api.Tests.TestInfrastructure;

/// <summary>
/// Hosts the API with the Fake payment provider (ADR-003) and a temp SQLite database
/// per test class.
/// </summary>
public class PaymentsApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"payments-tests-{Guid.NewGuid():N}.db");

    /// <summary>Captured log output, for tests asserting lifecycle events (NFR-6).</summary>
    public CapturingLoggerProvider Logs { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureLogging(logging => logging.AddProvider(Logs));

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PaymentsDb"] = $"Data Source={_dbPath}",
                ["PaymentProvider:Type"] = "Fake",
            });
        });
    }

    /// <summary>The Fake provider singleton, for tests that need to manipulate simulated provider state.</summary>
    public FakePaymentProvider FakeProvider => (FakePaymentProvider)Services.GetRequiredService<IPaymentProvider>();

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing && File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }
}
