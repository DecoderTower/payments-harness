namespace Payments.Api.Providers;

/// <summary>
/// Provider abstraction for creating, confirming, retrieving, and cancelling PaymentIntents.
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

    /// <summary>
    /// Cancels an existing PaymentIntent and returns it as the provider reports it afterwards.
    /// When the provider refuses because the intent is not in a cancellable state (including
    /// already cancelled), the current intent is returned rather than an error being thrown.
    /// </summary>
    Task<ProviderPaymentIntent> CancelPaymentIntentAsync(
        string providerReference, CancellationToken cancellationToken);
}
