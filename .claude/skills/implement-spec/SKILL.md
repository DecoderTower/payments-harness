---
name: implement-spec
description: Implement a reviewed spec from docs/specs/ end to end, mapping every acceptance scenario to a named test. Use after spec-flow has produced a reviewed spec.
---

# implement-spec

The agent operates inside the harness: the spec defines what to build, the constraints define what the system will accept, and the feedback loops decide when it is done.

If the superpowers plugin is available, use its planning and TDD implementation workflow to execute the steps below. This skill supplies the inputs (the spec and the context list) and the definition of done; it does not replace the engineering process.

Input: the path to one spec under `docs/specs/`.

## Steps

1. Read the context list in `AGENTS.md`, the target spec, and `docs/specs/payment-platform-nfrs.md`. Read every ADR in `docs/decisions/`.
2. Plan before coding. List every acceptance scenario in the spec with the test name that will cover it (e.g. `AS3_ReplayingSameCreateRequestReturnsOriginalDeposit`). Present the plan, then build.
3. Implement in `src/Payments.Api`, tests in `tests/Payments.Api.Tests`, per the layout in `AGENTS.md`.
4. All PSP access goes through the provider interface. Both implementations stay behaviourally equivalent: the real provider (`Stripe`) and the configuration-selected `Fake`. Tests run against the fake; the business-logic path is identical (ADR 003).
5. Run `dotnet build` and `dotnet test` until green. The stop gate enforces this — work cannot be declared done while either is red.
6. Report the acceptance-scenario-to-test mapping as a table, with any scenario not covered called out explicitly rather than papered over.

## Constraints

- Do not edit anything under `docs/`. If the spec is wrong or ambiguous, stop and ask — the spec changes deliberately via `spec-flow`, never as collateral damage of implementation.
- No internal statuses beyond those in the spec's status model. No provider states leaked as internal statuses.
- Idempotency, retry safety, timeout, and ambiguous-outcome behaviour follow the NFR spec exactly.
- Secrets come from configuration (user-secrets locally). Never hardcode keys, never log `clientSecret` or payment instrument data.
