namespace Payments.Api.Providers;

/// <summary>
/// Provider abstraction for creating, confirming, and retrieving PaymentIntents.
/// Both the Stripe and Fake implementations behave equivalently and share the
/// same business-logic path (ADR-003).
/// </summary>
public interface IPaymentProvider
{
    Task<ProviderPaymentIntent> CreatePaymentIntentAsync(
        CreatePaymentIntentRequest request, CancellationToken cancellationToken);

    Task<ProviderPaymentIntent> ConfirmPaymentIntentAsync(
        string providerReference, ConfirmPaymentIntentRequest request, CancellationToken cancellationToken);

    Task<ProviderPaymentIntent> RetrievePaymentIntentAsync(
        string providerReference, CancellationToken cancellationToken);
}
