# <Flow Name> Runbook

<!--
Derived from the Operations section of docs/specs/<flow>-processing.md (NFR-13).
Written from the spec, not the code, so that it can be followed by someone who has
never read the code. Where this runbook and the spec disagree, the spec wins and
this runbook is corrected. Every section is required.
-->

## 1. Scope

The flow this runbook covers, the spec it is derived from, and the operations it relies on (route and purpose, from the spec's API expectations).

## 2. Signals

What tells you something is wrong: the health and readiness endpoints, alerts, and the NFR-6 events and log fields to look at. For each signal, the failure mode in section 3 it points to.

## 3. Failure Modes

One subsection per failure mode in the spec's Operations section:

- **Symptoms** — what the signal looks like.
- **Diagnose** — read-only checks that confirm this failure mode and rule out others, with the identifiers to use (`depositId`, `providerReference`, correlation id).
- **Respond** — the safe response through specified operations, in order, and whether each step is safe to repeat.
- **Verify** — how to confirm the flow has recovered.
- **Escalate** — when to stop and who to involve.

## 4. Rollback

When a release is the cause: redeploy the previous released image (`docs/Factory.md`, Release). Note any schema or configuration change in the current release and confirm the previous release can run against it.

## 5. For Operations Agents

An agent may help a human investigate. It works only within this section.

- **Allowed** — the read-only checks it may run, listed explicitly: which endpoints it may call with `GET`, which logs and events it may read, which provider lookups it may make. Anything not listed is not allowed.
- **Never** — change persisted state, call an operation that changes state (including reconciliation) unless this section lists it as agent-safe and a human approves each call, deploy or roll back, change configuration or secrets, or contact the provider with a write operation.
- **Report** — post findings on the incident issue: the failure mode from section 3 that matches, the evidence (identifiers, statuses, timestamps, event names), and the human response from section 3 it recommends. Never print secrets, client secrets, or payment instrument data.

## 6. After the Incident

File or update the issue: a bug naming the FR or NFR that was violated, or a feature if no requirement covered the behaviour. Record the postmortem on that issue. It ends with the spec, a decision record, or this runbook changed through the lifecycle — or with a recorded reason why none needs to change.
