using Payments.Api.Domain;

namespace Payments.Api.Contracts;

/// <summary>POST /deposits request body (spec section 7.1).</summary>
public record CreateDepositRequest(
    string AccountReference,
    string CustomerReference,
    long Amount,
    string Currency,
    string IdempotencyKey);

/// <summary>POST /deposits/{depositId}/confirm request body (spec section 7.2).</summary>
public record ConfirmDepositRequest(
    string PaymentMethodReference,
    string? ReturnUrl);

/// <summary>Describes additional customer action required to complete the deposit.</summary>
public record NextActionResponse(string Type, string? RedirectUrl);

/// <summary>Deposit representation returned by all endpoints (spec sections 7.1-7.4).</summary>
public record DepositResponse(
    Guid DepositId,
    string AccountReference,
    string CustomerReference,
    long Amount,
    string Currency,
    string? ProviderReference,
    DepositStatus Status,
    string? ClientSecret,
    NextActionResponse? NextAction,
    string? FailureCode,
    string? FailureMessage)
{
    public static DepositResponse FromDeposit(Deposit deposit) => new(
        deposit.DepositId,
        deposit.AccountReference,
        deposit.CustomerReference,
        deposit.Amount,
        deposit.Currency,
        deposit.ProviderReference,
        deposit.Status,
        deposit.ClientSecret,
        deposit.NextActionType is null ? null : new NextActionResponse(deposit.NextActionType, deposit.NextActionRedirectUrl),
        deposit.LastFailureCode,
        deposit.LastFailureMessage);
}
