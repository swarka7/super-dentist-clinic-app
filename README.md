# Super Dentist
Modern .NET 8 clinic management with a WPF operations client and a read-only ASP.NET Core reporting API.

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
Super Dentist is a clinic management platform for small to mid-size dental clinics. Its WPF client handles daily operations such as doctors, patients, treatments, and appointments, while its read-only API exposes reporting data for additional clients. It is designed for simple front-desk workflows with maintainable engineering boundaries.

## ✨ Key Features
- Modern WPF desktop app built with .NET 8
- Read-only ASP.NET Core Web API with OpenAPI/Swagger
- Clean MVVM architecture + Dependency Injection
- Application layer for business use cases and service implementations
- Application-owned clinic list queries and dashboard aggregation
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
- ASP.NET Core Web API
- OpenAPI / Swagger
- MVVM (CommunityToolkit.Mvvm)
- Dependency Injection (Microsoft.Extensions.*)
- SQLite
- Serilog logging
- xUnit tests

## ⚡ Quick Start
Requirements:
- Windows 10/11
- .NET 8 SDK (or Visual Studio 2022)

Run the WPF app:
1. `dotnet build "Super Dentist.sln"`
2. `dotnet run --project src/SuperDentist.App/SuperDentist.App.csproj`

The application project is `SuperDentist.App`. The executable/assembly is branded as `Super Dentist`, so local build output is named `Super Dentist.exe`.

Run the reporting API:
1. `dotnet run --project src/SuperDentist.Api/SuperDentist.Api.csproj`
2. Open `http://localhost:5080/swagger` or call `http://localhost:5080/health`.

The development API URL is defined in `src/SuperDentist.Api/Properties/launchSettings.json` and can be overridden with standard ASP.NET Core configuration such as `ASPNETCORE_URLS`. Development CORS permits only `http://localhost:5173` by default; production does not enable a permissive CORS policy.

First run behavior:
- A local SQLite database is created automatically
- Schema migrations run before the app starts using the database
- Demo data is seeded only after migrations complete successfully

SQLite and logs:
- Default DB: `%LOCALAPPDATA%\SuperDentist\superdentist.db`
- Override DB path: set `Database:Path` in the active App/API configuration or `SUPERDENTIST_DB_PATH`
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
- API integration tests that start the real ASP.NET Core host against isolated, migrated temporary SQLite databases.
- SQLite-free dashboard aggregation tests with deterministic service fakes.

## 🧱 Architecture Overview
Layer diagram:
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

Responsibilities:
- SuperDentist.App: WPF Views/ViewModels, composition root, DI host, navigation, validation, logging setup, user messaging, and read-only audit history
- SuperDentist.Api: read-only REST/JSON endpoints, API DTOs, HTTP validation, Swagger, health checks, CORS, and API request/error handling
- SuperDentist.Application: business use cases, audited entity operations, bounded clinic queries, dashboard aggregation, actor resolution, deterministic snapshot serialization, and audit search
- SuperDentist.Core: domain entities, audit models/query types, repository/service/transaction contracts, shared results, and options
- SuperDentist.Infrastructure: SQLite connection and transaction management, schema migrations, demo seeding, audit persistence, and repository implementations
- SuperDentist.Tests: unit and integration tests

Patterns used:
- MVVM with `ObservableValidator` and command-based actions
- Repository + service interfaces for clean boundaries
- DI for ViewModels, Application services, Infrastructure repositories, and database initialization
- Application transaction boundary for atomic clinic changes and audit inserts

## Read-only Reporting API
The API reuses the same Application services, Infrastructure repositories, migrations, and SQLite database as the WPF client. It does not duplicate clinic business rules and exposes no mutation endpoints.

Endpoints:
- `GET /api/doctors` and `GET /api/doctors/{id}`
- `GET /api/patients` and `GET /api/patients/{id}`
- `GET /api/appointments`
- `GET /api/treatments`
- `GET /api/audit`
- `GET /api/dashboard/summary`
- `GET /health`

List endpoints support bounded limits and relevant search, identifier, date-range, actor, entity, and operation filters. The dashboard reports patient and doctor totals, today/upcoming appointments, patient-treatment completion, unpaid treatment value, doctor utilization, treatment usage/value, upcoming appointments, and recent audit activity. Since the current domain has no inactive-doctor flag, all stored doctors are counted as active. Outstanding value means the catalog value of patient-treatment records not marked paid.

The API returns dedicated response DTOs rather than persistence objects; doctor salary is intentionally omitted because the dashboard does not require staff compensation data. Centralized exception handling returns generic problem details without stack traces or internal exception messages, and request logging excludes query strings to avoid unnecessarily recording filter values.

Authentication and authorization are intentionally outside this checkpoint. The API exposes patient and audit data and must be treated as a local/trusted-development service until authenticated access controls are added. Current repository contracts return complete entity lists before Application filtering and paging; response sizes are bounded, but database-side read projections remain a future scalability improvement.

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
