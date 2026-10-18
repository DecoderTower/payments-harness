# Architecture

This document is the durable description of how `Payments.Api` is structured and which dependency paths are allowed. It is part of the harness context: the dependency rules below are not guidance, they are constraints, and every numbered rule is enforced mechanically by a matching test in `tests/Payments.Api.ArchitectureTests` (see ADR-004). A change that violates a rule fails `dotnet test` and is stopped by the same feedback loop that runs the rest of the suite.

If the architecture needs to change, the intent changes here first — update this document and record the decision in `docs/decisions/`, then update the matching architecture test, then the code. Never the other way around.

## Component map

```text
                 HTTP edge
  +---------------------------------------+
  |  Endpoints          Middleware        |
  +-------+-----------------+-------------+
          |                 |
          v                 |
      Contracts             |
          |                 v
          |             Services  <-- single business-logic path (ADR-003)
          |              /  |  \
          v             v   |   v
        Domain <-- Persistence  Providers --> Stripe SDK (Stripe implementation only)
```

`Program` (the composition root, in the global namespace) wires configuration and dependency injection and may see everything. It is the only place where provider selection happens, and it happens by configuration only (ADR-003).

## Components

| Namespace (`Payments.Api.*`) | Responsibility | May depend on |
| --- | --- | --- |
| `Domain` | Deposit aggregate, internal status model, PaymentIntent status mapping | nothing (BCL only) |
| `Contracts` | HTTP request/response DTOs | `Domain` |
| `Persistence` | EF Core `DbContext`, schema, and the migrations that evolve it (ADR-008) | `Domain`, EF Core |
| `Providers` | PSP abstraction (`IPaymentProvider`) with Stripe and Fake implementations | Stripe SDK only, and only inside the Stripe implementation |
| `Services` | Deposit business logic — the single path both providers flow through | `Domain`, `Persistence`, `Providers`, EF Core |
| `Endpoints` | Minimal API surface; maps DTOs to service commands | `Contracts`, `Services`, ASP.NET Core |
| `Middleware` | Exception-to-ProblemDetails mapping | `Services`, `Providers`, ASP.NET Core |

Note that `Providers` does not depend on `Domain`: the PSP abstraction speaks its own provider model, and `Domain.PaymentIntentStatusMapper` translates provider state into the internal status model. That keeps the provider boundary swappable in both directions.

## Dependency rules

Each rule is enforced by the identically numbered test in `tests/Payments.Api.ArchitectureTests/ArchitectureRulesTests.cs`.

1. **Domain depends on nothing else in the service and no infrastructure.** No other `Payments.Api` namespace, no EF Core, no ASP.NET Core, no Stripe SDK. The status model and mapping logic stay pure and trivially testable.
2. **Contracts depend only on Domain.** DTOs may reference domain types (such as `DepositStatus`) but never services, persistence, or providers.
3. **Endpoints reach Persistence and Providers only through Services.** The HTTP layer cannot query the database or call the PSP directly; every request flows through the single business-logic path (ADR-003).
4. **Services do not depend on the HTTP edge.** No `Endpoints`, `Middleware`, or `Contracts`. Business logic stays callable without an HTTP context.
5. **Persistence depends only on Domain.** The `DbContext` maps domain types; it knows nothing about services or providers.
6. **Providers are self-contained.** No dependency on any other `Payments.Api` namespace. The PSP boundary owns its own request/result model (`ProviderModels`).
7. **Only Providers depend on the Stripe SDK.** The rest of the service sees `IPaymentProvider` and the provider model, never `Stripe.*` types (ADR-001, ADR-003). This is what makes the Fake provider a genuine substitute rather than a parallel code path.
8. **EF Core stays behind the service boundary.** Only `Services` and `Persistence` may reference `Microsoft.EntityFrameworkCore`; the HTTP edge, contracts, domain, and providers never see it.

## Enforcement

- `tests/Payments.Api.ArchitectureTests` loads the `Payments.Api` assembly with [ArchUnitNET](https://github.com/TNG/ArchUnitNET) and checks every rule above. Layer-to-layer rules are checked between loaded types; rules about external frameworks (Stripe SDK, EF Core, ASP.NET Core) are checked against dependency targets by namespace pattern, because referenced assemblies are not loaded into the analyzed architecture.
- The tests run as part of `dotnet test`, which the stop gate executes before an agent can claim work is done.
- The composition root (`Program`, global namespace) is exempt by construction: the rules are scoped to `Payments.Api.*` namespaces.
