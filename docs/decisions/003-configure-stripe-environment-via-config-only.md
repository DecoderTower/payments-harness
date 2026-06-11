# ADR 003: Configure Stripe Environment via Config Only

- Status: Accepted

## Context

The repository is intended to run locally against Stripe testing environments and to remain credible as a starting point for later non-test deployment. The project should not fork business behaviour between local testing and later environments.

## Decision

Stripe environment selection will be configuration-driven only.

The service will use environment-specific secrets, account settings, endpoints where applicable, and related Stripe configuration supplied externally. The internal deposit workflow, state model, retry behaviour, and provider mapping will remain the same across environments.

## Rationale

Using configuration rather than code branches keeps the integration honest. It reduces the risk that local development exercises one code path while later environments run another. It also matches how Stripe environments are actually selected in practice: by credentials and account configuration, not by a separate payment domain model.

## Consequences

- there will be no environment-specific business logic branches for deposit processing
- there will be no separate demo-mode code path
- local development will use Stripe test mode or sandbox credentials through configuration
- moving beyond sandbox will require configuration changes only, not code changes
- the same internal status model and PaymentIntent mapping will apply in every environment
