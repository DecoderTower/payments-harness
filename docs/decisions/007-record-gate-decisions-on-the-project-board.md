# ADR 007: Record Gate Decisions on the Project Board

- Status: Accepted

## Context

ADR-005 held gate state in `stage:*` labels that only humans change, with a GitHub Projects board as a view of them. In practice the board is where the maintainer works, and moving a card there changed nothing the factory reads: #24's card sat in Build while its label still said Design. Every approval took a label edit plus a matching card move, and the two drifted. The repository is personally owned, so GitHub raises no event an Action can react to when a card moves; labels were chosen for that reason, at the cost of not being where decisions are actually made.

## Decision

The project board is where gate decisions are made and recorded:

- an issue's stage is the column of its card in the built-in **Status** field of the "Payments Harness Factory" project: Triage, Requirements, Design, Build, Release, Done; moving the card is the spec, design, or release approval
- the `stage:*` labels are retired
- the project's built-in workflows add every new issue to Triage and close an issue when its card is moved to Done
- one Action, triggered by a merged pull request, moves the cards of the service-change issues it references to Release; it records the code approval a human just made and needs a token with project access
- agents read the board but never move cards, edit fields, or change the project; the skills check an issue's column before acting, as they checked its label before

## Rationale

A gate is only as good as the record of it, and the record should sit where the decision is made. The built-in Status field — rather than a custom field — is what the project's built-in workflows act on and what the issue timeline reports when a card moves, which keeps an audit trail of who approved what and when. The two automatic moves only follow events GitHub does raise (an issue opened, a pull request merged), so nothing reacts to a card move and nothing needs polling.

## Consequences

- dragging a card is a gate decision; there is no second record to keep in step with it
- an agent cannot pass a gate by moving a card: the permissions deny every command that edits the board, and a wiring test checks the deny rules exist
- automation that would react to a card move — an agent starting work when a card reaches its column — cannot be event-driven on a personally owned repository; it polls the board, or the repository moves to an organisation
- the board and its token become part of the factory: a missing or expired token stops cards moving to Release on merge, which `check-drift` reports as a merged issue still in Build
- issues labelled before this decision keep their labels in history; their cards carry the current state
