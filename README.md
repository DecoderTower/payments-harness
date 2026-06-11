# Payments Harness: Stripe Deposit Service

This repository is the starting point for a Stripe-backed deposit service. It is the companion repository for the [Harness Engineering series](https://decodertower.substack.com/p/why-context-isnt-enough-in-ai-assisted), picking up where Part 1 left off: context engineering improves how an agent makes decisions, but the harness defines the system those decisions have to live in. This repository is that harness, built around one intentionally narrow business flow: a customer depositing money into a wallet or account.

The goal is to capture a small but credible vertical slice. The repository is limited on purpose so the core concerns stay visible: request validation, idempotency, provider state mapping, reconciliation, and configuration-driven environment handling.

## Why Start With Deposits

A deposit is the smallest flow that still forces the real decisions: how a request becomes idempotent, how Stripe's PaymentIntent states map onto an internal status model, and how the system reconciles when those two views disagree. Everything in this repository exists to make those decisions visible and durable.

Other payment flows build on the same foundations — the same status model, the same provider mapping, the same reconciliation loop. Starting narrow keeps that foundation honest before it has to support anything else.

## Current Repository Structure

```text
/
|-- AGENTS.md
|-- README.md
|-- payments-harness.slnx
|-- .claude/
|   |-- settings.json
|   |-- hooks/
|   |   |-- spec-guard.mjs
|   |   `-- stop-gate.mjs
|   `-- skills/
|       |-- spec-feature/
|       |-- implement-spec/
|       `-- verify-feature/
|-- docs/
|   |-- Architecture.md
|   |-- decisions/
|   |   |-- TEMPLATE.md
|   |   |-- 001-use-stripe-paymentintents-for-deposits.md
|   |   |-- 002-enforce-server-side-idempotency.md
|   |   |-- 003-configure-stripe-environment-via-config-only.md
|   |   `-- 004-enforce-architecture-rules-with-archunit-tests.md
|   `-- specs/
|       |-- TEMPLATE.md
|       |-- payment-platform-nfrs.md
|       `-- stripe-deposit-processing.md
|-- src/
|   `-- Payments.Api/
`-- tests/
    |-- Payments.Api.Tests/
    `-- Payments.Api.ArchitectureTests/
```

The repository started as durable context only; the deposit slice in `src/` and `tests/` was implemented against that context afterwards.

## How the Documents Work Together

`docs/specs/stripe-deposit-processing.md` is the main functional specification for the deposit lifecycle. It defines the API expectations, the internal status model, the mapping from Stripe PaymentIntent states, and the acceptance scenarios.

`docs/specs/payment-platform-nfrs.md` captures the cross-cutting behavioural requirements that should remain stable as implementation begins, including idempotency, retries, timeout handling, ambiguous outcomes, durability, observability, and monetary handling.

`docs/Architecture.md` describes the component structure of the service and the dependency rules between its parts. The rules are enforced mechanically by the architecture tests in `tests/Payments.Api.ArchitectureTests`.

`docs/decisions/` contains concise architectural decisions that future implementation work should inherit unless they are explicitly replaced.

`AGENTS.md` is the short operating guide for future coding agents. It tells them what to read first and which constraints they must preserve.

## Stripe Environments

Local development is intended to run against Stripe testing environments, using test mode or sandbox credentials supplied through configuration.

That is not treated as a separate system. The repository assumes one Stripe integration model across environments:

- the same deposit lifecycle
- the same internal status model
- the same PaymentIntent-based provider mapping

If the project later moves beyond sandbox or test credentials, the intended change is configuration only. The business logic and state model should not fork by environment.

## The Harness

Part 1 of the series defines a harness as three things working together. Each artifact in this repository belongs to one of them.

**Context** makes intent explicit and durable — not transient prompts or plans, but something versioned, shared, and visible to both agents and engineers:

- `docs/specs/` — durable specs for the deposit flow and the cross-cutting NFRs
- `docs/Architecture.md` — the component map and dependency rules the implementation must keep
- `docs/decisions/` — decision records that survive across sessions
- `AGENTS.md` — the operating guide an agent reads before changing anything

**Constraints** shape how decisions can be expressed and make certain paths impossible:

- the constraints in `AGENTS.md`: deposit-only scope, no invented lifecycle states, idempotency that cannot be bypassed
- the internal status model and PaymentIntent mapping, which implementations follow rather than reinterpret
- the dependency rules in `docs/Architecture.md`, enforced by ArchUnit tests in `tests/Payments.Api.ArchitectureTests` — a forbidden dependency fails the test suite instead of waiting for review
- hooks and permission settings under `.claude/`, added alongside the implementation, that enforce some of these mechanically

**Feedback loops** are the mechanisms that tell you when the system has drifted:

- every acceptance scenario in a spec maps to a named test
- a stop gate that runs the build and tests before an agent can claim work is done
- end-to-end verification against a running service, including reconciliation with the provider

Tactical context engineering improves local decisions within a session. The harness is what maintains coherence across sessions: intent stays durable, implementation is constrained, and drift is caught when it happens. The agent operates inside that harness — its output is shaped not just by what it sees, but by what the system will accept.

## Why This Shape

This is a blog companion repository, but it is not meant to be a throwaway example. The intent is to start from durable context that a future implementation can follow directly:

- a clear deposit-only scope
- explicit functional requirements
- explicit non-functional requirements
- a small set of decisions that prevent the integration from drifting into inconsistent behaviour

That keeps the starting point honest without introducing unnecessary architecture too early.
