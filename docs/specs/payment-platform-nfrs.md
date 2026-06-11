# Payment Platform Non-Functional Requirements

## 1. Purpose

This document defines the cross-cutting non-functional and behavioural requirements for the Stripe-backed deposit service in this repository.

These requirements are durable context. They apply to all future implementation work for the deposit service unless explicitly superseded by an updated specification or decision record.

The intent is to keep the system operationally credible while preserving a simple implementation model suitable for a narrow deposit-only vertical slice.

## 2. Applicability

These requirements apply to:

- create-deposit operations
- deposit confirmation flows
- deposit retrieval and reconciliation flows
- persistence of deposit state
- integration with Stripe in local development and in later non-test environments

These requirements do not imply CQRS, event sourcing, or other heavy architecture patterns. A straightforward service plus durable persistence model is sufficient, provided it satisfies the behaviours below.

## 3. Requirements

### NFR-1 Idempotency

Create-deposit operations shall require a caller-supplied `idempotencyKey`.

The service shall:

- persist the idempotency key with the normalized create request fingerprint
- return the original result when the same key is replayed with the same payload
- reject reuse of the same key with a different normalized payload
- forward a stable idempotency key to Stripe for provider-side protection when creating the PaymentIntent

Idempotency is mandatory behaviour, not a best-effort optimisation.

### NFR-2 Safe Retry Behaviour

The service shall be safe to retry in the presence of network failure, process restart, or ambiguous client-side timeout.

At minimum:

- retrying `POST /deposits` with the same `idempotencyKey` shall not create a duplicate deposit
- retrying retrieval or reconciliation shall not mutate the deposit in a way that creates duplicate provider-side effects
- retrying confirmation shall operate on the existing `providerReference` and shall never create a second PaymentIntent

Retries shall preserve business identity. A retry is the same deposit attempt, not a new deposit.

### NFR-3 Timeout Policy

All outbound calls to Stripe shall use explicit, finite timeouts.

The implementation shall:

- use a configurable outbound timeout
- avoid waiting indefinitely on Stripe API calls
- propagate timeout outcomes in a way that distinguishes them from clear provider declines or validation failures

For an initial implementation, a total Stripe request timeout of around 10 seconds is acceptable, provided it remains configurable.

### NFR-4 Ambiguous Outcome Handling

The service shall treat some failures as ambiguous rather than assuming the provider operation failed.

Examples include:

- timeout after a request may already have reached Stripe
- connection drop after the provider may already have created or confirmed the PaymentIntent
- process failure after the provider response was received but before the internal state was durably updated

When an ambiguous outcome occurs, the service shall:

- preserve enough state to recover the deposit
- avoid creating a second PaymentIntent for the same logical deposit
- allow the caller to retry safely
- support explicit reconciliation against Stripe to recover the latest truth

Ambiguous outcome handling is an operational concern. It is not a separate business lifecycle state.

### NFR-5 State Durability

Deposit state shall be stored durably.

At minimum, the implementation shall persist:

- core deposit attributes such as `depositId`, `amount`, `currency`, `accountReference`, and `customerReference`
- create-deposit idempotency data
- `providerReference` once known
- current internal `status`
- timestamps for creation and last update

The system shall not rely on in-memory state for correctness.

Before a successful response is returned for a state-changing operation, the corresponding deposit state shall be persisted.

A simple persistent record plus transition timestamps is sufficient for this phase. No event-sourcing model is required.

### NFR-6 Observability

The service shall produce structured logs and, where useful, explicit lifecycle events for deposit processing.

Observability shall make it possible to answer:

- which `depositId` corresponds to a given Stripe `providerReference`
- whether a create or confirm operation was replayed idempotently
- whether a deposit is pending, succeeded, failed, or awaiting reconciliation
- what the latest known provider state was

At minimum, logs or events for deposit lifecycle changes shall include:

- `depositId`
- `providerReference` when known
- `status`
- `accountReference`
- `customerReference`
- correlation or trace identifier for the request

Sensitive values such as secrets, raw payment instrument details, and full client secrets shall not be logged.

### NFR-7 Error Classification

The service shall classify errors into operationally meaningful categories.

At minimum, the implementation shall distinguish between:

- validation errors
- idempotency conflicts
- provider business failures such as card decline or failed authentication
- provider transient failures such as timeout or temporary unavailability
- internal processing errors
- ambiguous outcomes requiring retry or reconciliation

Provider business failures shall not be reported as internal server failures. They are expected domain outcomes and shall be surfaced as such.

### NFR-8 Data Protection

The service shall minimise the handling of sensitive payment data.

Specifically:

- primary account numbers, CVC values, and equivalent raw card data shall not pass through or be stored by the service
- Stripe client-side collection or Stripe-managed tokens and references shall be used where payment method details are needed
- Stripe secret keys, webhook secrets, and similar credentials shall be supplied through configuration and secret management, not committed to the repository
- `clientSecret` values shall be treated as sensitive operational data and shall not be written to general-purpose logs

The deposit service may store business identifiers such as `accountReference` and `customerReference`, but should avoid storing unnecessary personal data.

### NFR-9 Monetary Handling

Amounts shall be represented and persisted in minor units as integers.

The implementation shall:

- reject decimal floating-point amount inputs for the core API
- validate currency compatibility with the amount representation
- preserve the exact requested amount and currency throughout processing

No amount arithmetic shall rely on binary floating-point types.

### NFR-10 Async and Webhook Readiness

The system shall be ready for asynchronous provider updates even if the first implementation relies primarily on synchronous API calls and explicit reconciliation.

This means:

- the internal status model shall allow deposits to remain in `ACTION_REQUIRED` or `PROCESSING`
- the implementation shall not assume that every deposit reaches a terminal state during the initial request/response cycle
- future webhook handling shall be able to update the same deposit record shape without changing the core domain model

Webhook support is not required in this phase, but the design shall not block it.

### NFR-11 Configuration-Driven Environment Selection

Stripe environment selection shall be configuration-driven.

The implementation shall:

- use environment-specific API keys, account identifiers, webhook secrets, and related values supplied through configuration
- keep the same internal deposit behaviour model across local development and later non-test environments
- avoid environment-specific business logic branches
- avoid a separate demo-mode or test-only code path for deposit processing

Local development shall use Stripe test credentials, including test mode or sandbox credentials as appropriate. Moving beyond those environments shall require configuration changes only, not code changes.

### NFR-12 Stripe API Versioning

The integration shall use an explicit Stripe API version strategy.

At minimum:

- the project shall pin or intentionally select a Stripe API version rather than relying on an unmanaged default
- upgrades to the Stripe API version shall be reviewed as an explicit change
- if webhook endpoints are added later, their API version shall be aligned intentionally with the service integration

The implementation should record the chosen Stripe API version in configuration or deployment metadata so it can be audited during incidents and upgrades.
