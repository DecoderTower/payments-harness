# Stripe Deposit Processing Specification

## 1. Purpose

This specification defines the functional requirements for a deposit service that accepts customer deposits into an internal wallet or account and uses Stripe PaymentIntents as the payment provider abstraction.

The document is intended to be durable context for implementation. It describes the deposit flow, the service-facing API expectations, the internal deposit state model, and the required behaviour when Stripe returns successful, authentication-required, or failed outcomes.

## 2. Scope

This specification applies only to deposits.

In scope:

- creating a deposit request
- validating deposit input
- creating a Stripe PaymentIntent
- persisting and retrieving an internal deposit record
- confirming a deposit against an existing PaymentIntent
- handling successful, authentication-required, processing, cancelled, and failed deposit outcomes
- reconciling latest provider state from Stripe on demand
- emitting lifecycle events and structured logs for deposit processing

Out of scope:

- refunds
- withdrawals
- disputes
- subscriptions
- payouts
- partial capture
- multi-PSP routing
- stored-value ledger behaviour beyond referencing an `accountReference`

For local development, the service is expected to run against Stripe testing environments such as test mode or a sandbox. Those environments are not separate business flows. The deposit lifecycle and service behaviour defined in this document apply consistently across environments, with environment-specific credentials and settings supplied through configuration.

## 3. Actors

- `Caller`: the upstream system invoking the deposit service API.
- `Customer`: the end user funding a wallet or account.
- `Deposit service`: the system specified by this document.
- `Stripe`: the external payment service provider used to create, confirm, and retrieve PaymentIntents.
- `Operator`: an engineer or support process inspecting logs, events, or reconciliation outcomes.

## 4. External Dependency: Stripe

The service shall integrate with Stripe using the PaymentIntent API.

The Stripe PaymentIntent is the provider-side representation of a single deposit attempt. The internal `providerReference` stored by the deposit service shall be the Stripe PaymentIntent identifier, for example `pi_...`.

The service shall assume the same Stripe integration model in local development and in later non-test environments:

- the same PaymentIntent lifecycle is used in every environment
- the same internal deposit statuses are used in every environment
- different environments are selected only by configuration such as API keys, account settings, and webhook secrets

## 5. Deposit Record

Each deposit shall be represented internally by a durable deposit record containing at least:

- `depositId`: service-generated unique identifier for the deposit
- `accountReference`: caller-supplied reference for the wallet or account being funded
- `customerReference`: caller-supplied customer identifier
- `amount`: integer amount in minor units
- `currency`: ISO 4217 currency code
- `idempotencyKey`: caller-supplied key used for create-deposit idempotency
- `providerReference`: Stripe PaymentIntent identifier once known
- `status`: internal deposit status
- `createdAt`: timestamp of initial persistence
- `updatedAt`: timestamp of latest mutation

The implementation may persist additional operational fields such as:

- `lastProviderStatus`
- `lastFailureCode`
- `lastFailureMessage`
- `clientSecret`
- `reconciledAt`
- `confirmationAttemptedAt`

## 6. Deposit Lifecycle

The deposit lifecycle shall follow this high-level sequence:

1. The caller submits a create-deposit request with deposit details and a caller-generated `idempotencyKey`.
2. The deposit service validates the request and reserves the idempotency key against the normalized request payload.
3. The deposit service creates a durable internal deposit record.
4. The deposit service creates a Stripe PaymentIntent for the deposit amount and currency.
5. The deposit service persists the Stripe `providerReference` and the mapped internal status.
6. The caller confirms the deposit for an existing `depositId`.
7. The deposit service updates internal state from the PaymentIntent outcome.
8. The caller or an operator may retrieve the deposit by `depositId`.
9. The caller or an operator may request reconciliation, causing the service to fetch the latest PaymentIntent state from Stripe and update the internal record.

The lifecycle is defined for one deposit attempt per `depositId`. If a deposit reaches `FAILED` or `CANCELLED`, the caller shall create a new deposit to try again rather than reusing the terminal deposit.

## 7. API Expectations

