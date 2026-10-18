---
name: implement-spec
description: Implementation phase — build an issue at stage:build against its approved technical design, mapping every acceptance scenario to a named test, and open the PR. Use after design-change and a human approval.
---

# implement-spec

This is the Implementation phase of the factory (`docs/Factory.md`). The agent operates inside the harness: the spec defines what to build, the approved technical design defines its shape, the constraints define what the system will accept, and the feedback loops decide when it is done.

Input: an issue number at `stage:build`.

## Steps

1. Confirm the issue carries `stage:build` (`gh issue view <n> --comments`). If it does not, stop: implementation has not been authorised.
2. Read the context list in `AGENTS.md`, the approved **Technical design** comment on the issue, the spec it builds on when behaviour changes, `docs/specs/payment-platform-nfrs.md`, and every decision record in `docs/decisions/`.
3. Implement the technical design as approved. It is the plan; do not re-plan. A small deviation is recorded for the PR description. A design that cannot work as approved stops here and goes back to design, with the reason.
4. Implement in `src/Payments.Api`, tests in `tests/Payments.Api.Tests`, per the layout in `AGENTS.md`. Every acceptance scenario gets a named test (e.g. `AS3_ReplayingSameCreateRequestReturnsOriginalDeposit`).
5. All PSP access goes through the provider interface. Both implementations stay behaviourally equivalent: the real provider (`Stripe`) and the configuration-selected `Fake`. Tests run against the fake; the business-logic path is identical (ADR 003).
6. Run `dotnet build` and `dotnet test` until green. The stop gate enforces this — work cannot be declared done while either is red.
7. Open the PR with `gh pr create`, following `.github/pull_request_template.md`: `Closes #<n>`, the technical design copied in, deviations, and the acceptance-scenario-to-test table with any scenario not covered called out explicitly rather than papered over.

## Constraints

- Do not edit anything under `docs/`. If the spec or design is wrong or ambiguous, stop and ask — they change deliberately through `spec-feature` and `design-change`, never as collateral damage of implementation.
- Never merge, approve, or change labels; code review ends with a human.
- No internal statuses beyond those in the spec's status model. No provider states leaked as internal statuses.
- Idempotency, retry safety, timeout, and ambiguous-outcome behaviour follow the NFR spec exactly.
- Secrets come from configuration (user-secrets locally). Never hardcode keys, never log `clientSecret` or payment instrument data.
