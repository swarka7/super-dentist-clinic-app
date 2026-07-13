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
  - SQLite database initialization + seeding
  - SQLite connection management and repository implementations
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
- First run creates schema and seeds demo data.
- Repository interfaces live in Core; SQLite implementations live in Infrastructure.
- Infrastructure remains responsible for technical persistence concerns: connections, schema, seeding, SQL, and SQLite-specific mapping.

## Domain Models
- `Doctor`: Id, FirstName, LastName, Phone, Address, Email, Specialization, Salary
- `Patient`: Id, FirstName, LastName, Phone, Address, Email, Age, TreatmentStatus, DoctorId
- `Treatment`: Number, Type, Price, Tools
- `Appointment`: PatientId, DoctorId, Date, Time, TreatmentNumber
- `PatientTreatment`: PatientId, TreatmentNumber, IsCompleted, IsPaid, StartDate

## Startup Flow
1. App builds a DI host + config + logging.
2. App registers Application services and Infrastructure repositories.
3. Database initializer creates schema + demo data if needed.
4. MainWindow starts with Doctors view.

## Logging
- Serilog writes logs to `%LOCALAPPDATA%\SuperDentist\logs\superdentist.log`

## Testing
- Application service unit tests can run with simple fake repositories and no SQLite dependency.
- Appointment conflict tests use deterministic, test-owned data rather than production demo seed data.
- SQLite-backed tests create isolated temporary databases and clean them up afterward.
