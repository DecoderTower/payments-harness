# ADR 001: Use Stripe PaymentIntents for Deposits

- Status: Accepted

## Context

This repository is scoped to a deposit-only service backed by Stripe. The service needs a provider abstraction that can represent the full lifecycle of a deposit attempt, including confirmation, customer authentication, processing, success, and failure.

## Decision

The project will use Stripe PaymentIntents as the provider-side representation of each deposit attempt.

The internal `providerReference` for a deposit will be the Stripe PaymentIntent identifier.

## Rationale

PaymentIntents are the Stripe abstraction designed for payment lifecycle management. They provide:

- a durable provider object for a single deposit attempt
- explicit lifecycle states that map cleanly into the service's internal deposit statuses
- support for authentication-required flows such as 3DS
- a straightforward basis for retrieval and reconciliation

This is a better fit for the deposit flow than modelling against lower-level charge primitives.

## Consequences

- internal deposit status mapping will be defined in terms of PaymentIntent states
- reconciliation will retrieve the Stripe PaymentIntent by `providerReference`
- manual capture is out of scope for this repository and `requires_capture` is treated as unsupported
- future implementation work should preserve one PaymentIntent per deposit attempt
