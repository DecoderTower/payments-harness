namespace Payments.Api.Providers;

/// <summary>
/// Stripe environment configuration (ADR-003, NFR-3, NFR-11, NFR-12). Supplied via
/// configuration / user-secrets, never hardcoded.
/// </summary>
public class StripeOptions
{
    public const string SectionName = "Stripe";

    public string ApiKey { get; set; } = "";

    /// <summary>
    /// The Stripe API version chosen deliberately (NFR-12, ADR-006). Must equal the version
    /// the Stripe.net SDK is built for; the Stripe provider refuses to start otherwise.
    /// </summary>
    public string ApiVersion { get; set; } = "";

    /// <summary>Total outbound request timeout for Stripe calls (NFR-3).</summary>
    public int TimeoutSeconds { get; set; } = 10;
}
