# AGENTS

This repository is built as a harness: durable context, enforced constraints, and feedback loops that catch drift. This file is the operating guide for agents working inside it.

## Context — read before making changes

These documents carry the durable intent. They are versioned and shared; treat them as the source of truth.

- `docs/specs/stripe-deposit-processing.md`
- `docs/specs/payment-platform-nfrs.md`
- `docs/Architecture.md`
- `docs/decisions/001-use-stripe-paymentintents-for-deposits.md`
- `docs/decisions/002-enforce-server-side-idempotency.md`
- `docs/decisions/003-configure-stripe-environment-via-config-only.md`
- `docs/decisions/004-enforce-architecture-rules-with-archunit-tests.md`

## Project layout

- `src/Payments.Api/` — ASP.NET Core minimal API service
- `tests/Payments.Api.Tests/` — xUnit tests
- `tests/Payments.Api.ArchitectureTests/` — ArchUnitNET tests enforcing `docs/Architecture.md`
- `docs/specs/` — functional and non-functional specifications (source of truth)
- `docs/Architecture.md` — component map and enforced dependency rules
- `docs/decisions/` — architecture decision records
- `.claude/skills/` — repo workflow skills: `spec-feature`, `implement-spec`, `verify-feature`

If a listed path does not exist yet, this document is ahead of the implementation. Create the missing piece to match this layout; do not invent an alternative structure.

## Commands

- Build: `dotnet build`
- Test: `dotnet test`
- Run: `dotnet run --project src/Payments.Api`

## Configuration and secrets

- Stripe credentials come from .NET user-secrets locally and environment configuration elsewhere. Never commit keys.
- The payment provider (`Stripe` or `Fake`) is selected by configuration only, per `docs/decisions/003`. Both implementations sit behind the same provider interface and share one business-logic path.

## How a change gets made

1. New or changed behaviour starts with a spec change in `docs/specs/`, following the structure of the existing specs (use `docs/specs/TEMPLATE.md`). Intent changes in the context first, then in the code — never the other way around.
2. Architectural choices get a decision record in `docs/decisions/` (use `docs/decisions/TEMPLATE.md`).
3. Implement against the spec. Every acceptance scenario in the spec maps to a named test.
4. Verify the flow end-to-end against a running service before claiming it works.

## Feedback loops — definition of done

These are the mechanisms that catch drift. Work is not done until every loop is green.

- `dotnet build` and `dotnet test` pass.
- Every acceptance scenario in the relevant spec has a corresponding test.
- Lifecycle transitions emit structured logs or events per NFR-6.
- No disagreement between spec and code is left unresolved.

## Constraints

These rules make certain paths impossible. Some are enforced mechanically by hooks; treat the rest as equally hard.

- Treat the specs as the source of truth. If code and spec disagree, fix the code or update the spec deliberately.
- Keep the project deposit-only. Do not add refunds, withdrawals, disputes, payouts, subscriptions, partial capture, or multi-PSP routing without a spec change.
- Do not invent new lifecycle states beyond those defined in `docs/specs/stripe-deposit-processing.md`.
- Do not bypass caller-supplied create-deposit idempotency.
- Do not change retry or ambiguous-outcome behaviour without updating `docs/specs/payment-platform-nfrs.md`.
- Keep Stripe environment handling configuration-driven. No separate demo, test-only, or live-only business logic paths.
- Preserve the PaymentIntent-based provider model unless a new decision record explicitly changes it.
- Respect the dependency rules in `docs/Architecture.md`. They are enforced by `tests/Payments.Api.ArchitectureTests`; a structural change starts with that document and a decision record, then the matching test, then the code.
