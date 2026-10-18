# ADR 005: Run Delivery as a Lights-On Software Factory

- Status: Accepted

## Context

Each step of the harness — specifying, implementing, verifying — can now be run by an agent, and chaining them into a pipeline is the obvious next move. The pull is toward a lights-off factory: issues go in, merged code comes out, and nobody reads it. Tests can show that code is correct today, but not that it is maintainable, and decay in unread code surfaces months later, when the people who must fix it no longer understand it.

## Decision

Delivery runs as a lights-on software factory, as described in `docs/Factory.md`:

- work moves through the lifecycle phases — intake and triage, prioritisation, requirements, design, implementation, testing, security, code review, release, operations, maintenance — and agents do the work in each phase
- humans own prioritisation and three gates: they review and merge the requirements (spec), approve the design (decision records and a short technical design) before any code is written, and read the code before merging it
- GitHub Issues is the intake; gate state is held in `stage:*` labels that only humans change, and a Projects board is a view of that state
- any available agent reviews an implementation PR first, as an advisory comment; a human performs the final review and the merge
- checks a machine can perform run deterministically in CI on every PR, independent of the agent's configuration
- releases are versioned container images; the deployment target — local Docker now, a cloud host later — is selected by configuration only
- the harness is agent-agnostic: `AGENTS.md` guides every agent, and skills and hooks have one canonical copy

## Rationale

Human attention is the scarce resource, not code generation. The factory is designed to spend that attention where only a human can — intent, design, maintainability — and to remove it from everything a machine can check. The design gate is the highest-leverage point: steering the shape of a change before it is built costs minutes, while steering it in review costs a rebuild. Reading the code stays mandatory because no deterministic loop measures maintainability.

GitHub Issues keeps intake, review, and CI in one place the agent can reach with `gh`; the tracker is swappable because the factory depends on gate semantics, not on GitHub. Keeping gate transitions human-only, and enforced by the allowlist, means no agent can advance its own work. Issue text is untrusted input, so the spec gate always stands between an issue and an implementation.

Selecting the deployment target by configuration applies ADR-003's reasoning to environments: what runs locally is what runs in the cloud.

## Consequences

- there is no unattended path from an issue to a merged change; every merge has a human reviewer who read the code
- agents cannot merge PRs, close issues, or change `stage:*` labels; a human may skip a gate deliberately, an agent never can
- nothing is implemented from issue text; implementation starts only from an approved technical design, built on a merged spec when behaviour changes
- technical designs are working documents on issues and PRs, never committed; specs and decision records remain the only durable statements of intent
- the agent review is advisory and cannot block or approve; the factory runs with whichever agent is available
- PR size is reported with a warning above 400 changed lines in `src/` and `tests/`, not enforced
- scheduled and operational loops may file issues, at most one per signal, but never push code
- there is no environment-specific build or deployment code path; moving from local Docker to a cloud host is a configuration change
- schema changes must ship as versioned migrations, which requires its own decision record before the first release that changes the schema
- changing the factory requires changing `docs/Factory.md` and this record first, then the enforcing mechanism
