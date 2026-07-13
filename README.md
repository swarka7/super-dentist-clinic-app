# Super Dentist
Modern WPF clinic management for dentists — fast scheduling, clean records, and reliable reporting in a single desktop app.

Why this exists: many small clinics still rely on spreadsheets or legacy software. Super Dentist is a clean, modern desktop alternative built to be fast, understandable, and maintainable.

## 📸 Screenshots
1. Dashboard / Today view  
   `docs/screenshots/screenshot-dashboard.png`
2. Doctors management  
   `docs/screenshots/screenshot-doctors.png`
3. Patients management + validation  
   `docs/screenshots/screenshot-patients.png`
4. Appointments + conflict validation  
   `docs/screenshots/screenshot-appointments.png`
5. Treatments catalog  
   `docs/screenshots/screenshot-treatments.png`
6. Patient treatments  
   `docs/screenshots/screenshot-patient-treatments.png`
7. Reports  
   `docs/screenshots/screenshot-reports.png`

## 🚀 Overview
Super Dentist is a desktop application for small to mid-size dental clinics. It streamlines daily operations like managing doctors, patients, treatments, and appointments, with built-in reports to keep the team informed. It’s designed to be simple for front‑desk workflows while still solidly engineered for maintainability.

## ✨ Key Features
- Modern WPF desktop app built with .NET 8
- Clean MVVM architecture + Dependency Injection
- Application layer for business use cases and service implementations
- Versioned SQLite schema migrations with in-place upgrades
- SQLite foreign-key enforcement for clinic relationships
- SQLite database auto-create + seeded demo data after successful migrations
- Strong input validation with friendly inline errors
- Navigation and search/filter lists
- Appointment conflict detection (prevents double‑booking)
- Persistent, searchable, read-only audit history for clinic data changes
- Structured logging for troubleshooting
- Automated unit and integration tests for Application services, migrations, and SQLite behavior

## 🛠 Tech Stack
- C#, .NET 8
- WPF
- MVVM (CommunityToolkit.Mvvm)
- Dependency Injection (Microsoft.Extensions.*)
- SQLite
- Serilog logging
- xUnit tests

## ⚡ Quick Start
Requirements:
- Windows 10/11
- .NET 8 SDK (or Visual Studio 2022)

Run the app:
1. `dotnet build "Super Dentist.sln"`
2. `dotnet run --project src/SuperDentist.App/SuperDentist.App.csproj`

The application project is `SuperDentist.App`. The executable/assembly is branded as `Super Dentist`, so local build output is named `Super Dentist.exe`.

First run behavior:
- A local SQLite database is created automatically
- Schema migrations run before the app starts using the database
- Demo data is seeded only after migrations complete successfully

SQLite and logs:
- Default DB: `%LOCALAPPDATA%\SuperDentist\superdentist.db`
- Override DB path: set `Database:Path` in `src/SuperDentist.App/appsettings.json` or `SUPERDENTIST_DB_PATH`
- Logs: `%LOCALAPPDATA%\SuperDentist\logs\superdentist.log`

## 🧪 Testing
Run tests:
`dotnet test "Super Dentist.sln"`

The test suite includes:
- Application service unit tests using simple fake repositories, proving business use cases can be tested without SQLite.
- Integration-style appointment tests that run against isolated temporary SQLite databases.
- Deterministic, test-owned doctors, patients, treatments, and appointments instead of production demo seed data.
- Migration tests for empty databases, baseline upgrades, idempotency, foreign-key enforcement, restrictive deletes, audit timestamps, and the version 3 audit trail.
- Audit tests for actor and UTC capture, deterministic JSON snapshots, filtering, newest-first ordering, transaction rollback, initialization idempotency, and persistence across reopened connections.

## 🧱 Architecture Overview
Layer diagram:
```
SuperDentist.App
  -> SuperDentist.Application
  -> SuperDentist.Infrastructure
  -> SuperDentist.Core

SuperDentist.Application -> SuperDentist.Core
SuperDentist.Infrastructure -> SuperDentist.Core
```

Responsibilities:
- SuperDentist.App: WPF Views/ViewModels, composition root, DI host, navigation, validation, logging setup, user messaging, and read-only audit history
- SuperDentist.Application: business use cases, audited entity operations, actor resolution, deterministic snapshot serialization, and audit search
- SuperDentist.Core: domain entities, audit models/query types, repository/service/transaction contracts, shared results, and options
- SuperDentist.Infrastructure: SQLite connection and transaction management, schema migrations, demo seeding, audit persistence, and repository implementations
- SuperDentist.Tests: unit and integration tests

