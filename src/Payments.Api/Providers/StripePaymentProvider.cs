using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;

namespace Payments.Api.Providers;

/// <summary>
/// <see cref="IPaymentProvider"/> implementation backed by the Stripe PaymentIntent API
/// (ADR-001), selected via <c>PaymentProvider:Type = Stripe</c> (ADR-003).
/// </summary>
public class StripePaymentProvider : IPaymentProvider
{
    private readonly PaymentIntentService _paymentIntents;
    private readonly StripeOptions _settings;

    /// <summary>The Stripe API version this build of Stripe.net is generated for (ADR-006).</summary>
    public static string SdkApiVersion => StripeConfiguration.ApiVersion;

    public StripePaymentProvider(IOptions<StripeOptions> options, ILogger<StripePaymentProvider> logger)
    {
        _settings = options.Value;

        // NFR-12 / ADR-006: never run against a Stripe API version nobody recorded.
        if (!string.Equals(_settings.ApiVersion, SdkApiVersion, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Stripe API version mismatch: configuration records '{_settings.ApiVersion}' but Stripe.net is built for " +
                $"'{SdkApiVersion}'. Review the Stripe changelog and update Stripe:ApiVersion deliberately (ADR-006).");
        }

        logger.LogInformation("Stripe API version {StripeApiVersion}", SdkApiVersion);

        var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds) };
        var stripeClient = new StripeClient(_settings.ApiKey, httpClient: new SystemNetHttpClient(httpClient));
        _paymentIntents = new PaymentIntentService(stripeClient);
    }

    public async Task<ProviderPaymentIntent> CreatePaymentIntentAsync(
        CreatePaymentIntentRequest request, CancellationToken cancellationToken)
    {
        var options = new PaymentIntentCreateOptions
        {
            Amount = request.Amount,
            Currency = request.Currency.ToLowerInvariant(),
            CaptureMethod = "automatic",
            Metadata = new Dictionary<string, string>
            {
                ["depositId"] = request.DepositId.ToString(),
            },
        };

        try
        {
            var intent = await _paymentIntents.CreateAsync(
                options, BuildRequestOptions(request.IdempotencyKey), cancellationToken);
            return ToProviderPaymentIntent(intent);
        }
        catch (StripeException ex) when (IsTransient(ex))
        {
            throw new ProviderTransientException("Stripe PaymentIntent creation failed transiently.", ex);
        }
    }

    public async Task<ProviderPaymentIntent> ConfirmPaymentIntentAsync(
        string providerReference, ConfirmPaymentIntentRequest request, CancellationToken cancellationToken)
    {
        var options = new PaymentIntentConfirmOptions
        {
            PaymentMethod = request.PaymentMethodReference,
            ReturnUrl = request.ReturnUrl,
        };

        try
        {
            var intent = await _paymentIntents.ConfirmAsync(
                providerReference, options, BuildRequestOptions(), cancellationToken);
            return ToProviderPaymentIntent(intent);
        }
        catch (StripeException ex) when (ex.StripeError?.PaymentIntent is not null)
        {
            // Declines surface as a non-2xx response with the updated PaymentIntent
            // attached; this is a normal FAILED outcome, not a transient error.
            return ToProviderPaymentIntent(ex.StripeError.PaymentIntent);
        }
        catch (StripeException ex) when (IsTransient(ex))
        {
            throw new ProviderTransientException("Stripe PaymentIntent confirmation failed transiently.", ex);
        }
    }

    public async Task<ProviderPaymentIntent> RetrievePaymentIntentAsync(
        string providerReference, CancellationToken cancellationToken)
    {
        try
        {
            var intent = await _paymentIntents.GetAsync(
                providerReference, requestOptions: BuildRequestOptions(), cancellationToken: cancellationToken);
            return ToProviderPaymentIntent(intent);
        }
        catch (StripeException ex) when (IsTransient(ex))
        {
            throw new ProviderTransientException("Stripe PaymentIntent retrieval failed transiently.", ex);
        }
    }

    private static RequestOptions BuildRequestOptions(string? idempotencyKey = null) => new()
    {
        IdempotencyKey = idempotencyKey,
    };

    private static bool IsTransient(StripeException ex) =>
        ex.HttpStatusCode == 0
        || (int)ex.HttpStatusCode >= 500
        || ex.HttpStatusCode == System.Net.HttpStatusCode.TooManyRequests;

    private static ProviderPaymentIntent ToProviderPaymentIntent(PaymentIntent intent)
    {
        ProviderNextAction? nextAction = intent.NextAction is null
            ? null
            : new ProviderNextAction(intent.NextAction.Type, intent.NextAction.RedirectToUrl?.Url);

        ProviderError? lastPaymentError = intent.LastPaymentError is null
            ? null
            : new ProviderError(
                intent.LastPaymentError.DeclineCode ?? intent.LastPaymentError.Code,
                intent.LastPaymentError.Message);

        return new ProviderPaymentIntent(
            intent.Id,
            intent.Status,
            intent.ClientSecret,
            nextAction,
            lastPaymentError);
    }
}
