# Super Dentist
Modern WPF clinic management for dentists — fast scheduling, clean records, and reliable reporting in a single desktop app.

Why this exists: many small clinics still rely on spreadsheets or legacy software. Super Dentist is a clean, modern desktop alternative built to be fast, understandable, and maintainable.

## Screenshots
Add screenshots to `docs/screenshots/` and keep the filenames below so the README renders correctly.

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

## Overview
Super Dentist is a desktop application for small to mid-size dental clinics. It streamlines daily operations like managing doctors, patients, treatments, and appointments, with built-in reports to keep the team informed. It’s designed to be simple for front‑desk workflows while still solidly engineered for maintainability.

## Key Features
- Modern WPF desktop app built with .NET 8
- Clean MVVM architecture + Dependency Injection
- SQLite database (auto-create + seeded demo data)
- Strong input validation with friendly inline errors
- Navigation and search/filter lists
- Appointment conflict detection (prevents double‑booking)
- Structured logging for troubleshooting
- Automated tests for core services

## Tech Stack
- C#, .NET 8
- WPF
- MVVM (CommunityToolkit.Mvvm)
- Dependency Injection (Microsoft.Extensions.*)
- SQLite
- Serilog logging
- xUnit tests

## Quick Start
Requirements:
- Windows 10/11
- .NET 8 SDK (or Visual Studio 2022)

Run the app:
1. `dotnet build "Super Dentist.sln"`
2. `dotnet run --project src/SuperDentist.App/SuperDentist.App.csproj`

First run behavior:
- A local SQLite database is created automatically
- Demo data is seeded so the app looks alive immediately

SQLite and logs:
- Default DB: `%LOCALAPPDATA%\SuperDentist\superdentist.db`
- Override DB path: set `Database:Path` in `src/SuperDentist.App/appsettings.json` or `SUPERDENTIST_DB_PATH`
- Logs: `%LOCALAPPDATA%\SuperDentist\logs\superdentist.log`

Run tests:
`dotnet test "tests/SuperDentist.Tests/SuperDentist.Tests.csproj"`

## Architecture Overview
Layer diagram:
```
App (WPF UI)
   └── Core (domain models + interfaces)
         └── Infrastructure (SQLite + repositories + services)
```

Responsibilities:
- App: Views, ViewModels, navigation, validation, and UX
- Core: domain entities and service/repository contracts
- Infrastructure: SQLite database, repositories, and data initialization

Patterns used:
- MVVM with `ObservableValidator` and command-based actions
- Repository + service interfaces for clean boundaries
- DI for ViewModels and services

## Demo Data + Domain Model
Seeded demo data is created on first run (doctors, patients, treatments, appointments, patient treatments).

Main entities:
- Doctor: profile + specialization + salary
- Patient: profile + assigned doctor + treatment status
- Treatment: catalog of procedures and pricing
- Appointment: date/time with doctor + patient
- PatientTreatment: treatment status and billing info

## Quality & Reliability
- Validation rules: required fields, numeric formats, email format, date/time formats
- Errors are shown inline; forms reset cleanly after successful saves
- Global exception handling with clear user messages
- Logs written to `%LOCALAPPDATA%\SuperDentist\logs\superdentist.log`

## Docs
- Architecture details: `docs/ARCHITECTURE.md`
