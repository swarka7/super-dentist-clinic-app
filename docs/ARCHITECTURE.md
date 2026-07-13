# Architecture Overview

SuperDentist uses clean dependency boundaries with two composition roots: the WPF operations client and a read-only ASP.NET Core reporting API.

## Solution Layout
- `src/SuperDentist.App`
  - WPF UI (Views + ViewModels)
  - Composition root for DI, configuration, logging, navigation, theming, validation, and user messaging
- `src/SuperDentist.Api`
  - ASP.NET Core composition root, REST/JSON endpoints, DTO mapping, Swagger, health checks, CORS, and HTTP error handling
- `src/SuperDentist.Application`
  - Business use cases, audited entity operations, bounded clinic queries, dashboard aggregation, actor resolution, snapshot serialization, and audit search
  - Depends on Core abstractions, not SQLite or WPF
- `src/SuperDentist.Core`
  - Domain models (Doctor, Patient, Treatment, Appointment, PatientTreatment, AuditEntry)
  - Repository, service, actor-provider, and transaction interfaces
  - Shared results and options
- `src/SuperDentist.Infrastructure`
  - SQLite connection/transaction management, schema migrations, database initialization, and demo seeding
  - SQLite entity/audit repositories and persistence-specific SQL
- `tests/SuperDentist.Tests`
  - xUnit unit, SQLite integration, dashboard, and in-process API tests

## Dependency Direction
```
SuperDentist.App
  -> SuperDentist.Application
  -> SuperDentist.Infrastructure
  -> SuperDentist.Core

SuperDentist.Api
  -> SuperDentist.Application
  -> SuperDentist.Infrastructure
  -> SuperDentist.Core

SuperDentist.Application -> SuperDentist.Core
SuperDentist.Infrastructure -> SuperDentist.Core
```

`SuperDentist.App` is the actual WPF application project. Its assembly/executable is branded as `Super Dentist`, so a local build produces `Super Dentist.exe`. `SuperDentist.Api` is an independent executable composition root; neither client references the other.

## UI + Navigation
- `MainWindow` hosts a `ContentControl` bound to `ShellViewModel.CurrentViewModel`.
- `NavigationService` swaps ViewModels, and XAML DataTemplates map ViewModels to Views.
- Audit History is a discoverable, read-only navigation destination with filters and before/after inspection.

## Read-only Reporting API
The API exposes DTO-only read contracts for doctors, patients, appointments, treatments, audit history, dashboard metrics, and health. Controllers perform HTTP binding/validation and DTO mapping, then delegate filtering and aggregation to Application services. There are no mutation endpoints.

List requests use bounded limits. Text, doctor, patient, appointment date-range, audit entity/operation/actor, and audit UTC-range filters are supported where relevant. Invalid transport input returns validation problem details; missing doctor and patient resources return 404. Unexpected failures are handled centrally and return generic problem details without exception messages or stack traces.

Swagger is enabled in Development. Development CORS allows only configured React origins and defaults to `http://localhost:5173`; no unrestricted production policy is registered. Structured request logs record method, path, status, duration, and trace ID without query strings.

The `/health` endpoint executes a SQLite check. API startup runs the same database initializer and migrations as WPF, and the default database path is shared. The API is read-only, but concurrent WPF writes and API reads still rely on SQLite locking behavior.

Authentication and authorization are not present. Because patient and audit DTOs contain clinic data, the API is suitable only for local/trusted development until a later security milestone protects it.

## Application Layer
- Application services implement the doctor, patient, treatment, appointment, and patient-treatment use cases.
- Services depend on repository and service contracts from Core.
- Business validation and appointment conflict behavior live here rather than in SQLite repositories.
- Application services construct audit records, resolve the current actor through an abstraction, and serialize deterministic JSON snapshots.
- `ClinicQueryService` owns reusable list filtering, deterministic ordering, and bounded paging without ASP.NET Core dependencies.
- `DashboardQueryService` owns cross-entity operational aggregation and returns explicit Application response models.
- With no inactive-doctor property, every stored doctor currently counts as active. Outstanding treatment value is the catalog value of patient-treatment records not marked paid.

