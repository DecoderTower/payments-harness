---
name: verify-feature
description: Prove a flow against a running service by driving its API end to end through the spec's acceptance scenarios. Use after implement-spec, or any time someone asks whether a flow actually works.
---

# verify-feature

This is a feedback loop: tests passing is necessary but not sufficient — this skill checks the running system still matches the spec, which is how drift gets caught.

Input: the path to one spec under `docs/specs/`.

## Steps

1. Read the spec's acceptance scenarios and API expectations.
2. Choose the provider by configuration only: `Fake` by default; `Stripe` when test-mode credentials are configured and the user wants a real provider run. Same commands, same code path, different config (ADR 003).
3. Start the service with `dotnet run --project src/Payments.Api` in the background and wait for it to listen.
4. Drive the API through each acceptance scenario in order with real HTTP requests, asserting status codes, response fields, and internal status transitions against the spec — including the negative scenarios (validation rejection, idempotency conflict).
5. Check the service logs for the lifecycle events NFR-6 requires, with correlation fields present and no secrets leaked.
6. Stop the service.
7. Report scenario by scenario: pass/fail, with the observed evidence (ids, statuses, provider references). Failures are reported as found — never adjusted to fit.

## Constraints

- Read-only with respect to the repo: no code or doc changes. Findings go to the user; fixes go through `implement-spec` or `spec-feature`.
- Never print or log `clientSecret`, API keys, or payment instrument details.
- When using real Stripe test mode, use Stripe's documented test payment methods (e.g. `pm_card_visa`); never raw card numbers.
