using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using SuperDentist.Core.Services;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SuperDentist.Infrastructure.Data
{
    public sealed class SqliteDatabaseInitializer : IDatabaseInitializer
    {
        private readonly ISqliteConnectionFactory _connectionFactory;
        private readonly SqliteDatabaseMigrator _migrator;
        private readonly ILogger<SqliteDatabaseInitializer> _logger;

        public SqliteDatabaseInitializer(ISqliteConnectionFactory connectionFactory, SqliteDatabaseMigrator migrator, ILogger<SqliteDatabaseInitializer> logger)
        {
            _connectionFactory = connectionFactory;
            _migrator = migrator;
            _logger = logger;
        }

        public async Task<InitializationResult> InitializeAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                string databasePath = _connectionFactory.DatabasePath;
                Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);

                MigrationResult migrationResult = await _migrator.MigrateAsync(cancellationToken).ConfigureAwait(false);

                await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
                bool isNew = !await HasRowsAsync(connection, "Doctors", cancellationToken).ConfigureAwait(false);
                if (isNew)
                {
                    await SeedAsync(connection, cancellationToken).ConfigureAwait(false);
                }

                _logger.LogInformation("SQLite database ready at {DatabasePath} with schema version {SchemaVersion}", databasePath, migrationResult.CurrentVersion);
                return new InitializationResult(isNew, databasePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SQLite initialization failed");
                throw;
            }
        }