## Data Access
- SQLite database stored under `%LOCALAPPDATA%\SuperDentist\superdentist.db` by default.
- Repository interfaces live in Core; SQLite implementations live in Infrastructure.
- Infrastructure remains responsible for technical persistence concerns: connections, migrations, schema, seeding, SQL, and SQLite-specific mapping.
- Every SQLite connection enables `PRAGMA foreign_keys = ON` immediately after opening.
- Audited mutations use a shared SQLite transaction scope so entity and audit writes commit atomically.

## Schema Versioning
Schema versioning is managed by `SqliteDatabaseMigrator` in Infrastructure and tracked in a dedicated `SchemaMigrations` table.

Current migrations:
- Version 1: baseline schema matching the original SQLite table layout.
- Version 2: integrity upgrade with foreign-key constraints and UTC audit timestamp columns.
- Version 3: application audit trail with append-only triggers and timestamp/entity/actor/operation indexes.

Migration behavior:
- Migrations run in deterministic version order.
- Each migration runs inside a transaction.
- A migration is recorded as applied only after its SQL and integrity checks succeed.
- Startup logs migration failures and fails initialization rather than continuing with a partially upgraded database.
- New databases are created by running migrations from version 1 through the latest version.
- Existing unversioned databases with the original tables are adopted as version 1 and then upgraded incrementally.
- Demo data seeding happens only after migrations complete successfully.

## Foreign Keys & Delete Policies
Foreign-key constraints protect the existing clinic relationships:
- Patient → Doctor
- Appointment → Patient
- Appointment → Doctor
- Appointment → Treatment
- PatientTreatment → Patient
- PatientTreatment → Treatment

All relationships use `ON DELETE RESTRICT` and `ON UPDATE RESTRICT`. This is intentionally conservative for healthcare data: deleting a referenced doctor, patient, treatment, appointment, or treatment record should fail rather than silently cascade-delete clinical or scheduling history.

`Appointment.TreatmentNumber` and `Patient.DoctorId` remain nullable at the database level for compatibility with older data and current UI behavior. When present, the values must reference valid rows.

## Audit Timestamps
The main mutable entities include UTC audit metadata:
- `CreatedAtUtc`
- `UpdatedAtUtc`

The columns are applied to Doctors, Patients, Treatments, Appointments, and PatientTreatments. Existing rows receive valid UTC values during migration. Repository inserts populate both timestamps, and repository updates refresh `UpdatedAtUtc`.

## Application Audit Trail
`AuditEntry` is a Core model with Id, EntityType, EntityId, Operation, Actor, TimestampUtc, OldValues, NewValues, and CorrelationId. `AuditOperation` restricts operations to `Created`, `Updated`, and `Deleted`. `AuditQuery` supports entity type, partial entity ID, actor, operation, UTC range, and result-limit filters; SQLite always returns newest records first.

Application services construct audit entries after successful validation and mutation. `AuditService` resolves the actor, creates or accepts a correlation ID, obtains UTC time through `TimeProvider`, and serializes key-sorted snapshot dictionaries into deterministic JSON. Failed validation and conflict results do not emit success entries.

Snapshot fields:
- Doctor: Id, FirstName, LastName, Phone, Address, Email, Specialization, Salary.
- Patient: Id, FirstName, LastName, Phone, Address, Email, Age, TreatmentStatus, DoctorId.
- Treatment: Number, Type, Price, Tools.
- Appointment: PatientId, DoctorId, Date, Time, TreatmentNumber.
- PatientTreatment: PatientId, TreatmentNumber, IsCompleted, IsPaid, StartDate.

Persistence timestamps are excluded because the audit entry has its own timestamp. Configuration, connection strings, secrets, logs, exceptions, and stack traces are never included.

`ICurrentActorProvider` isolates actor resolution from audit consumers. The desktop implementation currently returns `LocalUser`. This label provides local application traceability only; it is not verified identity, authentication, or authorization. A future authenticated-user/role provider can replace it through DI.

