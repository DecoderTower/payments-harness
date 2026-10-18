using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Payments.Api.Providers;
using Payments.Api.Tests.TestInfrastructure;

namespace Payments.Api.Tests.Providers;

/// <summary>
/// NFR-12 / ADR-006: the Stripe API version is recorded in configuration and verified against
/// the SDK, so an SDK upgrade that moves the API version cannot pass unnoticed.
/// </summary>
public class StripeApiVersionTests(PaymentsApiFactory factory) : PaymentsApiTestBase(factory)
{
    [Fact]
    public void NFR12_RecordedStripeApiVersionMatchesSdk()
    {
        var recorded = Factory.Services.GetRequiredService<IOptions<StripeOptions>>().Value.ApiVersion;

        Assert.Equal(StripePaymentProvider.SdkApiVersion, recorded);
    }

    [Fact]
    public void NFR12_StripeProviderRefusesToStartOnApiVersionMismatch()
    {
        var options = Options.Create(new StripeOptions { ApiKey = "sk_test_unused", ApiVersion = "2026-08-26.dahlia" });

        var error = Assert.Throws<InvalidOperationException>(
            () => new StripePaymentProvider(options, NullLogger<StripePaymentProvider>.Instance));

        Assert.Contains("2026-08-26.dahlia", error.Message);
        Assert.Contains(StripePaymentProvider.SdkApiVersion, error.Message);
    }

    [Fact]
    public void NFR12_StripeProviderStartsWhenApiVersionsMatch()
    {
        var options = Options.Create(new StripeOptions { ApiKey = "sk_test_unused", ApiVersion = StripePaymentProvider.SdkApiVersion });

        var provider = new StripePaymentProvider(options, NullLogger<StripePaymentProvider>.Instance);

        Assert.NotNull(provider);
    }
}
