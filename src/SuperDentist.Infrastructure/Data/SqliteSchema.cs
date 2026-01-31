namespace SuperDentist.Infrastructure.Data
{
    internal static class SqliteSchema
    {
        public const string SchemaSql = @"
CREATE TABLE IF NOT EXISTS Doctors (
    Id TEXT PRIMARY KEY,
    FirstName TEXT NOT NULL,
    LastName TEXT NOT NULL,
    Phone TEXT,
    Address TEXT,
    Email TEXT,
    Specialization TEXT,
    Salary INTEGER NOT NULL DEFAULT 0
);

CREATE TABLE IF NOT EXISTS Patients (
    Id TEXT PRIMARY KEY,
    FirstName TEXT NOT NULL,
    LastName TEXT NOT NULL,
    Address TEXT,
    Phone TEXT,
    Email TEXT,
    Age INTEGER NOT NULL DEFAULT 0,
    TreatmentStatus TEXT,
    DoctorId TEXT
);

CREATE TABLE IF NOT EXISTS Treatments (
    Number TEXT PRIMARY KEY,
    Type TEXT,
    Price INTEGER NOT NULL DEFAULT 0,
    Tools TEXT
);

CREATE TABLE IF NOT EXISTS Appointments (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    PatientId TEXT NOT NULL,
    DoctorId TEXT NOT NULL,
    Date TEXT NOT NULL,
    Time TEXT NOT NULL,
    TreatmentNumber TEXT
);

CREATE TABLE IF NOT EXISTS PatientTreatments (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    PatientId TEXT NOT NULL,
    TreatmentNumber TEXT NOT NULL,
    IsCompleted TEXT,
    IsPaid TEXT,
    StartDate TEXT
);

CREATE UNIQUE INDEX IF NOT EXISTS IX_Appointments_PatientId ON Appointments (PatientId);
CREATE UNIQUE INDEX IF NOT EXISTS IX_Appointments_DoctorSlot ON Appointments (DoctorId, Date, Time);
CREATE INDEX IF NOT EXISTS IX_PatientTreatments_PatientId ON PatientTreatments (PatientId);
";
    }
}
