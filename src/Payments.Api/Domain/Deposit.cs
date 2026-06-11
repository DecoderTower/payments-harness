namespace Payments.Api.Domain;

/// <summary>
/// Durable internal deposit record, per docs/specs/stripe-deposit-processing.md section 5.
/// </summary>
public class Deposit
{
    public Guid DepositId { get; set; }
    public required string AccountReference { get; set; }
    public required string CustomerReference { get; set; }
    public long Amount { get; set; }
    public required string Currency { get; set; }
    public required string IdempotencyKey { get; set; }
    public required string PayloadFingerprint { get; set; }
    public string? ProviderReference { get; set; }
    public DepositStatus Status { get; set; }

    public string? LastProviderStatus { get; set; }
    public string? LastFailureCode { get; set; }
    public string? LastFailureMessage { get; set; }
    public string? ClientSecret { get; set; }
    public string? NextActionType { get; set; }
    public string? NextActionRedirectUrl { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? ConfirmationAttemptedAt { get; set; }
    public DateTimeOffset? ReconciledAt { get; set; }
}
