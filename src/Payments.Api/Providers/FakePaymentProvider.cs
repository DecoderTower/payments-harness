using System.Collections.Concurrent;

namespace Payments.Api.Providers;

/// <summary>
/// In-memory <see cref="IPaymentProvider"/> simulation selected via
/// <c>PaymentProvider:Type = Fake</c> (ADR-003).
///
/// Confirmation outcomes are driven by the same payment-method tokens Stripe
/// documents for test mode, so behaviour matches real Stripe test mode
/// (spec section 10, AS-10):
/// <list type="bullet">
/// <item><c>pm_card_visa</c> confirms successfully (<c>succeeded</c>)</item>
/// <item><c>pm_card_chargeDeclined</c> is declined (<c>requires_payment_method</c> + <c>last_payment_error</c>)</item>
/// <item><c>pm_card_authenticationRequired</c> requires customer action (<c>requires_action</c>)</item>
/// </list>
/// </summary>
public class FakePaymentProvider : IPaymentProvider
{
    private readonly ConcurrentDictionary<string, FakeIntentState> _intents = new();
    private readonly ConcurrentDictionary<string, string> _idempotencyKeys = new();
    private int _failNextCreateCount;

    public Task<ProviderPaymentIntent> CreatePaymentIntentAsync(
        CreatePaymentIntentRequest request, CancellationToken cancellationToken)
    {
        // Mirrors Stripe's idempotency-key behaviour: a retried create for a key that
        // already produced a PaymentIntent returns that same intent rather than a new
        // one, so NFR-4 recovery cannot create duplicates.
        var providerReference = _idempotencyKeys.GetOrAdd(request.IdempotencyKey, _ =>
        {
            var reference = $"pi_fake_{Guid.NewGuid():N}";
            _intents[reference] = new FakeIntentState
            {
                Status = "requires_payment_method",
                ClientSecret = $"{reference}_secret_{Guid.NewGuid():N}",
            };
            return reference;
        });

        if (Interlocked.Decrement(ref _failNextCreateCount) >= 0)
        {
            throw new ProviderTransientException("Simulated transient failure.", new TimeoutException());
        }

        Interlocked.Increment(ref _failNextCreateCount);

        return Task.FromResult(ToProviderPaymentIntent(providerReference, GetState(providerReference)));
    }

    public Task<ProviderPaymentIntent> ConfirmPaymentIntentAsync(
        string providerReference, ConfirmPaymentIntentRequest request, CancellationToken cancellationToken)
    {
        var state = GetState(providerReference);

        switch (request.PaymentMethodReference)
        {
            case "pm_card_visa":
                state.Status = "succeeded";
                state.LastPaymentErrorCode = null;
                state.LastPaymentErrorMessage = null;
                break;

            case "pm_card_chargeDeclined":
                state.Status = "requires_payment_method";
                state.LastPaymentErrorCode = "card_declined";
                state.LastPaymentErrorMessage = "Your card was declined.";
                break;

            case "pm_card_authenticationRequired":
                state.Status = "requires_action";
                state.NextActionType = "redirect_to_url";
                state.NextActionRedirectUrl = $"https://fake-stripe.test/3ds/{providerReference}";
                break;

            default:
                throw new ArgumentException(
                    $"Unrecognized fake payment method reference '{request.PaymentMethodReference}'.",
                    nameof(request));
        }

        return Task.FromResult(ToProviderPaymentIntent(providerReference, state));
    }

    public Task<ProviderPaymentIntent> RetrievePaymentIntentAsync(
        string providerReference, CancellationToken cancellationToken)
    {
        var state = GetState(providerReference);

        return Task.FromResult(ToProviderPaymentIntent(providerReference, state));
    }

    /// <summary>
    /// Test-only seam: directly advances the simulated PaymentIntent to a new status,
    /// e.g. to simulate Stripe completing asynchronous processing before reconciliation.
    /// </summary>
    public void SetStatus(string providerReference, string status)
    {
        GetState(providerReference).Status = status;
    }

    /// <summary>
    /// Test-only seam: makes the next <paramref name="times"/> calls to
    /// <see cref="CreatePaymentIntentAsync"/> throw <see cref="ProviderTransientException"/>,
    /// to simulate an ambiguous outcome during create (NFR-4).
    /// </summary>
    public void FailNextCreate(int times = 1) => Interlocked.Exchange(ref _failNextCreateCount, times);

    private FakeIntentState GetState(string providerReference)
    {
        if (!_intents.TryGetValue(providerReference, out var state))
        {
            throw new KeyNotFoundException($"Unknown PaymentIntent '{providerReference}'.");
        }

        return state;
    }

    private static ProviderPaymentIntent ToProviderPaymentIntent(string providerReference, FakeIntentState state) =>
        new(
            providerReference,
            state.Status,
            state.ClientSecret,
            state.NextActionType is null ? null : new ProviderNextAction(state.NextActionType, state.NextActionRedirectUrl),
            state.LastPaymentErrorCode is null && state.LastPaymentErrorMessage is null
                ? null
                : new ProviderError(state.LastPaymentErrorCode, state.LastPaymentErrorMessage));

    private sealed class FakeIntentState
    {
        public required string Status { get; set; }
        public string? ClientSecret { get; set; }
        public string? NextActionType { get; set; }
        public string? NextActionRedirectUrl { get; set; }
        public string? LastPaymentErrorCode { get; set; }
        public string? LastPaymentErrorMessage { get; set; }
    }
}
