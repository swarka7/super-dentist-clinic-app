using Microsoft.Data.Sqlite;
using SuperDentist.Core;
using SuperDentist.Core.Repositories;
using SuperDentist.Infrastructure.Data;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SuperDentist.Infrastructure.Repositories
{
    public sealed class SqliteDoctorRepository : SqliteRepositoryBase, IDoctorRepository
    {
        public SqliteDoctorRepository(SqliteConnectionFactory connectionFactory) : base(connectionFactory)
        {
        }

        public async Task<IReadOnlyList<Doctor>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var doctors = new List<Doctor>();
            await using var scope = await OpenScopeAsync(cancellationToken).ConfigureAwait(false);
            await using var command = scope.CreateCommand();
            command.CommandText = @"SELECT Id, FirstName, LastName, Phone, Address, Email, Specialization, Salary, CreatedAtUtc, UpdatedAtUtc
                                    FROM Doctors ORDER BY LastName, FirstName;";

            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                doctors.Add(MapDoctor(reader));
            }

            return doctors;
        }

        public async Task<Doctor?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            await using var scope = await OpenScopeAsync(cancellationToken).ConfigureAwait(false);
            await using var command = scope.CreateCommand();
            command.CommandText = @"SELECT Id, FirstName, LastName, Phone, Address, Email, Specialization, Salary, CreatedAtUtc, UpdatedAtUtc
                                    FROM Doctors WHERE Id = @Id;";
            command.Parameters.AddWithValue("@Id", id);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            return await reader.ReadAsync(cancellationToken).ConfigureAwait(false) ? MapDoctor(reader) : null;
        }

        public async Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default)
        {
            await using var scope = await OpenScopeAsync(cancellationToken).ConfigureAwait(false);
            await using var command = scope.CreateCommand();
            command.CommandText = "SELECT 1 FROM Doctors WHERE Id = @Id LIMIT 1;";
            command.Parameters.AddWithValue("@Id", id);
            object? result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            return result != null;
        }

        public async Task AddAsync(Doctor doctor, CancellationToken cancellationToken = default)
        {
            await using var scope = await OpenScopeAsync(cancellationToken).ConfigureAwait(false);
            await using var command = scope.CreateCommand();
            command.CommandText = @"INSERT INTO Doctors (Id, FirstName, LastName, Phone, Address, Email, Specialization, Salary, CreatedAtUtc, UpdatedAtUtc)
                                    VALUES (@Id, @FirstName, @LastName, @Phone, @Address, @Email, @Specialization, @Salary, @CreatedAtUtc, @UpdatedAtUtc);";
            AddDoctorParameters(command, doctor, includeCreatedAt: true);
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task UpdateAsync(Doctor doctor, CancellationToken cancellationToken = default)
        {
            await using var scope = await OpenScopeAsync(cancellationToken).ConfigureAwait(false);
            await using var command = scope.CreateCommand();
            command.CommandText = @"UPDATE Doctors
                                    SET FirstName = @FirstName,
                                        LastName = @LastName,
                                        Phone = @Phone,
                                        Address = @Address,
                                        Email = @Email,
                                        Specialization = @Specialization,
                                        Salary = @Salary,
                                        UpdatedAtUtc = @UpdatedAtUtc
                                    WHERE Id = @Id;";
            AddDoctorParameters(command, doctor, includeCreatedAt: false);
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            await using var scope = await OpenScopeAsync(cancellationToken).ConfigureAwait(false);
            await using var command = scope.CreateCommand();
            command.CommandText = "DELETE FROM Doctors WHERE Id = @Id;";
            command.Parameters.AddWithValue("@Id", id);
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        private static Doctor MapDoctor(SqliteDataReader reader)
        {
            return new Doctor
            {
                Id = reader.GetString(0),
                FirstName = reader.GetString(1),
                LastName = reader.GetString(2),
                Phone = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                Address = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                Email = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                Specialization = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                Salary = reader.IsDBNull(7) ? 0 : reader.GetInt32(7),
                CreatedAtUtc = ReadUtcDateTime(reader, 8),
                UpdatedAtUtc = ReadUtcDateTime(reader, 9)
            };
        }

        private static void AddDoctorParameters(SqliteCommand command, Doctor doctor, bool includeCreatedAt)
        {
            string now = UtcNowText();
            command.Parameters.AddWithValue("@Id", doctor.Id);
            command.Parameters.AddWithValue("@FirstName", doctor.FirstName);
            command.Parameters.AddWithValue("@LastName", doctor.LastName);
            command.Parameters.AddWithValue("@Phone", doctor.Phone ?? string.Empty);
            command.Parameters.AddWithValue("@Address", doctor.Address ?? string.Empty);
            command.Parameters.AddWithValue("@Email", doctor.Email ?? string.Empty);
            command.Parameters.AddWithValue("@Specialization", doctor.Specialization ?? string.Empty);
            command.Parameters.AddWithValue("@Salary", doctor.Salary);
            if (includeCreatedAt)
            {
                command.Parameters.AddWithValue("@CreatedAtUtc", now);
            }

            command.Parameters.AddWithValue("@UpdatedAtUtc", now);
        }
    }
}