`IApplicationTransaction` gives Application a persistence-independent atomic boundary. Infrastructure implements it with a SQLite transaction and an async-flow-local connection scope. Entity repositories and `SqliteAuditRepository` join that scope, so the persisted before/after reads, mutation, and required audit insert commit or roll back together. Create and update paths reload the stored row before building the after-snapshot. Nested work reuses the owning transaction, concurrent commands in that scope are serialized, separate top-level async flows remain isolated, and completed contexts are marked inactive so they cannot be reused. This deliberately avoids a broad Unit of Work refactor. The boundary is local to one SQLite database and does not provide distributed transaction semantics.

Audit persistence is append-only. `IAuditRepository` exposes only add and search operations, and migration 3 creates SQLite triggers that reject direct updates and deletes. There is currently no retention or archival policy.

The WPF Audit History view is read-only. It displays newest-first records, UTC and local timestamps, filters, correlation IDs, and formatted before/after JSON for the selected entry.
## Domain Models
- `Doctor`: Id, FirstName, LastName, Phone, Address, Email, Specialization, Salary, CreatedAtUtc, UpdatedAtUtc
- `Patient`: Id, FirstName, LastName, Phone, Address, Email, Age, TreatmentStatus, DoctorId, CreatedAtUtc, UpdatedAtUtc
- `Treatment`: Number, Type, Price, Tools, CreatedAtUtc, UpdatedAtUtc
- `Appointment`: PatientId, DoctorId, Date, Time, TreatmentNumber, CreatedAtUtc, UpdatedAtUtc
- `PatientTreatment`: PatientId, TreatmentNumber, IsCompleted, IsPaid, StartDate, CreatedAtUtc, UpdatedAtUtc
- `AuditEntry`: Id, EntityType, EntityId, Operation, Actor, TimestampUtc, OldValues, NewValues, CorrelationId

## Startup Flow
WPF:
1. App builds a DI host with configuration and Serilog.
2. App registers Application services and Infrastructure repositories.
3. Database migrator upgrades SQLite and the initializer seeds demo data when needed.
4. MainWindow starts with Doctors view.

API:
1. ASP.NET Core builds the API composition root and registers the same Application and Infrastructure modules.
2. Database initialization completes before endpoints begin serving traffic.
3. Exception handling, request logging, development Swagger/CORS, and controllers enter the request pipeline.

## Logging
- WPF Serilog writes logs to `%LOCALAPPDATA%\SuperDentist\logs\superdentist.log`.
- The API uses structured `ILogger` request and exception events through ASP.NET Core providers.
- Migration and initialization failures are logged with the failing migration version/name where applicable.

## Testing
- Application service unit tests can run with simple fake repositories and no SQLite dependency.
- Appointment conflict tests use deterministic, test-owned data rather than production demo seed data.
- SQLite-backed tests create isolated temporary databases and clean them up afterward.
- Test databases use the production migration mechanism instead of a duplicated test schema.
- Migration tests cover empty database upgrades, baseline and version-2 upgrades without data loss, foreign-key enforcement, restrictive deletes, idempotency, audit timestamps, and migration 3.
- Audit tests cover SQLite-free serialization, actor/UTC capture, before/after values, failed-operation suppression, atomic rollback, filtering, newest-first ordering, initializer idempotency, and persistence across reopened connections.
- Dashboard aggregation is unit tested with deterministic, SQLite-free service fakes.
- API tests use `WebApplicationFactory`, isolated temporary SQLite databases, and the production migration mechanism.
- API coverage includes startup/DI, Swagger, health, metrics, filters, limits, 404/400 behavior, audit queries, and generic 500 responses.

The existing repository contracts load full entity collections before Application filtering and paging. HTTP result sizes are bounded, but database-side projections and count queries remain a scalability improvement for larger datasets. Today/upcoming calculations use the API host local date because appointment dates remain legacy strings and no clinic time-zone setting exists yet.