This specification assumes a service-facing HTTP API using JSON. Exact route naming may vary, but the following logical operations are required.

### 7.1 Create Deposit

`POST /deposits`

Expected request fields:

- `accountReference`
- `customerReference`
- `amount`
- `currency`
- `idempotencyKey`

Expected behaviour:

- validates the request
- enforces caller-supplied idempotency
- creates an internal deposit record
- creates a Stripe PaymentIntent
- persists the `providerReference`
- returns the current deposit representation

Expected response fields:

- `depositId`
- `accountReference`
- `customerReference`
- `amount`
- `currency`
- `providerReference` when known
- `status`
- `clientSecret` when the caller needs it for Stripe-managed customer action

### 7.2 Confirm Deposit

`POST /deposits/{depositId}/confirm`

Expected request fields:

- `paymentMethodReference` or equivalent Stripe-confirmation input
- optional caller context such as `returnUrl` when required by the Stripe integration

Expected behaviour:

- loads the existing deposit by `depositId`
- rejects confirmation for terminal deposits
- confirms the existing Stripe PaymentIntent rather than creating a new one
- persists the resulting provider and internal state
- returns the updated deposit representation

Expected response fields:

- `depositId`
- `providerReference`
- `status`
- `clientSecret` when further customer action is required
- `nextAction` summary when applicable
- provider failure details when the confirmation attempt fails

### 7.3 Retrieve Deposit

`GET /deposits/{depositId}`

Expected behaviour:

- returns the persisted internal view of the deposit
- does not call Stripe implicitly
- includes the latest known internal status and provider reference

### 7.4 Reconcile Deposit

`POST /deposits/{depositId}/reconcile`

Expected behaviour:

- fetches the latest PaymentIntent from Stripe using the stored `providerReference`
- maps the Stripe state into the internal deposit state model
- updates the deposit record durably
- returns the refreshed deposit representation

## 8. Internal Status Model

The deposit service shall use the following internal statuses and shall not treat Stripe object states as the public internal status model directly.

### 8.1 `RECEIVED`

The create request has been accepted and the internal record exists, but the Stripe PaymentIntent has not yet been durably linked or the service is still recovering from an in-flight provider operation.

### 8.2 `PENDING_CONFIRMATION`

The deposit exists and is ready to be confirmed against Stripe. A PaymentIntent may be waiting for a payment method, or it may already be confirmable depending on the chosen client flow.

### 8.3 `ACTION_REQUIRED`

The deposit cannot complete until the customer performs additional provider-managed authentication or another required action.

### 8.4 `PROCESSING`

Stripe has accepted the confirmation request and the final outcome is pending asynchronously.

### 8.5 `SUCCEEDED`

The deposit has completed successfully according to Stripe and is considered funded from the payment-service perspective.

### 8.6 `FAILED`

The deposit attempt did not complete successfully and will not proceed further. A new deposit must be created for a new attempt.

### 8.7 `CANCELLED`

The Stripe PaymentIntent has been cancelled and the deposit will not proceed further.

`SUCCEEDED`, `FAILED`, and `CANCELLED` are terminal statuses.

## 9. Mapping from Stripe PaymentIntent Status to Internal Status

The deposit service shall map Stripe PaymentIntent states into internal deposit states as follows.

| Stripe PaymentIntent status | Internal deposit status | Notes |
| --- | --- | --- |
| not yet available | `RECEIVED` | Used only while the service has accepted a request but does not yet have a durably linked provider object. |
| `requires_payment_method` | `PENDING_CONFIRMATION` | Used when the deposit is awaiting its first usable payment method or equivalent confirmation input. |
| `requires_payment_method` with a failed confirmation attempt recorded | `FAILED` | Used when the confirmation attempt has already been made and Stripe returned the intent to `requires_payment_method` with failure information such as `last_payment_error`. |
| `requires_confirmation` | `PENDING_CONFIRMATION` | The intent exists and is ready for confirmation. |
| `requires_action` | `ACTION_REQUIRED` | The customer must complete additional action such as 3DS authentication. |
| `processing` | `PROCESSING` | Final settlement outcome is pending. |
| `succeeded` | `SUCCEEDED` | Deposit completed successfully. |
| `canceled` | `CANCELLED` | Deposit was cancelled and will not continue. |
| `requires_capture` | `FAILED` | This state is unsupported for the deposit service because manual capture is out of scope and must not be configured intentionally. The service shall raise an operational alert if observed. |