Patterns used:
- MVVM with `ObservableValidator` and command-based actions
- Repository + service interfaces for clean boundaries
- DI for ViewModels, Application services, Infrastructure repositories, and database initialization
- Application transaction boundary for atomic clinic changes and audit inserts

## 🗄 Data, Migrations & Seeding
Schema versioning is managed by the Infrastructure layer with a dedicated `SchemaMigrations` table.

Current migrations:
- Version 1: baseline schema matching the original SQLite table layout.
- Version 2: foreign-key constraints plus `CreatedAtUtc` and `UpdatedAtUtc` audit columns on mutable clinic entities.
- Version 3: append-only application audit trail with indexes for timestamp, entity, actor, and operation searches.

Upgrade behavior:
- New databases are created by running migrations in order.
- Existing unversioned databases are adopted as version 1, then upgraded incrementally.
- Existing user databases are not deleted or recreated during normal initialization.
- Existing rows receive valid UTC timestamp values during the integrity migration.
- Demo data seeding runs only after migrations succeed and only when the database has no doctors.

Delete policies:
- Patient → Doctor: `ON DELETE RESTRICT`
- Appointment → Patient, Doctor, Treatment: `ON DELETE RESTRICT`
- PatientTreatment → Patient, Treatment: `ON DELETE RESTRICT`

The restrictive policy prevents silent cascade deletion of medical and scheduling records.

Main entities:
- Doctor: profile + specialization + salary + audit timestamps
- Patient: profile + assigned doctor + treatment status + audit timestamps
- Treatment: catalog of procedures and pricing + audit timestamps
- Appointment: date/time with doctor + patient + optional treatment + audit timestamps
- PatientTreatment: treatment status and billing info + audit timestamps

## Audit Trail
Successful create, update, and delete operations for Doctors, Patients, Treatments, Appointments, and PatientTreatments produce persistent audit entries. Validation failures and rejected appointment conflicts do not create misleading success records.

Each entry records entity type and identifier, a `Created`, `Updated`, or `Deleted` operation, actor, UTC timestamp, structured JSON before/after snapshots, and a correlation ID.

Snapshot fields:
- Doctor: Id, FirstName, LastName, Phone, Address, Email, Specialization, Salary.
- Patient: Id, FirstName, LastName, Phone, Address, Email, Age, TreatmentStatus, DoctorId.
- Treatment: Number, Type, Price, Tools.
- Appointment: PatientId, DoctorId, Date, Time, TreatmentNumber.
- PatientTreatment: PatientId, TreatmentNumber, IsCompleted, IsPaid, StartDate.

Entity persistence timestamps, configuration, connection strings, secrets, logs, exceptions, and stack traces are excluded from snapshots. Audit entries are append-only: the Core repository contract exposes no update/delete operations, and SQLite triggers reject direct updates or deletes.

The audit query API supports entity type, partial entity ID, actor, operation, UTC date range, newest-first ordering, and record limits. The read-only Audit History screen exposes the entity, ID, actor, and operation filters, displays both UTC and local timestamps, and shows the selected before/after JSON.

The current actor provider returns `LocalUser`. This is a replaceable application abstraction for traceability on a single-user desktop installation, not secure identity verification or authentication. A future authentication milestone can replace it with an authenticated user/role provider without changing audit consumers.

The clinic mutation and required audit insert execute in one SQLite transaction. If audit persistence fails, the clinic mutation is rolled back rather than silently succeeding without history.
## ✅ Quality & Reliability
- Validation rules: required fields, numeric formats, email format, date/time formats
- Errors are shown inline; forms reset cleanly after successful saves
- Global exception handling with clear user messages
- Migrations run transactionally and are recorded only after success
- SQLite connections enable `PRAGMA foreign_keys = ON`
- Logs written to `%LOCALAPPDATA%\SuperDentist\logs\superdentist.log`

## 🧠 What I Learned
- Refactoring legacy WPF into clean MVVM
- Designing data layers with clear interfaces
- Building validation and UX that feels polished
- Structuring a desktop app for long‑term maintainability
- Building a reliable data initialization pipeline with logging
- Creating responsive, accessible layouts in WPF
- Managing command state and validation lifecycle correctly

## 🔮 Future Improvements
- Calendar view with drag‑and‑drop scheduling
- Advanced reporting and export (CSV/PDF)
- Authenticated users and role-based authorization integrated with the existing actor abstraction

## 📚 Docs
- Architecture details: `docs/ARCHITECTURE.md`
