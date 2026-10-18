---
name: design-change
description: Design phase for an issue at stage:design — draft decision records for its architectural choices and post a one-to-two page technical design on the issue for human approval. Use after the spec is merged, or directly for a technical change.
---

# design-change

This is the Design phase of the factory (`docs/Factory.md`): the highest-leverage human review. Steering the shape of a change here costs minutes; steering it in code review costs a rebuild. The output is decision records and a technical design, never code.

Input: an issue number.

## Steps

1. Confirm the issue carries `stage:design` (`gh issue view <n>`). If it does not, stop: the design phase has not been authorised.
2. Read the context list in `AGENTS.md`, the issue and its comments, the merged spec when behaviour changes, every decision record in `docs/decisions/`, and `docs/Architecture.md`.
3. Identify the architectural choices the change involves. For each one, put the options and a recommendation to the user — the choice is theirs — then draft a decision record from `docs/decisions/TEMPLATE.md`, numbered after the latest existing record. If the component structure or a dependency rule changes, update `docs/Architecture.md` in the same change.
4. Draft the technical design — one to two pages, the shape of the change rather than a line-by-line plan:
   - **Basis** — the issue, the spec FRs and acceptance scenarios, the decision records it follows
   - **Types** — new and changed types, each in its component per `docs/Architecture.md`
   - **Signatures** — public members only, no method bodies
   - **Persistence** — schema changes and their migrations
   - **Configuration** — new settings, selected by configuration only
   - **Files** — created and changed
   - **Tests** — each acceptance scenario mapped to a named test; for technical changes, the tests that prove behaviour is unchanged
   - **Observability and operability** — NFR-6 events added or changed, and how each failure mode in the spec's Operations section is detected (NFR-13)
   - **Risks and open questions**
5. Post the technical design on the issue with `gh issue comment <n>`, headed **Technical design**. Do not commit it.
6. Commit any decision records and `docs/Architecture.md` changes on branch `issue-<n>/design` and open a PR referencing the issue (`Refs #<n>`). A decision record takes the next free number; if another open PR already claims it, say so in the PR.
7. Stop. A human merges the decision records and approves the technical design by setting `stage:build` (Gate B). Implementation begins only then.

## Constraints

- Changes only to `docs/decisions/` and `docs/Architecture.md`. No code, and no method bodies in the technical design.
- Never change labels; approving the design is a human decision.
- A design that needs a behaviour change the spec does not describe stops here and goes back to requirements (`spec-feature`).
- New lifecycle states, weakened idempotency, or environment-specific code paths are not design choices; they are constraint violations and are reported as such.
