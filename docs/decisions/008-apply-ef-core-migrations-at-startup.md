# ADR 008: Apply EF Core Migrations at Startup

- Status: Accepted

## Context

The schema is created by `EnsureCreated()` on startup. It builds the schema once and can never change it, so the first schema-changing release has no upgrade path, and ADR-005 requires schema changes to ship as versioned migrations that stay compatible with the previous release. Three choices follow: when migrations are applied, which database engine they target, and how test databases are created.

## Decision

The schema is managed by EF Core migrations, and the service applies pending migrations itself at startup with `Database.Migrate()`, replacing `EnsureCreated()`.

- Migrations target SQLite, the engine already in use; on the local Docker release target the database file lives on a mounted volume.
- Startup migration is unconditional: every environment, the tests included, builds and upgrades its schema the same way, and there is no setting that turns it off.
- Test databases are created by the same startup path, so every test run applies the real migrations to a fresh database.
- A test fails when the EF model has changes that no migration covers.
- The first migration is the baseline: the schema `EnsureCreated()` produces today. No deployed database exists, so no baseline adoption step is needed; a local `payments.db` created before this change is deleted and recreated.

## Rationale

Applying migrations at startup keeps deployment a single step — start the released image — which is what configuration-only deployment (ADR-005) needs on the local Docker target and on a single-instance cloud host. EF Core takes a migration lock while applying, so instances starting at the same time do not race. Rollback stays safe: an older image finds no migrations it does not know to apply, and runs against the expanded schema that the expand-then-contract rule keeps compatible.

SQLite needs no new infrastructure, keeps tests fast and self-contained, and is sufficient for a single-instance deployment. Running the real migrations in tests means a broken migration fails CI rather than a release; building test schemas any other way would be a test-only code path.

The decision would change if the service ran several instances whose rollout must not wait on a migration, or moved to a server database, at which point a separate migration step and a new baseline become worthwhile.

## Consequences

- every schema change ships as a migration under `Payments.Api.Persistence`; `EnsureCreated()` is not used anywhere
- a migration must stay compatible with the previous release — expand in one release, contract in a later one — and that compatibility is reviewed at code approval; no migration is written to be reversed on rollback
- a model change without a migration fails `dotnet test`
- the service does not start when a migration fails, so a failed schema change is caught by the release's health check, not later by a request
- there is no environment-specific migration path; local, test, and deployed databases all migrate at startup
- moving to another engine, or to a separate migration step, requires a new decision record that supersedes this one, and a regenerated baseline because migrations are provider-specific
