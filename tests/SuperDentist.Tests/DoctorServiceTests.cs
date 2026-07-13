using SuperDentist.Core;
using SuperDentist.Infrastructure.Repositories;
using SuperDentist.Application.Services;
using System.Threading.Tasks;
using Xunit;

namespace SuperDentist.Tests
{
    public sealed class DoctorServiceTests
    {
        [Fact]
        public async Task AddAndRetrieveDoctor()
        {
            using var database = await SqliteTestDatabase.CreateAsync();
            var repository = new SqliteDoctorRepository(database.ConnectionFactory);
            var service = new DoctorService(repository);

            var doctor = new Doctor
            {
                Id = "999000001",
                FirstName = "Test",
                LastName = "Doctor",
                Email = "test@example.com",
                Phone = "0500000000",
                Address = "1 Test St",
                Specialization = "General",
                Salary = 8000
            };

            var result = await service.AddAsync(doctor);
            Assert.True(result.Success, result.ErrorMessage);

            var fetched = await service.GetByIdAsync("999000001");
            Assert.NotNull(fetched);
            Assert.Equal("Test", fetched!.FirstName);
        }
    }
}
