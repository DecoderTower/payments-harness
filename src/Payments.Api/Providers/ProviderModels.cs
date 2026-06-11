namespace Payments.Api.Providers;

/// <summary>
/// Input for creating a new provider PaymentIntent for a deposit attempt.
/// </summary>
public record CreatePaymentIntentRequest(
    long Amount,
    string Currency,
    Guid DepositId,
    string IdempotencyKey);

/// <summary>
/// Input for confirming an existing provider PaymentIntent.
/// </summary>
public record ConfirmPaymentIntentRequest(
    string PaymentMethodReference,
    string? ReturnUrl);

/// <summary>
/// Provider-side representation of a PaymentIntent, abstracted from the concrete PSP.
/// </summary>
public record ProviderPaymentIntent(
    string ProviderReference,
    string Status,
    string? ClientSecret,
    ProviderNextAction? NextAction,
    ProviderError? LastPaymentError);

/// <summary>
/// Describes additional customer action required to complete the deposit (e.g. 3DS).
/// </summary>
public record ProviderNextAction(string Type, string? RedirectUrl);

/// <summary>
/// Safe-to-store provider failure details for a declined or failed confirmation.
/// </summary>
public record ProviderError(string? Code, string? Message);
