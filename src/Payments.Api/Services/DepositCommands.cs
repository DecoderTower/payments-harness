namespace Payments.Api.Services;

/// <summary>Input for <see cref="DepositService.CreateAsync"/> (spec section 7.1).</summary>
public record CreateDepositCommand(
    string AccountReference,
    string CustomerReference,
    long Amount,
    string Currency,
    string IdempotencyKey);

/// <summary>Input for <see cref="DepositService.ConfirmAsync"/> (spec section 7.2).</summary>
public record ConfirmDepositCommand(
    string PaymentMethodReference,
    string? ReturnUrl);
