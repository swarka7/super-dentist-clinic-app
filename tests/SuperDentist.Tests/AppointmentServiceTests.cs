using SuperDentist.Core;
using SuperDentist.Infrastructure.Repositories;
using SuperDentist.Application.Services;
using System.Threading.Tasks;
using Xunit;

namespace SuperDentist.Tests
{
    public sealed class AppointmentServiceTests
    {
        private const string SlotConflictMessage = "This time slot is already booked for the selected doctor.";
        private const string PatientConflictMessage = "This patient already has an appointment.";

        [Fact]
        public async Task AddAsync_WhenDoctorSlotAlreadyBooked_RejectsConflictingAppointment()
        {
            using var database = await SqliteTestDatabase.CreateAsync();
            var service = CreateService(database);

            await database.SeedAppointmentReferencesAsync(
                ("900000001", "800000001", "T001"),
                ("900000002", "800000001", "T002"));

            var appointment = CreateAppointment(
                patientId: "900000001",
                doctorId: "800000001",
                date: "2030-01-15",
                time: "09:00",
                treatmentNumber: "T001");

            var first = await service.AddAsync(appointment);
            Assert.True(first.Success, first.ErrorMessage);

            var second = await service.AddAsync(CreateAppointment(
                patientId: "900000002",
                doctorId: appointment.DoctorId,
                date: appointment.Date,
                time: appointment.Time,
                treatmentNumber: "T002"));

            Assert.False(second.Success);
            Assert.Equal(SlotConflictMessage, second.ErrorMessage);
        }

        [Fact]
        public async Task AddAsync_WhenPatientAlreadyHasAppointment_RejectsConflictingAppointment()
        {
            using var database = await SqliteTestDatabase.CreateAsync();
            var service = CreateService(database);

            await database.SeedAppointmentReferencesAsync(
                ("900000101", "800000101", "T101"),
                ("900000102", "800000102", "T102"));

            var appointment = CreateAppointment(
                patientId: "900000101",
                doctorId: "800000101",
                date: "2030-02-01",
                time: "10:00",
                treatmentNumber: "T101");

            var first = await service.AddAsync(appointment);
            Assert.True(first.Success, first.ErrorMessage);

            var second = await service.AddAsync(CreateAppointment(
                patientId: appointment.PatientId,
                doctorId: "800000102",
                date: "2030-02-02",
                time: "11:00",
                treatmentNumber: "T102"));

            Assert.False(second.Success);
            Assert.Equal(PatientConflictMessage, second.ErrorMessage);
        }

        [Fact]
        public async Task AddAsync_WhenSameTimeDifferentDoctor_AllowsAppointment()
        {
            using var database = await SqliteTestDatabase.CreateAsync();
            var service = CreateService(database);

            await database.SeedAppointmentReferencesAsync(
                ("900000201", "800000201", "T201"),
                ("900000202", "800000202", "T202"));

            var first = await service.AddAsync(CreateAppointment(
                patientId: "900000201",
                doctorId: "800000201",
                date: "2030-03-01",
                time: "12:00",
                treatmentNumber: "T201"));

            Assert.True(first.Success, first.ErrorMessage);

            var second = await service.AddAsync(CreateAppointment(
                patientId: "900000202",
                doctorId: "800000202",
                date: "2030-03-01",
                time: "12:00",
                treatmentNumber: "T202"));

            Assert.True(second.Success, second.ErrorMessage);
        }

        [Fact]
        public async Task AddAsync_WhenSameDoctorDifferentTime_AllowsAppointment()
        {
            using var database = await SqliteTestDatabase.CreateAsync();
            var service = CreateService(database);

            await database.SeedAppointmentReferencesAsync(
                ("900000301", "800000301", "T301"),
                ("900000302", "800000301", "T302"));

            var first = await service.AddAsync(CreateAppointment(
                patientId: "900000301",
                doctorId: "800000301",
                date: "2030-04-01",
                time: "13:00",
                treatmentNumber: "T301"));

            Assert.True(first.Success, first.ErrorMessage);

            var second = await service.AddAsync(CreateAppointment(
                patientId: "900000302",
                doctorId: "800000301",
                date: "2030-04-01",
                time: "13:30",
                treatmentNumber: "T302"));

            Assert.True(second.Success, second.ErrorMessage);
        }

        private static AppointmentService CreateService(SqliteTestDatabase database)
        {
            return new AppointmentService(new SqliteAppointmentRepository(database.ConnectionFactory));
        }

        private static Appointment CreateAppointment(
            string patientId,
            string doctorId,
            string date,
            string time,
            string treatmentNumber)
        {
            return new Appointment
            {
                PatientId = patientId,
                DoctorId = doctorId,
                Date = date,
                Time = time,
                TreatmentNumber = treatmentNumber
            };
        }
    }
}