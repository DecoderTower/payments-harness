# ADR 002: Enforce Server-Side Idempotency

- Status: Accepted

## Context

Create-deposit operations are exposed over a network and can be retried because of caller timeout, connection loss, process restart, or ambiguous provider outcomes. Without strong idempotency, the same logical deposit could create multiple internal records or multiple Stripe PaymentIntents.

## Decision

Create-deposit operations will require a caller-supplied `idempotencyKey`.

The service will persist that key alongside a normalized request fingerprint and will treat retries with the same key and same payload as the same logical deposit request.

Retries must preserve the original idempotency key.

## Rationale

The caller is best placed to identify repeated submissions of the same logical deposit request. Server-side enforcement prevents duplicate provider-side payment attempts and gives the system a deterministic recovery path after ambiguous failures.

## Consequences

- `POST /deposits` cannot be called without an `idempotencyKey`
- replaying the same create request returns the original deposit result instead of creating a new deposit
- reusing the same key with a different payload is an error
- retry guidance in the implementation and documentation must preserve the original idempotency key