private static async Task<bool> HasRowsAsync(SqliteConnection connection, string tableName, CancellationToken cancellationToken)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = $"SELECT 1 FROM {tableName} LIMIT 1;";
            object? result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            return result != null;
        }

        private static async Task SeedAsync(SqliteConnection connection, CancellationToken cancellationToken)
        {
            using var transaction = connection.BeginTransaction();

            string today = DateTime.Today.ToString("yyyy-MM-dd");
            string yesterday = DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd");
            string tomorrow = DateTime.Today.AddDays(1).ToString("yyyy-MM-dd");
            string nextWeek = DateTime.Today.AddDays(7).ToString("yyyy-MM-dd");

            var doctors = new (string Id, string First, string Last, string Phone, string Address, string Email, string Specialty, int Salary)[]
            {
                ("100000001", "Alex", "Example", "5550100001", "1 Demo Blvd", "alex.example@example.com", "Orthodontics", 12000),
                ("100000002", "Taylor", "Sample", "5550100002", "5 Sample Ave", "taylor.sample@example.com", "Pediatric", 11000),
                ("100000003", "Jordan", "Placeholder", "5550100003", "12 Mock St", "jordan.placeholder@example.com", "Cosmetic", 13500),
                ("100000004", "Casey", "Fiction", "5550100004", "88 Test Rd", "casey.fiction@example.com", "Endodontics", 12800),
                ("100000005", "Morgan", "Demo", "5550100005", "17 Example Ave", "morgan.demo@example.com", "General", 9800),
                ("100000006", "Riley", "Mock", "5550100006", "2 Sample Dr", "riley.mock@example.com", "Periodontics", 14200),
                ("100000007", "Jamie", "Imaginary", "5550100007", "44 Placeholder Rd", "jamie.imaginary@example.com", "Implants", 15000),
                ("100000008", "Quinn", "Faux", "5550100008", "9 Demo Blvd", "quinn.faux@example.com", "General", 9500),
                ("100000009", "Avery", "Sample", "5550100009", "21 Test Ln", "avery.sample@example.com", "Prosthodontics", 14500),
                ("100000010", "Parker", "Example", "5550100010", "6 Mock St", "parker.example@example.com", "Oral Surgery", 16000),
                ("100000011", "Reese", "Placeholder", "5550100011", "3 Fiction Ave", "reese.placeholder@example.com", "General", 10200),
                ("100000012", "Skyler", "Demo", "5550100012", "77 Sample Rd", "skyler.demo@example.com", "Pediatric", 10800),
                ("100000013", "Rowan", "Example", "5550100013", "14 Demo St", "rowan.example@example.com", "Cosmetic", 13200),
                ("100000014", "Finley", "Sample", "5550100014", "55 Test Rd", "finley.sample@example.com", "General", 9900),
                ("100000015", "Emerson", "Placeholder", "5550100015", "19 Mock Ave", "emerson.placeholder@example.com", "Endodontics", 13800),
                ("100000016", "Dakota", "Fiction", "5550100016", "31 Example Ln", "dakota.fiction@example.com", "Pediatric", 11200),
                ("100000017", "Harper", "Demo", "5550100017", "8 Sample St", "harper.demo@example.com", "General", 10400),
                ("100000018", "Sage", "Mock", "5550100018", "26 Placeholder Rd", "sage.mock@example.com", "Periodontics", 14600),
                ("100000019", "Ellis", "Imaginary", "5550100019", "63 Fiction Ln", "ellis.imaginary@example.com", "Orthodontics", 15500),
                ("100000020", "Blake", "Faux", "5550100020", "40 Demo Ave", "blake.faux@example.com", "Implants", 15800)
            };

            foreach (var doctor in doctors)
            {
                await InsertDoctorAsync(connection, transaction, doctor.Id, doctor.First, doctor.Last, doctor.Phone, doctor.Address, doctor.Email, doctor.Specialty, doctor.Salary, cancellationToken).ConfigureAwait(false);
            }

            var treatments = new (string Number, string Type, int Price, string Tools)[]
            {
                ("T001", "Cleaning", 200, "Scaler"),
                ("T002", "Filling", 400, "Composite"),
                ("T003", "Root Canal", 1200, "Endo Kit"),
                ("T004", "Crown", 1800, "Ceramic Crown"),
                ("T005", "Whitening", 800, "LED Kit"),
                ("T006", "Implant Consult", 350, "X-Ray"),
                ("T007", "Extraction", 500, "Forceps"),
                ("T008", "Braces Follow-up", 250, "Ortho Kit"),
                ("T009", "Veneers", 2200, "Porcelain"),
                ("T010", "Night Guard", 900, "Mold"),
                ("T011", "Gum Therapy", 650, "Laser"),
                ("T012", "Invisalign Check", 300, "Aligner Kit"),
                ("T013", "Bridge Prep", 1600, "Bridge Kit"),
                ("T014", "Sealant", 180, "Sealant Kit"),
                ("T015", "Emergency Exam", 250, "Exam Kit")
            };

            foreach (var treatment in treatments)
            {
                await InsertTreatmentAsync(connection, transaction, treatment.Number, treatment.Type, treatment.Price, treatment.Tools, cancellationToken).ConfigureAwait(false);
            }

            var patients = new (string Id, string First, string Last, string Address, string Phone, string Email, int Age, string Status, string DoctorId)[]
            {
                ("200000001", "Patient01", "Example", "101 Demo St", "5550101001", "patient01@example.com", 29, "Yes", "100000001"),
                ("200000002", "Patient02", "Example", "102 Demo St", "5550101002", "patient02@example.com", 41, "No", "100000002"),
                ("200000003", "Patient03", "Example", "103 Demo St", "5550101003", "patient03@example.com", 33, "Yes", "100000003"),
                ("200000004", "Patient04", "Example", "104 Demo St", "5550101004", "patient04@example.com", 52, "No", "100000004"),
                ("200000005", "Patient05", "Example", "105 Demo St", "5550101005", "patient05@example.com", 24, "Yes", "100000005"),
                ("200000006", "Patient06", "Example", "106 Demo St", "5550101006", "patient06@example.com", 37, "No", "100000006"),
                ("200000007", "Patient07", "Example", "107 Demo St", "5550101007", "patient07@example.com", 46, "Yes", "100000007"),
                ("200000008", "Patient08", "Example", "108 Demo St", "5550101008", "patient08@example.com", 27, "No", "100000008"),
                ("200000009", "Patient09", "Example", "109 Demo St", "5550101009", "patient09@example.com", 31, "Yes", "100000009"),
                ("200000010", "Patient10", "Example", "110 Demo St", "5550101010", "patient10@example.com", 22, "No", "100000010"),
                ("200000011", "Patient11", "Example", "111 Demo St", "5550101011", "patient11@example.com", 39, "Yes", "100000011"),
                ("200000012", "Patient12", "Example", "112 Demo St", "5550101012", "patient12@example.com", 55, "No", "100000012"),
                ("200000013", "Patient13", "Example", "113 Demo St", "5550101013", "patient13@example.com", 48, "Yes", "100000001"),
                ("200000014", "Patient14", "Example", "114 Demo St", "5550101014", "patient14@example.com", 34, "No", "100000002"),
                ("200000015", "Patient15", "Example", "115 Demo St", "5550101015", "patient15@example.com", 28, "Yes", "100000003"),
                ("200000016", "Patient16", "Example", "116 Demo St", "5550101016", "patient16@example.com", 43, "No", "100000004"),
                ("200000017", "Patient17", "Example", "117 Demo St", "5550101017", "patient17@example.com", 36, "Yes", "100000005"),
                ("200000018", "Patient18", "Example", "118 Demo St", "5550101018", "patient18@example.com", 26, "No", "100000006"),
                ("200000019", "Patient19", "Example", "119 Demo St", "5550101019", "patient19@example.com", 47, "Yes", "100000007"),
                ("200000020", "Patient20", "Example", "120 Demo St", "5550101020", "patient20@example.com", 32, "No", "100000008"),
                ("200000021", "Patient21", "Example", "121 Demo St", "5550101021", "patient21@example.com", 58, "Yes", "100000009"),
                ("200000022", "Patient22", "Example", "122 Demo St", "5550101022", "patient22@example.com", 30, "No", "100000010"),
                ("200000023", "Patient23", "Example", "123 Demo St", "5550101023", "patient23@example.com", 21, "Yes", "100000011"),
                ("200000024", "Patient24", "Example", "124 Demo St", "5550101024", "patient24@example.com", 49, "No", "100000012"),
                ("200000025", "Patient25", "Example", "125 Demo St", "5550101025", "patient25@example.com", 45, "Yes", "100000001"),
                ("200000026", "Patient26", "Example", "126 Demo St", "5550101026", "patient26@example.com", 35, "No", "100000002"),
                ("200000027", "Patient27", "Example", "127 Demo St", "5550101027", "patient27@example.com", 27, "Yes", "100000003"),
                ("200000028", "Patient28", "Example", "128 Demo St", "5550101028", "patient28@example.com", 40, "No", "100000004"),
                ("200000029", "Patient29", "Example", "129 Demo St", "5550101029", "patient29@example.com", 38, "Yes", "100000005"),
                ("200000030", "Patient30", "Example", "130 Demo St", "5550101030", "patient30@example.com", 23, "No", "100000006"),
                ("200000031", "Patient31", "Example", "131 Demo St", "5550101031", "patient31@example.com", 44, "Yes", "100000007"),
                ("200000032", "Patient32", "Example", "132 Demo St", "5550101032", "patient32@example.com", 31, "No", "100000008"),
                ("200000033", "Patient33", "Example", "133 Demo St", "5550101033", "patient33@example.com", 29, "Yes", "100000009"),
                ("200000034", "Patient34", "Example", "134 Demo St", "5550101034", "patient34@example.com", 54, "No", "100000010"),
                ("200000035", "Patient35", "Example", "135 Demo St", "5550101035", "patient35@example.com", 42, "Yes", "100000011"),
                ("200000036", "Patient36", "Example", "136 Demo St", "5550101036", "patient36@example.com", 36, "No", "100000012"),
                ("200000037", "Patient37", "Example", "137 Demo St", "5550101037", "patient37@example.com", 25, "Yes", "100000013"),
                ("200000038", "Patient38", "Example", "138 Demo St", "5550101038", "patient38@example.com", 33, "No", "100000014"),
                ("200000039", "Patient39", "Example", "139 Demo St", "5550101039", "patient39@example.com", 47, "Yes", "100000015"),
                ("200000040", "Patient40", "Example", "140 Demo St", "5550101040", "patient40@example.com", 28, "No", "100000016"),
                ("200000041", "Patient41", "Example", "141 Demo St", "5550101041", "patient41@example.com", 34, "Yes", "100000017"),
                ("200000042", "Patient42", "Example", "142 Demo St", "5550101042", "patient42@example.com", 52, "No", "100000018"),
                ("200000043", "Patient43", "Example", "143 Demo St", "5550101043", "patient43@example.com", 39, "Yes", "100000019"),
                ("200000044", "Patient44", "Example", "144 Demo St", "5550101044", "patient44@example.com", 27, "No", "100000020"),
                ("200000045", "Patient45", "Example", "145 Demo St", "5550101045", "patient45@example.com", 46, "Yes", "100000013"),
                ("200000046", "Patient46", "Example", "146 Demo St", "5550101046", "patient46@example.com", 32, "No", "100000014"),
                ("200000047", "Patient47", "Example", "147 Demo St", "5550101047", "patient47@example.com", 23, "Yes", "100000015"),
                ("200000048", "Patient48", "Example", "148 Demo St", "5550101048", "patient48@example.com", 41, "No", "100000016"),
                ("200000049", "Patient49", "Example", "149 Demo St", "5550101049", "patient49@example.com", 30, "Yes", "100000017"),
                ("200000050", "Patient50", "Example", "150 Demo St", "5550101050", "patient50@example.com", 37, "No", "100000018"),
                ("200000051", "Patient51", "Example", "151 Demo St", "5550101051", "patient51@example.com", 26, "Yes", "100000019"),
                ("200000052", "Patient52", "Example", "152 Demo St", "5550101052", "patient52@example.com", 48, "No", "100000020"),
                ("200000053", "Patient53", "Example", "153 Demo St", "5550101053", "patient53@example.com", 40, "Yes", "100000013"),
                ("200000054", "Patient54", "Example", "154 Demo St", "5550101054", "patient54@example.com", 29, "No", "100000014"),
                ("200000055", "Patient55", "Example", "155 Demo St", "5550101055", "patient55@example.com", 35, "Yes", "100000015"),
                ("200000056", "Patient56", "Example", "156 Demo St", "5550101056", "patient56@example.com", 50, "No", "100000016"),
                ("200000057", "Patient57", "Example", "157 Demo St", "5550101057", "patient57@example.com", 28, "Yes", "100000017"),
                ("200000058", "Patient58", "Example", "158 Demo St", "5550101058", "patient58@example.com", 33, "No", "100000018"),
                ("200000059", "Patient59", "Example", "159 Demo St", "5550101059", "patient59@example.com", 45, "Yes", "100000019"),
                ("200000060", "Patient60", "Example", "160 Demo St", "5550101060", "patient60@example.com", 27, "No", "100000020")
            };

            foreach (var patient in patients)
            {
                await InsertPatientAsync(connection, transaction, patient.Id, patient.First, patient.Last, patient.Address, patient.Phone, patient.Email, patient.Age, patient.Status, patient.DoctorId, cancellationToken).ConfigureAwait(false);
            }

            var timeSlots = new[] { "08:00", "08:30", "09:00", "09:30", "10:00", "10:30", "11:00", "11:30", "12:00", "12:30", "13:00", "13:30", "14:00", "14:30", "15:00", "15:30", "16:00" };
            var dates = new[]
            {
                DateTime.Today.AddDays(-3).ToString("yyyy-MM-dd"),
                DateTime.Today.AddDays(-2).ToString("yyyy-MM-dd"),
                yesterday,
                today,
                tomorrow,
                DateTime.Today.AddDays(2).ToString("yyyy-MM-dd"),
                DateTime.Today.AddDays(3).ToString("yyyy-MM-dd"),
                nextWeek
            };

            int appointmentCount = Math.Min(45, patients.Length);
            for (int i = 0; i < appointmentCount; i++)
            {
                var patient = patients[i];
                string date = dates[i % dates.Length];
                string time = timeSlots[i % timeSlots.Length];
                string doctorId = doctors[i % doctors.Length].Id;
                string treatmentNumber = treatments[i % treatments.Length].Number;

                await InsertAppointmentAsync(connection, transaction, patient.Id, doctorId, date, time, treatmentNumber, cancellationToken).ConfigureAwait(false);
            }

            int patientTreatmentCount = Math.Min(55, patients.Length);
            for (int i = 0; i < patientTreatmentCount; i++)
            {
                var patient = patients[i];
                string treatmentNumber = treatments[i % treatments.Length].Number;
                string startDate = dates[(i + 1) % dates.Length];
                string isCompleted = i % 3 == 0 ? "Yes" : "No";
                string isPaid = i % 4 == 0 ? "Yes" : "No";

                await InsertPatientTreatmentAsync(connection, transaction, patient.Id, treatmentNumber, isCompleted, isPaid, startDate, cancellationToken).ConfigureAwait(false);
            }

            transaction.Commit();
        }

        private static Task InsertDoctorAsync(SqliteConnection connection, SqliteTransaction transaction, string id, string firstName, string lastName, string phone, string address, string email, string specialization, int salary, CancellationToken cancellationToken)
        {
            return ExecuteInsertAsync(
                connection,
                transaction,
                @"INSERT OR IGNORE INTO Doctors (Id, FirstName, LastName, Phone, Address, Email, Specialization, Salary)
                  VALUES (@Id, @FirstName, @LastName, @Phone, @Address, @Email, @Specialization, @Salary);",
                cancellationToken,
                ("@Id", id),
                ("@FirstName", firstName),
                ("@LastName", lastName),
                ("@Phone", phone),
                ("@Address", address),
                ("@Email", email),
                ("@Specialization", specialization),
                ("@Salary", salary));
        }

        private static Task InsertPatientAsync(SqliteConnection connection, SqliteTransaction transaction, string id, string firstName, string lastName, string address, string phone, string email, int age, string treatmentStatus, string doctorId, CancellationToken cancellationToken)
        {
            return ExecuteInsertAsync(
                connection,
                transaction,
                @"INSERT OR IGNORE INTO Patients (Id, FirstName, LastName, Address, Phone, Email, Age, TreatmentStatus, DoctorId)
                  VALUES (@Id, @FirstName, @LastName, @Address, @Phone, @Email, @Age, @TreatmentStatus, @DoctorId);",
                cancellationToken,
                ("@Id", id),
                ("@FirstName", firstName),
                ("@LastName", lastName),
                ("@Address", address),
                ("@Phone", phone),
                ("@Email", email),
                ("@Age", age),
                ("@TreatmentStatus", treatmentStatus),
                ("@DoctorId", doctorId));
        }

        private static Task InsertTreatmentAsync(SqliteConnection connection, SqliteTransaction transaction, string number, string type, int price, string tools, CancellationToken cancellationToken)
        {
            return ExecuteInsertAsync(
                connection,
                transaction,
                @"INSERT OR IGNORE INTO Treatments (Number, Type, Price, Tools)
                  VALUES (@Number, @Type, @Price, @Tools);",
                cancellationToken,
                ("@Number", number),
                ("@Type", type),
                ("@Price", price),
                ("@Tools", tools));
        }

        private static Task InsertAppointmentAsync(SqliteConnection connection, SqliteTransaction transaction, string patientId, string doctorId, string date, string time, string treatmentNumber, CancellationToken cancellationToken)
        {
            return ExecuteInsertAsync(
                connection,
                transaction,
                @"INSERT OR IGNORE INTO Appointments (PatientId, DoctorId, Date, Time, TreatmentNumber)
                  VALUES (@PatientId, @DoctorId, @Date, @Time, @TreatmentNumber);",
                cancellationToken,
                ("@PatientId", patientId),
                ("@DoctorId", doctorId),
                ("@Date", date),
                ("@Time", time),
                ("@TreatmentNumber", treatmentNumber));
        }

        private static Task InsertPatientTreatmentAsync(SqliteConnection connection, SqliteTransaction transaction, string patientId, string treatmentNumber, string isCompleted, string isPaid, string startDate, CancellationToken cancellationToken)
        {
            return ExecuteInsertAsync(
                connection,
                transaction,
                @"INSERT OR IGNORE INTO PatientTreatments (PatientId, TreatmentNumber, IsCompleted, IsPaid, StartDate)
                  VALUES (@PatientId, @TreatmentNumber, @IsCompleted, @IsPaid, @StartDate);",
                cancellationToken,
                ("@PatientId", patientId),
                ("@TreatmentNumber", treatmentNumber),
                ("@IsCompleted", isCompleted),
                ("@IsPaid", isPaid),
                ("@StartDate", startDate));
        }

        private static async Task ExecuteInsertAsync(SqliteConnection connection, SqliteTransaction transaction, string sql, CancellationToken cancellationToken, params (string Name, object? Value)[] parameters)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.Transaction = transaction;
            foreach (var (name, value) in parameters)
            {
                command.Parameters.AddWithValue(name, value ?? DBNull.Value);
            }

            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
