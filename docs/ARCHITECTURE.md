# Architecture Overview

SuperDentist follows a clean MVVM architecture with clear separation between UI, business use cases, domain abstractions, and persistence.

## Solution Layout
- `src/SuperDentist.App`
  - WPF UI (Views + ViewModels)
  - Composition root for DI, configuration, logging, navigation, theming, validation, and user messaging
- `src/SuperDentist.Application`
  - Business use cases and service implementations
  - Depends on Core abstractions, not SQLite or WPF
- `src/SuperDentist.Core`
  - Domain models (Doctor, Patient, Treatment, Appointment, PatientTreatment)
  - Repository and service interfaces
  - Shared results and options
- `src/SuperDentist.Infrastructure`
  - SQLite connection management, schema migrations, database initialization, and demo seeding
  - SQLite repository implementations and persistence-specific SQL
- `tests/SuperDentist.Tests`
  - xUnit unit and integration tests

## Dependency Direction
```
SuperDentist.App
  -> SuperDentist.Application
  -> SuperDentist.Infrastructure
  -> SuperDentist.Core

SuperDentist.Application -> SuperDentist.Core
SuperDentist.Infrastructure -> SuperDentist.Core
```

`SuperDentist.App` is the actual WPF application project. Its assembly/executable is branded as `Super Dentist`, so a local build produces `Super Dentist.exe`.

## UI + Navigation
- `MainWindow` hosts a `ContentControl` bound to `ShellViewModel.CurrentViewModel`.
- `NavigationService` swaps ViewModels, and XAML DataTemplates map ViewModels to Views.

## Application Layer
- Application services implement the doctor, patient, treatment, appointment, and patient-treatment use cases.
- Services depend on repository and service contracts from Core.
- Business validation and appointment conflict behavior live here rather than in SQLite repositories.

## Data Access
- SQLite database stored under `%LOCALAPPDATA%\SuperDentist\superdentist.db` by default.
- Repository interfaces live in Core; SQLite implementations live in Infrastructure.
- Infrastructure remains responsible for technical persistence concerns: connections, migrations, schema, seeding, SQL, and SQLite-specific mapping.
- Every SQLite connection enables `PRAGMA foreign_keys = ON` immediately after opening.

## Schema Versioning
Schema versioning is managed by `SqliteDatabaseMigrator` in Infrastructure and tracked in a dedicated `SchemaMigrations` table.

Current migrations:
- Version 1: baseline schema matching the original SQLite table layout.
- Version 2: integrity upgrade with foreign-key constraints and UTC audit timestamp columns.

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

## Domain Models
- `Doctor`: Id, FirstName, LastName, Phone, Address, Email, Specialization, Salary, CreatedAtUtc, UpdatedAtUtc
- `Patient`: Id, FirstName, LastName, Phone, Address, Email, Age, TreatmentStatus, DoctorId, CreatedAtUtc, UpdatedAtUtc
- `Treatment`: Number, Type, Price, Tools, CreatedAtUtc, UpdatedAtUtc
- `Appointment`: PatientId, DoctorId, Date, Time, TreatmentNumber, CreatedAtUtc, UpdatedAtUtc
- `PatientTreatment`: PatientId, TreatmentNumber, IsCompleted, IsPaid, StartDate, CreatedAtUtc, UpdatedAtUtc

## Startup Flow
1. App builds a DI host + config + logging.
2. App registers Application services and Infrastructure repositories.
3. Database migrator upgrades the SQLite schema to the latest version.
4. Database initializer seeds demo data if the migrated database has no doctors.
5. MainWindow starts with Doctors view.

## Logging
- Serilog writes logs to `%LOCALAPPDATA%\SuperDentist\logs\superdentist.log`
- Migration and initialization failures are logged with the failing migration version/name where applicable.

## Testing
- Application service unit tests can run with simple fake repositories and no SQLite dependency.
- Appointment conflict tests use deterministic, test-owned data rather than production demo seed data.
- SQLite-backed tests create isolated temporary databases and clean them up afterward.
- Test databases use the production migration mechanism instead of a duplicated test schema.
- Migration tests cover empty database upgrades, baseline database upgrades without data loss, foreign-key enforcement, restrictive deletes, idempotency, and audit timestamp population.
