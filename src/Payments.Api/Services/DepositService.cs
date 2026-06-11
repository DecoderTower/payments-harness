using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Payments.Api.Domain;
using Payments.Api.Persistence;
using Payments.Api.Providers;

namespace Payments.Api.Services;

/// <summary>
/// The single business-logic path for deposit processing (ADR-003). Both the Stripe and
/// Fake providers flow through this same service.
/// </summary>
public class DepositService(
    PaymentsDbContext db,
    IPaymentProvider provider,
    IOptions<DepositOptions> depositOptions,
    ILogger<DepositService> logger)
{
    private static readonly DepositStatus[] TerminalStatuses =
        [DepositStatus.Succeeded, DepositStatus.Failed, DepositStatus.Cancelled];

    /// <summary>FR-2/FR-3: validates, enforces idempotency, persists, and creates the PaymentIntent.</summary>
    public async Task<Deposit> CreateAsync(CreateDepositCommand command, CancellationToken cancellationToken)
    {
        var errors = Validate(command);
        if (errors.Count > 0)
        {
            throw new DepositValidationException(errors);
        }

        var currency = command.Currency.ToUpperInvariant();
        var fingerprint = ComputeFingerprint(command.AccountReference, command.CustomerReference, command.Amount, currency);

        var existing = await db.Deposits.SingleOrDefaultAsync(d => d.IdempotencyKey == command.IdempotencyKey, cancellationToken);
        if (existing is not null)
        {
            return await HandleReplayAsync(existing, fingerprint, cancellationToken);
        }

        var deposit = new Deposit
        {
            DepositId = Guid.NewGuid(),
            AccountReference = command.AccountReference,
            CustomerReference = command.CustomerReference,
            Amount = command.Amount,
            Currency = currency,
            IdempotencyKey = command.IdempotencyKey,
            PayloadFingerprint = fingerprint,
            Status = DepositStatus.Received,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        db.Deposits.Add(deposit);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Another request won the race to create this idempotency key (NFR-2).
            db.Entry(deposit).State = EntityState.Detached;

            var winner = await db.Deposits.SingleOrDefaultAsync(d => d.IdempotencyKey == command.IdempotencyKey, cancellationToken)
                ?? throw new InvalidOperationException(
                    $"Deposit insert for idempotency key '{command.IdempotencyKey}' failed but no existing deposit was found.");

            return await HandleReplayAsync(winner, fingerprint, cancellationToken);
        }

        LogTransition(deposit, "created");

        await LinkProviderPaymentIntentAsync(deposit, cancellationToken);

        return deposit;
    }

    /// <summary>FR-5: confirms an existing deposit's PaymentIntent.</summary>
    public async Task<Deposit> ConfirmAsync(Guid depositId, ConfirmDepositCommand command, CancellationToken cancellationToken)
    {
        var deposit = await db.Deposits.SingleOrDefaultAsync(d => d.DepositId == depositId, cancellationToken)
            ?? throw new DepositNotFoundException(depositId);

        if (TerminalStatuses.Contains(deposit.Status))
        {
            throw new TerminalDepositException(
                $"Deposit '{depositId}' is in terminal status '{deposit.Status}' and cannot be confirmed.");
        }

        if (deposit.ProviderReference is null)
        {
            throw new DepositNotReadyException(
                $"Deposit '{depositId}' does not yet have a provider reference and cannot be confirmed.");
        }

        deposit.ConfirmationAttemptedAt = DateTimeOffset.UtcNow;

        var providerIntent = await provider.ConfirmPaymentIntentAsync(
            deposit.ProviderReference,
            new ConfirmPaymentIntentRequest(command.PaymentMethodReference, command.ReturnUrl),
            cancellationToken);

        ApplyProviderIntent(deposit, providerIntent);

        await db.SaveChangesAsync(cancellationToken);

        LogTransition(deposit, "confirmed");

        return deposit;
    }

    /// <summary>FR-9: retrieves the persisted deposit by id without calling the provider.</summary>
    public async Task<Deposit?> GetAsync(Guid depositId, CancellationToken cancellationToken) =>
        await db.Deposits.AsNoTracking().SingleOrDefaultAsync(d => d.DepositId == depositId, cancellationToken);

    /// <summary>FR-10: fetches the latest provider state and updates the internal record.</summary>
    public async Task<Deposit> ReconcileAsync(Guid depositId, CancellationToken cancellationToken)
    {
        var deposit = await db.Deposits.SingleOrDefaultAsync(d => d.DepositId == depositId, cancellationToken)
            ?? throw new DepositNotFoundException(depositId);

        if (deposit.ProviderReference is null)
        {
            throw new DepositNotReadyException(
                $"Deposit '{depositId}' does not yet have a provider reference and cannot be reconciled.");
        }

        var previousStatus = deposit.Status;

        var providerIntent = await provider.RetrievePaymentIntentAsync(deposit.ProviderReference, cancellationToken);
        ApplyProviderIntent(deposit, providerIntent);
        deposit.ReconciledAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Deposit lifecycle event {Event}: depositId={DepositId} providerReference={ProviderReference} previousStatus={PreviousStatus} status={Status} accountReference={AccountReference} customerReference={CustomerReference}",
            "reconciled", deposit.DepositId, deposit.ProviderReference, previousStatus, deposit.Status, deposit.AccountReference, deposit.CustomerReference);

        return deposit;
    }

    private async Task<Deposit> HandleReplayAsync(Deposit existing, string fingerprint, CancellationToken cancellationToken)
    {
        if (existing.PayloadFingerprint != fingerprint)
        {
            throw new IdempotencyConflictException(
                $"Idempotency key '{existing.IdempotencyKey}' was already used with a different request payload.");
        }

        logger.LogInformation(
            "Deposit lifecycle event {Event}: depositId={DepositId} providerReference={ProviderReference} status={Status} accountReference={AccountReference} customerReference={CustomerReference}",
            "create_replayed", existing.DepositId, existing.ProviderReference, existing.Status, existing.AccountReference, existing.CustomerReference);

        if (existing.ProviderReference is null)
        {
            // The original create did not finish linking a PaymentIntent (ambiguous
            // outcome, NFR-4). Retry the link using the same idempotency key so Stripe
            // does not create a second PaymentIntent.
            await LinkProviderPaymentIntentAsync(existing, cancellationToken);
        }

        return existing;
    }

    private async Task LinkProviderPaymentIntentAsync(Deposit deposit, CancellationToken cancellationToken)
    {
        var providerIntent = await provider.CreatePaymentIntentAsync(
            new CreatePaymentIntentRequest(deposit.Amount, deposit.Currency, deposit.DepositId, deposit.IdempotencyKey),
            cancellationToken);

        ApplyProviderIntent(deposit, providerIntent);

        await db.SaveChangesAsync(cancellationToken);

        LogTransition(deposit, "linked");
    }

    private void ApplyProviderIntent(Deposit deposit, ProviderPaymentIntent intent)
    {
        deposit.ProviderReference = intent.ProviderReference;
        deposit.LastProviderStatus = intent.Status;
        deposit.ClientSecret = intent.ClientSecret;

        deposit.NextActionType = intent.NextAction?.Type;
        deposit.NextActionRedirectUrl = intent.NextAction?.RedirectUrl;

        deposit.LastFailureCode = intent.LastPaymentError?.Code;
        deposit.LastFailureMessage = intent.LastPaymentError?.Message;

        var hasFailedConfirmationAttempt = intent.Status == "requires_payment_method" && intent.LastPaymentError is not null;
        deposit.Status = PaymentIntentStatusMapper.Map(intent.Status, hasFailedConfirmationAttempt);

        if (PaymentIntentStatusMapper.RequiresOperationalAlert(intent.Status))
        {
            logger.LogWarning(
                "Deposit {DepositId} provider PaymentIntent {ProviderReference} returned unsupported status {ProviderStatus}; manual capture is out of scope (ADR-001).",
                deposit.DepositId, deposit.ProviderReference, intent.Status);
        }

        deposit.UpdatedAt = DateTimeOffset.UtcNow;
    }

    private void LogTransition(Deposit deposit, string eventName) =>
        logger.LogInformation(
            "Deposit lifecycle event {Event}: depositId={DepositId} providerReference={ProviderReference} status={Status} accountReference={AccountReference} customerReference={CustomerReference}",
            eventName, deposit.DepositId, deposit.ProviderReference, deposit.Status, deposit.AccountReference, deposit.CustomerReference);

    private List<string> Validate(CreateDepositCommand command)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(command.AccountReference))
        {
            errors.Add("accountReference is required.");
        }

        if (string.IsNullOrWhiteSpace(command.CustomerReference))
        {
            errors.Add("customerReference is required.");
        }

        if (command.Amount <= 0)
        {
            errors.Add("amount must be a positive integer.");
        }

        if (string.IsNullOrWhiteSpace(command.Currency))
        {
            errors.Add("currency is required.");
        }
        else if (!depositOptions.Value.SupportedCurrencies.Contains(command.Currency, StringComparer.OrdinalIgnoreCase))
        {
            errors.Add($"currency '{command.Currency}' is not supported.");
        }

        if (string.IsNullOrWhiteSpace(command.IdempotencyKey))
        {
            errors.Add("idempotencyKey is required.");
        }

        return errors;
    }

    private static string ComputeFingerprint(string accountReference, string customerReference, long amount, string currency)
    {
        var normalized = $"{accountReference}|{customerReference}|{amount}|{currency}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexString(hash);
    }
}
