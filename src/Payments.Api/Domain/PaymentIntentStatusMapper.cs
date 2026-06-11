namespace Payments.Api.Domain;

/// <summary>
/// Maps Stripe PaymentIntent statuses to internal deposit statuses, per
/// docs/specs/stripe-deposit-processing.md section 9.
/// </summary>
public static class PaymentIntentStatusMapper
{
    /// <summary>
    /// Maps a Stripe PaymentIntent status to the internal deposit status.
    /// </summary>
    /// <param name="providerStatus">The Stripe PaymentIntent <c>status</c> value.</param>
    /// <param name="hasFailedConfirmationAttempt">
    /// True when a confirmation attempt has already been made and Stripe returned the
    /// intent to <c>requires_payment_method</c> with failure information
    /// (e.g. <c>last_payment_error</c>).
    /// </param>
    public static DepositStatus Map(string providerStatus, bool hasFailedConfirmationAttempt) =>
        providerStatus switch
        {
            "requires_payment_method" => hasFailedConfirmationAttempt
                ? DepositStatus.Failed
                : DepositStatus.PendingConfirmation,
            "requires_confirmation" => DepositStatus.PendingConfirmation,
            "requires_action" => DepositStatus.ActionRequired,
            "processing" => DepositStatus.Processing,
            "succeeded" => DepositStatus.Succeeded,
            "canceled" => DepositStatus.Cancelled,
            // requires_capture is unsupported: manual capture is out of scope (ADR-001).
            "requires_capture" => DepositStatus.Failed,
            _ => throw new ArgumentOutOfRangeException(
                nameof(providerStatus), providerStatus, "Unrecognized Stripe PaymentIntent status")
        };

    /// <summary>
    /// True when the provider status indicates an unsupported configuration that should
    /// raise an operational alert (spec section 9, <c>requires_capture</c>).
    /// </summary>
    public static bool RequiresOperationalAlert(string providerStatus) =>
        providerStatus == "requires_capture";
}
