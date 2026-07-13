namespace SuperDentist.Infrastructure.Data
{
    internal static class SqliteSchema
    {
        public const int BaselineVersion = 1;
        public const int IntegrityVersion = 2;
        public const int LatestVersion = IntegrityVersion;

        public const string MigrationTableName = "SchemaMigrations";

        public const string BaselineSchemaSql = @"
CREATE TABLE Doctors (
    Id TEXT PRIMARY KEY,
    FirstName TEXT NOT NULL,
    LastName TEXT NOT NULL,
    Phone TEXT,
    Address TEXT,
    Email TEXT,
    Specialization TEXT,
    Salary INTEGER NOT NULL DEFAULT 0
);

CREATE TABLE Patients (
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

CREATE TABLE Treatments (
    Number TEXT PRIMARY KEY,
    Type TEXT,
    Price INTEGER NOT NULL DEFAULT 0,
    Tools TEXT
);

CREATE TABLE Appointments (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    PatientId TEXT NOT NULL,
    DoctorId TEXT NOT NULL,
    Date TEXT NOT NULL,
    Time TEXT NOT NULL,
    TreatmentNumber TEXT
);

CREATE TABLE PatientTreatments (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    PatientId TEXT NOT NULL,
    TreatmentNumber TEXT NOT NULL,
    IsCompleted TEXT,
    IsPaid TEXT,
    StartDate TEXT
);

CREATE UNIQUE INDEX IX_Appointments_PatientId ON Appointments (PatientId);
CREATE UNIQUE INDEX IX_Appointments_DoctorSlot ON Appointments (DoctorId, Date, Time);
CREATE INDEX IX_PatientTreatments_PatientId ON PatientTreatments (PatientId);
";

        public const string IntegrityUpgradeSql = @"
ALTER TABLE PatientTreatments RENAME TO PatientTreatments_Legacy;
ALTER TABLE Appointments RENAME TO Appointments_Legacy;
ALTER TABLE Patients RENAME TO Patients_Legacy;
ALTER TABLE Treatments RENAME TO Treatments_Legacy;
ALTER TABLE Doctors RENAME TO Doctors_Legacy;

CREATE TABLE Doctors (
    Id TEXT PRIMARY KEY,
    FirstName TEXT NOT NULL,
    LastName TEXT NOT NULL,
    Phone TEXT,
    Address TEXT,
    Email TEXT,
    Specialization TEXT,
    Salary INTEGER NOT NULL DEFAULT 0,
    CreatedAtUtc TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAtUtc TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE Treatments (
    Number TEXT PRIMARY KEY,
    Type TEXT,
    Price INTEGER NOT NULL DEFAULT 0,
    Tools TEXT,
    CreatedAtUtc TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAtUtc TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE Patients (
    Id TEXT PRIMARY KEY,
    FirstName TEXT NOT NULL,
    LastName TEXT NOT NULL,
    Address TEXT,
    Phone TEXT,
    Email TEXT,
    Age INTEGER NOT NULL DEFAULT 0,
    TreatmentStatus TEXT,
    DoctorId TEXT,
    CreatedAtUtc TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAtUtc TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT FK_Patients_Doctors
        FOREIGN KEY (DoctorId) REFERENCES Doctors(Id)
        ON UPDATE RESTRICT ON DELETE RESTRICT
);

CREATE TABLE Appointments (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    PatientId TEXT NOT NULL,
    DoctorId TEXT NOT NULL,
    Date TEXT NOT NULL,
    Time TEXT NOT NULL,
    TreatmentNumber TEXT,
    CreatedAtUtc TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAtUtc TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT FK_Appointments_Patients
        FOREIGN KEY (PatientId) REFERENCES Patients(Id)
        ON UPDATE RESTRICT ON DELETE RESTRICT,
    CONSTRAINT FK_Appointments_Doctors
        FOREIGN KEY (DoctorId) REFERENCES Doctors(Id)
        ON UPDATE RESTRICT ON DELETE RESTRICT,
    CONSTRAINT FK_Appointments_Treatments
        FOREIGN KEY (TreatmentNumber) REFERENCES Treatments(Number)
        ON UPDATE RESTRICT ON DELETE RESTRICT
);

CREATE TABLE PatientTreatments (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    PatientId TEXT NOT NULL,
    TreatmentNumber TEXT NOT NULL,
    IsCompleted TEXT,
    IsPaid TEXT,
    StartDate TEXT,
    CreatedAtUtc TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAtUtc TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT FK_PatientTreatments_Patients
        FOREIGN KEY (PatientId) REFERENCES Patients(Id)
        ON UPDATE RESTRICT ON DELETE RESTRICT,
    CONSTRAINT FK_PatientTreatments_Treatments
        FOREIGN KEY (TreatmentNumber) REFERENCES Treatments(Number)
        ON UPDATE RESTRICT ON DELETE RESTRICT
);

INSERT INTO Doctors (Id, FirstName, LastName, Phone, Address, Email, Specialization, Salary, CreatedAtUtc, UpdatedAtUtc)
SELECT Id, FirstName, LastName, Phone, Address, Email, Specialization, Salary, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
FROM Doctors_Legacy;

INSERT INTO Treatments (Number, Type, Price, Tools, CreatedAtUtc, UpdatedAtUtc)
SELECT Number, Type, Price, Tools, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
FROM Treatments_Legacy;

INSERT INTO Patients (Id, FirstName, LastName, Address, Phone, Email, Age, TreatmentStatus, DoctorId, CreatedAtUtc, UpdatedAtUtc)
SELECT Id, FirstName, LastName, Address, Phone, Email, Age, TreatmentStatus, NULLIF(DoctorId, ''), CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
FROM Patients_Legacy;

INSERT INTO Appointments (Id, PatientId, DoctorId, Date, Time, TreatmentNumber, CreatedAtUtc, UpdatedAtUtc)
SELECT Id, PatientId, DoctorId, Date, Time, NULLIF(TreatmentNumber, ''), CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
FROM Appointments_Legacy;

INSERT INTO PatientTreatments (Id, PatientId, TreatmentNumber, IsCompleted, IsPaid, StartDate, CreatedAtUtc, UpdatedAtUtc)
SELECT Id, PatientId, TreatmentNumber, IsCompleted, IsPaid, StartDate, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
FROM PatientTreatments_Legacy;

DROP TABLE PatientTreatments_Legacy;
DROP TABLE Appointments_Legacy;
DROP TABLE Patients_Legacy;
DROP TABLE Treatments_Legacy;
DROP TABLE Doctors_Legacy;

CREATE UNIQUE INDEX IX_Appointments_PatientId ON Appointments (PatientId);
CREATE UNIQUE INDEX IX_Appointments_DoctorSlot ON Appointments (DoctorId, Date, Time);
CREATE INDEX IX_PatientTreatments_PatientId ON PatientTreatments (PatientId);
";
    }
}