## 10. Functional Requirements

### FR-1 Request Validation

The service shall validate create-deposit requests before calling Stripe.

At minimum it shall enforce:

- `accountReference` is present
- `customerReference` is present
- `amount` is a positive integer expressed in minor units
- `currency` is present and supported by the service
- `idempotencyKey` is present for create-deposit requests

If validation fails, the service shall reject the request without creating a Stripe PaymentIntent.

### FR-2 Create Deposit

On a valid create request, the service shall:

1. reserve the `idempotencyKey` against the normalized request payload
2. allocate a unique `depositId`
3. create a durable internal deposit record
4. create a Stripe PaymentIntent for the requested `amount` and `currency`
5. store the PaymentIntent identifier as `providerReference`
6. store Stripe metadata sufficient to correlate the provider object to the internal deposit, including `depositId`
7. return the current deposit representation

The created PaymentIntent shall be configured for deposit charging, not for manual capture or deferred merchant-specific flows that are outside this repository's scope.

### FR-3 Create-Deposit Idempotency

For `POST /deposits`, the service shall require a caller-supplied `idempotencyKey`.

If the same `idempotencyKey` is received again with the same normalized request payload, the service shall return the existing deposit result instead of creating a new PaymentIntent or a new `depositId`.

If the same `idempotencyKey` is received again with a different normalized request payload, the service shall reject the request as an idempotency conflict.

### FR-4 Persist Internal Deposit Record

The service shall persist deposit state durably before reporting the current state as successful to the caller.

At minimum, once create-deposit returns a successful response, the internal deposit record shall contain:

- `depositId`
- `accountReference`
- `customerReference`
- `amount`
- `currency`
- `idempotencyKey`
- `providerReference` if PaymentIntent creation succeeded
- `status`

### FR-5 Confirm Deposit

The service shall support confirmation of an existing deposit by internal `depositId`.

On confirm, the service shall:

- load the deposit using `depositId`
- use the existing stored `providerReference`
- send a confirm request to Stripe for that PaymentIntent
- persist the mapped internal status returned by the provider outcome
- return the updated deposit representation

The confirm operation shall not create a new Stripe PaymentIntent.

### FR-6 Successful Deposit Flow

If Stripe returns a successful PaymentIntent outcome for the deposit, the service shall:

- map the deposit to `SUCCEEDED`
- persist the terminal state durably
- emit a lifecycle event or structured log indicating deposit success
- make the successful state visible through retrieval and reconciliation

### FR-7 Authentication-Required Flow

If Stripe returns `requires_action`, the service shall:

- map the deposit to `ACTION_REQUIRED`
- persist the provider reference and latest status
- return the information required for the caller to continue the customer journey, including `clientSecret` or equivalent next-action information
- allow later retrieval or reconciliation to reflect the updated outcome after the required action completes

### FR-8 Decline or Failure Flow

If Stripe indicates that the deposit attempt failed, including a decline that returns the PaymentIntent to `requires_payment_method` after confirmation, the service shall:

- map the deposit to `FAILED`
- persist provider failure information that is safe to store
- return a domain response that makes the failure visible to the caller
- avoid treating the failure as a successful or processing deposit

### FR-9 Retrieve Deposit by Internal ID

The service shall support retrieval of a deposit by `depositId`.

The returned deposit shall include at least:

- `depositId`
- `accountReference`
- `customerReference`
- `amount`
- `currency`
- `providerReference` when known
- `status`
- any safe failure details relevant to the latest known deposit outcome

### FR-10 Reconcile Latest State from Stripe on Demand

The service shall support an explicit reconciliation operation for a deposit.

