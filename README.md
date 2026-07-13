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
- SQLite database (auto-create + seeded demo data)
- Strong input validation with friendly inline errors
- Navigation and search/filter lists
- Appointment conflict detection (prevents double‑booking)
- Structured logging for troubleshooting
- Automated unit and integration tests for Application services and SQLite behavior

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
- Demo data is seeded so the app looks alive immediately

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
- SuperDentist.App: WPF Views/ViewModels, composition root, DI host, navigation, validation, logging setup, and user messaging
- SuperDentist.Application: business use cases and service implementations for doctors, patients, treatments, appointments, and patient treatments
- SuperDentist.Core: domain entities, repository/service contracts, shared results, and options
- SuperDentist.Infrastructure: SQLite connection management, schema initialization, demo seeding, and repository implementations
- SuperDentist.Tests: unit and integration tests

Patterns used:
- MVVM with `ObservableValidator` and command-based actions
- Repository + service interfaces for clean boundaries
- DI for ViewModels, Application services, and Infrastructure repositories

## 🗄 Data & Seeding
Seeded demo data is created on first run (doctors, patients, treatments, appointments, patient treatments).

Main entities:
- Doctor: profile + specialization + salary
- Patient: profile + assigned doctor + treatment status
- Treatment: catalog of procedures and pricing
- Appointment: date/time with doctor + patient
- PatientTreatment: treatment status and billing info

## ✅ Quality & Reliability
- Validation rules: required fields, numeric formats, email format, date/time formats
- Errors are shown inline; forms reset cleanly after successful saves
- Global exception handling with clear user messages
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
- Role‑based access and audit trails

## 📚 Docs
- Architecture details: `docs/ARCHITECTURE.md`
