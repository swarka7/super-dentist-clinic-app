# Architecture Overview

SuperDentist follows a clean MVVM architecture with clear separation between UI, domain logic, and data access.

## Solution Layout
- `src/SuperDentist.App`
  - WPF UI (Views + ViewModels)
  - DI, navigation, theming, validation, and user messaging
- `src/SuperDentist.Core`
  - Domain models (Doctor, Patient, Treatment, Appointment, PatientTreatment)
  - Repository and service interfaces
  - Shared results and options
- `src/SuperDentist.Infrastructure`
  - SQLite database initialization + seeding
  - SQLite repositories and service implementations
- `tests/SuperDentist.Tests`
  - xUnit tests for services and SQLite behavior

## UI + Navigation
- `MainWindow` hosts a `ContentControl` bound to `ShellViewModel.CurrentViewModel`.
- `NavigationService` swaps ViewModels, and XAML DataTemplates map ViewModels to Views.

## Data Access
- SQLite database stored under `%LOCALAPPDATA%\SuperDentist\superdentist.db` by default.
- First run creates schema and seeds demo data.
- Repository interfaces live in Core; SQLite implementations live in Infrastructure.

## Domain Models
- `Doctor`: Id, FirstName, LastName, Phone, Address, Email, Specialization, Salary
- `Patient`: Id, FirstName, LastName, Phone, Address, Email, Age, TreatmentStatus, DoctorId
- `Treatment`: Number, Type, Price, Tools
- `Appointment`: PatientId, DoctorId, Date, Time, TreatmentNumber
- `PatientTreatment`: PatientId, TreatmentNumber, IsCompleted, IsPaid, StartDate

## Startup Flow
1. App builds a DI host + config + logging.
2. Database initializer creates schema + demo data if needed.
3. MainWindow starts with Doctors view.

## Logging
- Serilog writes logs to `%LOCALAPPDATA%\SuperDentist\logs\superdentist.log`
