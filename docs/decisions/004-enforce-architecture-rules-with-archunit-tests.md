# ADR 004: Enforce Architecture Rules with ArchUnit Tests

- Status: Accepted

## Context

The service has a deliberate internal structure — a pure domain, a self-contained PSP boundary, and a single business-logic path — that earlier decisions depend on (ADR-001, ADR-003). Until now that structure existed only as convention: nothing stopped a change from calling the Stripe SDK out of a service or querying the database from an endpoint. Conventions that are not enforced drift.

## Decision

The dependency rules documented in `docs/Architecture.md` are enforced by architecture tests in `tests/Payments.Api.ArchitectureTests`, written with ArchUnitNET (`TngTech.ArchUnitNET.xUnit`). Each numbered rule in the architecture document maps to one named test. The project runs as part of `dotnet test`, so the existing stop gate enforces the rules mechanically.

## Rationale

Architecture tests turn structural intent into a constraint rather than guidance: a violating dependency fails the build-and-test loop instead of waiting for a reviewer to notice. Expressing the rules as ordinary xUnit tests reuses the feedback loops the harness already has — no new hook, linter, or CI step. ArchUnitNET was chosen over lighter alternatives (NetArchTest) for its expressive fluent API and failure messages that name the offending types.

One verified caveat shaped the implementation: ArchUnitNET only loads the `Payments.Api` assembly, so rules about external frameworks (Stripe SDK, EF Core, ASP.NET Core) must match dependency targets by namespace pattern; matching them as loaded types produces an empty set and a rule that always passes.

## Consequences

- the dependency rules in `docs/Architecture.md` are constraints, not documentation: violating code cannot pass `dotnet test`
- no type outside `Payments.Api.Providers` can reference the Stripe SDK
- the HTTP edge cannot reach the database or the PSP except through `Payments.Api.Services`
- the domain model cannot acquire framework dependencies unnoticed
- changing the architecture now requires changing `docs/Architecture.md` and the matching test deliberately, in that order — structural drift surfaces as a named test failure
- new namespaces in `Payments.Api` must be added to the architecture document and rules, or the rules will not cover them
