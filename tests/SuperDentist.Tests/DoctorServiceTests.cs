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
    public sealed class DoctorServiceTests
    {
        [Fact]
        public async Task AddAndRetrieveDoctor()
        {
            string databasePath = Path.Combine(Path.GetTempPath(), $"superdentist-test-{Guid.NewGuid():N}.db");
            var options = Options.Create(new DatabaseOptions { Path = databasePath });
            var connectionFactory = new SqliteConnectionFactory(options);
            var initializer = new SqliteDatabaseInitializer(connectionFactory, NullLogger<SqliteDatabaseInitializer>.Instance);
            await initializer.InitializeAsync();

            var repository = new SqliteDoctorRepository(connectionFactory);
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
            Assert.True(result.Success);

            var fetched = await service.GetByIdAsync("999000001");
            Assert.NotNull(fetched);
            Assert.Equal("Test", fetched!.FirstName);
        }
    }
}
