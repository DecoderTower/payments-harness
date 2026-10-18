# ADR 006: Pin and Verify the Stripe API Version

- Status: Accepted

## Context

NFR-12 requires an explicitly chosen Stripe API version, reviewed whenever it changes and recorded so it can be audited. The integration did neither: `StripeClient` was constructed without a version, so the service used whatever version the Stripe.net SDK embeds. Stripe.net is strongly typed — each release is generated for exactly one API version and cannot be pointed at another — so an SDK upgrade is an API version upgrade. The Stripe.net 52 → 53 dependency update (#16) moved the API version from `2026-08-26.dahlia` to `2026-09-30.endive` with nobody deciding it, and CI could not notice, because tests run against the `Fake` provider (#24).

## Decision

The Stripe API version is a recorded, verified decision:

- the chosen version is recorded in configuration as `Stripe:ApiVersion`, alongside the other Stripe settings (ADR-003, NFR-11)
- the Stripe provider compares the recorded version with the version the SDK embeds when it is constructed, refuses to start if they differ, and logs the version it runs with
- a test fails whenever the recorded version differs from the SDK's, so an SDK upgrade that moves the API version cannot merge until someone changes the recorded version deliberately
- the version adopted now is `2026-09-30.endive`, reviewed against this integration: of its breaking changes, the removal of the writable `payment_method_types` parameter does not apply (the service never sends it), and the new 3DS `data_share_only` result does not affect the .NET SDK and is not read; the remaining breaking changes are in products the service does not use

## Rationale

With a strongly typed SDK, pinning cannot mean overriding the version per request; it means choosing the SDK release and verifying it is the one intended. Recording the version in configuration puts it where NFR-12 and operators expect it, and the check turns NFR-12 from a stated constraint into an enforced one: the dependency update path (`docs/Factory.md`, Maintenance) now stops on an API version change instead of relying on a reviewer noticing.

## Consequences

- a Stripe.net upgrade that moves the API version fails CI; it becomes a deliberate change that reviews Stripe's changelog against the deposit spec and updates `Stripe:ApiVersion`
- an upgrade that keeps the API version (patch and minor releases within a version) passes as before
- the service fails fast at startup, in Stripe mode, if configuration and SDK disagree; it never runs against an API version nobody recorded
- when webhook endpoints are added, their API version is aligned with `Stripe:ApiVersion` (NFR-12)
- the `Fake` provider is unaffected; the business-logic path stays identical across providers (ADR-003)
