# Super Dentist
[![CI](https://github.com/swarka7/super-dentist-clinic-app/actions/workflows/ci.yml/badge.svg)](https://github.com/swarka7/super-dentist-clinic-app/actions/workflows/ci.yml)

Full-stack .NET 8 clinic management with a WPF operations client, ASP.NET Core reporting API, and React operations dashboard.

Why this exists: many small clinics still rely on spreadsheets or legacy software. Super Dentist is a clean, modern clinic platform built to be fast, understandable, and maintainable.

## 📸 Screenshots
WPF client captures included in the repository:

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

Web portfolio captures should use these exact paths when captured from live API data:

1. Operations dashboard
   `docs/screenshots/screenshot-web-dashboard.png`
2. Read-only audit history
   `docs/screenshots/screenshot-web-audit.png`

## 🚀 Overview
Super Dentist is a clinic management platform for small to mid-size dental clinics. Its WPF client handles daily write operations such as doctors, patients, treatments, and appointments. A read-only ASP.NET Core API serves the React operations dashboard and other reporting consumers without duplicating business logic. The platform is designed for simple front-desk workflows with maintainable engineering boundaries.

## ✨ Key Features
- Modern WPF desktop app built with .NET 8
- Read-only ASP.NET Core Web API with OpenAPI/Swagger
- Responsive React and TypeScript clinic operations dashboard
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
- React, TypeScript, and Vite
- OpenAPI / Swagger
- MVVM (CommunityToolkit.Mvvm)
- Dependency Injection (Microsoft.Extensions.*)
- SQLite
- Serilog logging
- xUnit tests
- Vitest and Testing Library
- GitHub Actions CI and build artifacts

## ⚡ Quick Start
Requirements:
- Windows 10/11 to run the WPF client; the API and React client are cross-platform
- The .NET SDK selected by `global.json`
- The Node.js version selected by `.nvmrc`

Install dependencies once:
```powershell
dotnet restore "Super Dentist.sln"
Push-Location src/SuperDentist.Web
npm ci
Pop-Location
```

Start the API and React dashboard together:
```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\start-dev.ps1
```

The dependency-free cross-platform alternative is:
```shell
node scripts/start-dev.mjs
```

Both launchers verify required tools, build the API, start only the API and Vite child processes they own, and stop those children on exit. The PowerShell command bypasses signing policy only for that process and does not alter machine policy. They expose:
- Dashboard: `http://localhost:5173`
- API: `http://localhost:5080`
- Swagger: `http://localhost:5080/swagger`
- Health: `http://localhost:5080/health`

Run the WPF write client separately:
1. `dotnet build "Super Dentist.sln"`
2. `dotnet run --project src/SuperDentist.App/SuperDentist.App.csproj`

The application project is `SuperDentist.App`. The executable/assembly is branded as `Super Dentist`, so local build output is named `Super Dentist.exe`.

Manual API startup:
1. `dotnet run --project src/SuperDentist.Api/SuperDentist.Api.csproj`
2. Open `http://localhost:5080/swagger` or call `http://localhost:5080/health`.

The development API URL is defined in `src/SuperDentist.Api/Properties/launchSettings.json` and can be overridden with standard ASP.NET Core configuration such as `ASPNETCORE_URLS`. Development CORS permits only `http://localhost:5173` by default; production does not enable a permissive CORS policy.

Manual React startup in a second terminal:
1. `cd src/SuperDentist.Web`
2. `npm ci`
3. `npm run dev`
4. Open `http://localhost:5173`.

The typed frontend API client defaults to `http://localhost:5080`. Set `VITE_API_BASE_URL` in a local environment or local `.env` file to override it; `.env.example` contains the non-secret development example. Vite environment files other than `.env.example` are ignored.

First run behavior:
- A local SQLite database is created automatically
- Schema migrations run before the app starts using the database
- Demo data is seeded only after migrations complete successfully

SQLite and logs:
- Default DB: `%LOCALAPPDATA%\SuperDentist\superdentist.db`
- Override DB path: set `Database:Path` in the active App/API configuration or `SUPERDENTIST_DB_PATH`
- Logs: `%LOCALAPPDATA%\SuperDentist\logs\superdentist.log`

## 🧪 Testing
Run the complete repository verification on Windows:
```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\verify-all.ps1
```

The focused scripts are `scripts/verify-backend.ps1` and `scripts/verify-frontend.ps1`. The backend script restores, performs a Release build with warnings promoted to errors, and runs all .NET tests. The frontend script performs a clean lockfile install, type checking, linting, tests, and a production build.

Equivalent backend commands:
```powershell
dotnet restore "Super Dentist.sln"
dotnet build "Super Dentist.sln" -c Release --no-restore -warnaserror
dotnet test "Super Dentist.sln" -c Release --no-build --no-restore
```

Equivalent frontend commands from `src/SuperDentist.Web`:
```powershell
npm ci
npm run typecheck
npm run lint
npm test
npm run build
```

The test suite includes:
- Application service unit tests using simple fake repositories, proving business use cases can be tested without SQLite.
- Integration-style appointment tests that run against isolated temporary SQLite databases.
- Deterministic, test-owned doctors, patients, treatments, and appointments instead of production demo seed data.
- Migration tests for empty databases, baseline upgrades, idempotency, foreign-key enforcement, restrictive deletes, audit timestamps, and the version 3 audit trail.
- Audit tests for actor and UTC capture, deterministic JSON snapshots, filtering, newest-first ordering, transaction rollback, initialization idempotency, and persistence across reopened connections.
- API integration tests that start the real ASP.NET Core host against isolated, migrated temporary SQLite databases.
- SQLite-free dashboard aggregation tests with deterministic service fakes.
- Frontend behavior tests for dashboard states, retry, search, filters, audit JSON inspection, and pagination with the API boundary mocked.

## Continuous Integration
`.github/workflows/ci.yml` runs for pushes to `main`, pull requests targeting `main`, and manual dispatches. Its independent jobs are:
- Backend on Windows: cached restore, warning-as-error Release build, .NET tests, and a published API artifact.
- Frontend on Linux: cached `npm ci`, type checking, linting, one-shot tests, production build, and a `dist` artifact.

CI has read-only repository permissions and contains no deployment credentials. Artifacts are verification outputs, not a deployment pipeline.

## 🧱 Architecture Overview
Layer diagram:
```
SuperDentist.Web --REST/JSON--> SuperDentist.Api

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
- SuperDentist.Web: React/TypeScript read-only dashboard, typed API client, responsive routes, filters, and audit inspection
- SuperDentist.Application: business use cases, audited entity operations, bounded clinic queries, dashboard aggregation, actor resolution, deterministic snapshot serialization, and audit search
- SuperDentist.Core: domain entities, audit models/query types, repository/service/transaction contracts, shared results, and options
- SuperDentist.Infrastructure: SQLite connection and transaction management, schema migrations, demo seeding, audit persistence, and repository implementations
- SuperDentist.Tests: unit and integration tests
- SuperDentist.Web tests: Vitest and Testing Library behavior tests with the API boundary mocked

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

## Read-only React Dashboard
`SuperDentist.Web` is a second client alongside WPF. It communicates only through REST/JSON with `SuperDentist.Api`; it has no direct reference or access to Application, Infrastructure, SQLite, or the WPF process.

Routes:
- `/`: operational metrics, upcoming appointments, doctor workload, treatment value, and recent audit activity.
- `/doctors`: bounded searchable doctor directory; salary is not requested or displayed.
- `/patients`: bounded patient directory with assigned-doctor and treatment-status context.
- `/appointments`: bounded schedule with text, doctor, patient, and date-range filters.
- `/audit`: newest-first audit records with combinable filters and read-only before/after JSON inspection.

The frontend centralizes HTTP calls, DTOs, ASP.NET Problem Details handling, and request cancellation in `src/api`. Pages include loading, empty, retryable error, responsive table, and keyboard-focus states. Audit timestamps are stored by the API in UTC and displayed in the browser's local time zone with both semantics labeled. Dashboard monetary values are displayed as USD because the current domain model does not yet carry a clinic currency setting.

This web client is intentionally read-only. WPF remains the operational write client, and the API exposes no mutation endpoints. Browser refresh and direct routes require a production static host configured to fall back to `index.html`.

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

## Current Limitations
- Authentication and authorization are not implemented; the API and dashboard are for trusted/local evaluation only.
- `LocalUser` is an audit label, not verified identity.
- SQLite is appropriate for this single-clinic deployment model, not horizontally scaled multi-node writes.
- The React client and API are read-only; WPF remains the only write client.
- Application paging currently follows repository reads rather than database projections.
- Appointment dates remain legacy strings and use the API host's local date; no clinic time-zone setting exists.
- Currency presentation currently assumes USD because the domain has no clinic currency configuration.
- CI publishes verification artifacts but performs no environment deployment.

## 📚 Docs
- Architecture details: `docs/ARCHITECTURE.md`
