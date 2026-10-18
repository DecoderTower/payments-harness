# <Flow Name> Processing Specification

<!--
House style: follow docs/specs/stripe-deposit-processing.md. Durable specs, not plans:
write requirements ("the service shall..."), not implementation steps. Every section
below is required unless marked optional.
-->

## 1. Purpose

One or two paragraphs: what flow this specifies, which provider abstraction it uses, and what the document is for (durable context for implementation).

## 2. Scope

In scope: bullet list of behaviours this spec covers.

Out of scope: explicit bullet list. Scope control lives here — anything not listed as in scope is out.

State that the same lifecycle and status model apply across environments, with environment selection by configuration only (per ADR 003).

## 3. Actors

Name each actor (`Caller`, `Customer`, the service, the PSP, `Operator`) and its role in this flow.

## 4. External Dependency: <PSP Object>

Which provider object represents this flow (e.g. PaymentIntent, Refund). What the internal `providerReference` is. One provider object per attempt.

## 5. <Flow> Record

The durable internal record: required fields (id, references, amount in minor units, currency, idempotency key, provider reference, status, timestamps) and optional operational fields.

## 6. <Flow> Lifecycle

Numbered high-level sequence from request to terminal state. State whether terminal records can be retried (they should not be — a new attempt is a new record).

## 7. API Expectations

One subsection per logical operation: route, expected request fields, expected behaviour, expected response fields.

## 8. Internal Status Model

One subsection per internal status with a definition. Name the terminal statuses explicitly. Do not reuse provider states as the internal model, and do not invent states the provider mapping cannot reach.

## 9. Mapping from <PSP Object> Status to Internal Status

A table: provider status → internal status → notes. Every internal status from section 8 must be reachable here. Call out unsupported provider states and what the service does if it observes one.

## 10. Functional Requirements

FR-1, FR-2, ... Each one testable. Cover at minimum: validation, creation, idempotency, persistence, each outcome path (success, failure, pending), retrieval, reconciliation, and lifecycle events/logs.

## 11. Acceptance Scenarios

AS-1, AS-2, ... in Given/When/Then form. Every FR should be exercised by at least one scenario. Every scenario maps to a named test in the implementation — this is the feedback loop that catches drift.

## 12. Security Considerations

The security properties this flow must keep, each stated so code review can check it: which inputs are untrusted, who may call each operation, which data must never be logged or returned (secrets, client secrets, payment instrument data), and how provider callbacks or responses are authenticated. Name the FRs and scenarios that exercise each property.
