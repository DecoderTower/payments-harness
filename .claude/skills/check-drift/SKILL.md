---
name: check-drift
description: Maintenance-phase slow loop — compare the specs, decision records, tests, code, and factory documents for drift, and file at most one issue for the most significant finding. Use nightly or on demand.
---

# check-drift

This is the slow loop of the Maintenance phase (`docs/Factory.md`): a feedback loop that runs across the whole repository rather than one change. Loops file, humans decide — the output is at most one issue, never a fix.

## Steps

1. Read the context list in `AGENTS.md`.
2. Look for disagreements between what the context says and what the repository does:
   - every acceptance scenario in each spec has a correspondingly named test
   - the internal status model in each spec matches the status enum in the code, in both directions, and the provider-status mapping table matches the mapper
   - every operation in a spec's API expectations exists as an endpoint, and no endpoint exists that no spec describes
   - each decision record's consequences still hold in the code
   - every path in the `AGENTS.md` project layout exists, or is clearly ahead of the implementation
   - every mechanism marked "in place" in the `docs/Factory.md` enforcement table exists
   - the README repository structure matches the repository
3. Rank the findings. A spec and code that disagree outrank documentation that is out of date.
4. Check for an open issue already covering the top finding (`gh issue list --state open --search "Drift"`). If one exists, file nothing.
5. Otherwise file one issue for the top finding with `gh issue create`, titled `Drift: <finding>`, labelled `stage:triage` like every new issue, and written in the shape of the matching template: a bug naming the violated FR or acceptance scenario when the code disagrees with a spec, a technical change otherwise. Include the evidence: files, lines, and the conflicting statements.
6. Report every finding in the session, ranked, noting which one was filed.

## Constraints

- Read-only with respect to the repository: no commits, no fixes.
- At most one issue per run. Never decide which side of a disagreement is right — the issue states both sides; the spec changes deliberately or the code is fixed, through the lifecycle.
- Never change labels beyond the `stage:triage` every new issue starts with, and never close or comment on other issues.