When reconciliation is requested, the service shall:

- load the deposit by `depositId`
- require a stored `providerReference`
- retrieve the current PaymentIntent from Stripe
- map the provider state to the internal status model
- update the internal deposit record when Stripe reflects a newer state
- emit a lifecycle event or structured log that reconciliation occurred

### FR-11 Lifecycle Events and Logs

The service shall emit lifecycle events, logs, or both for key deposit transitions.

At minimum, observable signals shall exist for:

- deposit created
- deposit confirmed
- deposit moved to `ACTION_REQUIRED`
- deposit moved to `PROCESSING`
- deposit moved to `SUCCEEDED`
- deposit moved to `FAILED`
- deposit moved to `CANCELLED`
- deposit reconciled from Stripe

These signals shall include correlation fields such as `depositId` and `providerReference` when available.

## 11. Acceptance Scenarios

### AS-1 Create a Valid Deposit

Given a valid create request with `accountReference`, `customerReference`, `amount`, `currency`, and `idempotencyKey`
When the caller creates a deposit
Then the service creates a durable internal deposit record
And the service creates a Stripe PaymentIntent
And the response includes a `depositId`
And the response includes the Stripe PaymentIntent identifier as `providerReference`
And the deposit status is `PENDING_CONFIRMATION`

### AS-2 Reject an Invalid Deposit Request

Given a create request with a missing `accountReference`, unsupported `currency`, non-positive `amount`, or missing `idempotencyKey`
When the caller creates a deposit
Then the service rejects the request
And no Stripe PaymentIntent is created

### AS-3 Replay the Same Create Request Safely

Given a previously accepted create request
And the caller retries `POST /deposits` with the same `idempotencyKey` and the same normalized payload
When the service handles the retry
Then the service returns the original deposit result
And the service does not create a second PaymentIntent
And the service does not allocate a second `depositId`

### AS-4 Detect Idempotency Misuse

Given a previously accepted create request
And the caller retries `POST /deposits` with the same `idempotencyKey` but a different amount, currency, account, or customer payload
When the service handles the retry
Then the service rejects the request as an idempotency conflict
And the original deposit remains unchanged

### AS-5 Confirm a Deposit Successfully

Given an existing deposit in `PENDING_CONFIRMATION`
When the caller confirms the deposit with valid Stripe confirmation input
And Stripe returns `succeeded`
Then the service stores the deposit as `SUCCEEDED`
And retrieval of that `depositId` returns `SUCCEEDED`

### AS-6 Confirm a Deposit That Requires Customer Authentication

Given an existing deposit in `PENDING_CONFIRMATION`
When the caller confirms the deposit
And Stripe returns `requires_action`
Then the service stores the deposit as `ACTION_REQUIRED`
And the response contains the Stripe information needed to continue the customer action
And later reconciliation or retrieval reflects the latest provider outcome

### AS-7 Confirm a Deposit That Is Declined

Given an existing deposit in `PENDING_CONFIRMATION`
When the caller confirms the deposit
And Stripe returns the PaymentIntent to `requires_payment_method` with failure information
Then the service stores the deposit as `FAILED`
And the response indicates the deposit attempt failed
And the caller must create a new deposit for a new attempt

### AS-8 Retrieve a Deposit by Internal Identifier

Given an existing deposit
When the caller retrieves the deposit by `depositId`
Then the service returns the persisted deposit record
And the response includes `depositId`, `amount`, `currency`, `providerReference`, and `status`

### AS-9 Reconcile a Deposit from Stripe

Given an existing deposit with a stored `providerReference`
When the caller requests reconciliation
Then the service retrieves the latest PaymentIntent from Stripe
And the service updates the internal deposit record if Stripe reports a newer state
And the reconciliation result is visible through subsequent retrieval

### AS-10 Local Development Uses the Same Deposit Model

Given the service is running locally with Stripe test or sandbox credentials
When the caller creates and confirms a deposit
Then the service uses the same API operations, status mappings, and deposit lifecycle defined in this specification
And only environment-specific configuration changes between local and later non-test deployments
