using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SuperDentist.Core;
using SuperDentist.Core.Options;
using SuperDentist.Infrastructure.Data;
using SuperDentist.Infrastructure.Repositories;
using SuperDentist.Infrastructure.Services;
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace SuperDentist.Tests
{
    public sealed class AppointmentServiceTests
    {
        [Fact]
        public async Task PreventsDoubleBookingForDoctor()
        {
            string databasePath = Path.Combine(Path.GetTempPath(), $"superdentist-test-{Guid.NewGuid():N}.db");
            var options = Options.Create(new DatabaseOptions { Path = databasePath });
            var connectionFactory = new SqliteConnectionFactory(options);
            var initializer = new SqliteDatabaseInitializer(connectionFactory, NullLogger<SqliteDatabaseInitializer>.Instance);
            await initializer.InitializeAsync();

            var repository = new SqliteAppointmentRepository(connectionFactory);
            var service = new AppointmentService(repository);

            var appointment = new Appointment
            {
                PatientId = "200000010",
                DoctorId = "100000001",
                Date = DateTime.Today.ToString("yyyy-MM-dd"),
                Time = "10:00",
                TreatmentNumber = "T001"
            };

            var first = await service.AddAsync(appointment);
            Assert.True(first.Success);

            var second = await service.AddAsync(new Appointment
            {
                PatientId = "200000011",
                DoctorId = "100000001",
                Date = appointment.Date,
                Time = appointment.Time,
                TreatmentNumber = "T002"
            });

            Assert.False(second.Success);
        }
    }
}
