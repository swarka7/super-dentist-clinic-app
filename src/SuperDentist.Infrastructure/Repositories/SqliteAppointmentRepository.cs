using Microsoft.Data.Sqlite;
using SuperDentist.Core;
using SuperDentist.Core.Repositories;
using SuperDentist.Infrastructure.Data;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SuperDentist.Infrastructure.Repositories
{
    public sealed class SqliteAppointmentRepository : SqliteRepositoryBase, IAppointmentRepository
    {
        public SqliteAppointmentRepository(ISqliteConnectionFactory connectionFactory) : base(connectionFactory)
        {
        }

        public async Task<IReadOnlyList<Appointment>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var appointments = new List<Appointment>();
            await using var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            await using var command = connection.CreateCommand();
            command.CommandText = @"SELECT PatientId, DoctorId, Date, Time, TreatmentNumber, CreatedAtUtc, UpdatedAtUtc
                                    FROM Appointments ORDER BY Date, Time;";

            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                appointments.Add(MapAppointment(reader));
            }

            return appointments;
        }

        public async Task<Appointment?> GetByPatientIdAsync(string patientId, CancellationToken cancellationToken = default)
        {
            await using var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            await using var command = connection.CreateCommand();
            command.CommandText = @"SELECT PatientId, DoctorId, Date, Time, TreatmentNumber, CreatedAtUtc, UpdatedAtUtc
                                    FROM Appointments WHERE PatientId = @PatientId;";
            command.Parameters.AddWithValue("@PatientId", patientId);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            return await reader.ReadAsync(cancellationToken).ConfigureAwait(false) ? MapAppointment(reader) : null;
        }

        public async Task<IReadOnlyList<Appointment>> GetByDateAsync(string date, CancellationToken cancellationToken = default)
        {
            var appointments = new List<Appointment>();
            await using var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            await using var command = connection.CreateCommand();
            command.CommandText = @"SELECT PatientId, DoctorId, Date, Time, TreatmentNumber, CreatedAtUtc, UpdatedAtUtc
                                    FROM Appointments WHERE Date = @Date ORDER BY Time;";
            command.Parameters.AddWithValue("@Date", date);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                appointments.Add(MapAppointment(reader));
            }

            return appointments;
        }

        public async Task<bool> ExistsAsync(string patientId, CancellationToken cancellationToken = default)
        {
            await using var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1 FROM Appointments WHERE PatientId = @PatientId LIMIT 1;";
            command.Parameters.AddWithValue("@PatientId", patientId);
            object? result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            return result != null;
        }

        public async Task<bool> SlotExistsAsync(string doctorId, string date, string time, CancellationToken cancellationToken = default)
        {
            await using var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            await using var command = connection.CreateCommand();
            command.CommandText = @"SELECT 1 FROM Appointments
                                    WHERE DoctorId = @DoctorId AND Date = @Date AND Time = @Time
                                    LIMIT 1;";
            command.Parameters.AddWithValue("@DoctorId", doctorId);
            command.Parameters.AddWithValue("@Date", date);
            command.Parameters.AddWithValue("@Time", time);
            object? result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            return result != null;
        }

        public async Task AddAsync(Appointment appointment, CancellationToken cancellationToken = default)
        {
            await using var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            await using var command = connection.CreateCommand();
            command.CommandText = @"INSERT INTO Appointments (PatientId, DoctorId, Date, Time, TreatmentNumber, CreatedAtUtc, UpdatedAtUtc)
                                    VALUES (@PatientId, @DoctorId, @Date, @Time, @TreatmentNumber, @CreatedAtUtc, @UpdatedAtUtc);";
            AddAppointmentParameters(command, appointment, includeCreatedAt: true);
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task UpdateAsync(Appointment appointment, CancellationToken cancellationToken = default)
        {
            await using var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            await using var command = connection.CreateCommand();
            command.CommandText = @"UPDATE Appointments
                                    SET DoctorId = @DoctorId,
                                        Date = @Date,
                                        Time = @Time,
                                        TreatmentNumber = @TreatmentNumber,
                                        UpdatedAtUtc = @UpdatedAtUtc
                                    WHERE PatientId = @PatientId;";
            AddAppointmentParameters(command, appointment, includeCreatedAt: false);
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task DeleteAsync(string patientId, CancellationToken cancellationToken = default)
        {
            await using var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            await using var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM Appointments WHERE PatientId = @PatientId;";
            command.Parameters.AddWithValue("@PatientId", patientId);
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        private static Appointment MapAppointment(SqliteDataReader reader)
        {
            return new Appointment
            {
                PatientId = reader.GetString(0),
                DoctorId = reader.GetString(1),
                Date = reader.GetString(2),
                Time = reader.GetString(3),
                TreatmentNumber = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                CreatedAtUtc = ReadUtcDateTime(reader, 5),
                UpdatedAtUtc = ReadUtcDateTime(reader, 6)
            };
        }

        private static void AddAppointmentParameters(SqliteCommand command, Appointment appointment, bool includeCreatedAt)
        {
            string now = UtcNowText();
            command.Parameters.AddWithValue("@PatientId", appointment.PatientId);
            command.Parameters.AddWithValue("@DoctorId", appointment.DoctorId);
            command.Parameters.AddWithValue("@Date", appointment.Date);
            command.Parameters.AddWithValue("@Time", appointment.Time);
            command.Parameters.AddWithValue("@TreatmentNumber", DbNullableText(appointment.TreatmentNumber));
            if (includeCreatedAt)
            {
                command.Parameters.AddWithValue("@CreatedAtUtc", now);
            }

            command.Parameters.AddWithValue("@UpdatedAtUtc", now);
        }
    }
}