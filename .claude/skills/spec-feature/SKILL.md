---
name: spec-feature
description: Turn the intent for a new or changed business flow into a durable spec and decision records under docs/. Use whenever behaviour is being added or changed — intent changes in the context first, then in the code.
---

# spec-feature

This is the Context component of the harness: moving from plans to durable specs. The output is documents, never code.

## Steps

1. Read the context list in `AGENTS.md`, then read `docs/specs/stripe-deposit-processing.md` for the house style and `docs/specs/payment-platform-nfrs.md` for the cross-cutting requirements every flow inherits.
2. Interview the user about the flow. Cover, in order:
   - what the flow is and what triggers it
   - in scope / out of scope (push back to keep scope narrow — out-of-scope bullets are scope control)
   - which PSP object represents an attempt (one provider object per attempt)
   - the lifecycle from request to terminal state
   - the internal status model and the provider-status mapping
   - how this flow relates to existing records (e.g. a refund references a deposit)
3. Draft `docs/specs/<flow>-processing.md` from `docs/specs/TEMPLATE.md`. Every internal status must be reachable in the mapping table; every FR must be exercised by at least one acceptance scenario.
4. For each architectural choice (new PSP object, new invariant), draft a decision record from `docs/decisions/TEMPLATE.md`, numbered after the latest existing ADR.
5. Update the out-of-scope lists of existing specs and `AGENTS.md` constraints if this flow changes what is allowed (e.g. the repo is no longer deposit-only).
6. Stop for human review of the documents. Do not start implementation; that is `implement-spec`'s job, and it begins only from a reviewed spec.

## Constraints

- No changes outside `docs/` and `AGENTS.md`.
- Do not weaken the NFRs; new flows inherit idempotency, retry safety, timeouts, durability, observability, and monetary handling as written.
- Amounts are integers in minor units. Idempotency keys are caller-supplied. No environment-specific business logic.
