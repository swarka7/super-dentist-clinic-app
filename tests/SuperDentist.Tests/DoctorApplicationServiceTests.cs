using SuperDentist.Application.Services;
using SuperDentist.Core;
using SuperDentist.Core.Repositories;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace SuperDentist.Tests
{
    public sealed class DoctorApplicationServiceTests
    {
        [Fact]
        public async Task AddAsync_WhenDoctorAlreadyExists_ReturnsDuplicateMessageWithoutSqlite()
        {
            var repository = new FakeDoctorRepository(new Doctor { Id = "999000001" });
            var service = new DoctorService(repository);

            var result = await service.AddAsync(new Doctor { Id = "999000001" });

            Assert.False(result.Success);
            Assert.Equal("A doctor with this ID already exists.", result.ErrorMessage);
            Assert.Empty(repository.AddedDoctors);
        }

        private sealed class FakeDoctorRepository : IDoctorRepository
        {
            private readonly List<Doctor> _doctors;

            public FakeDoctorRepository(params Doctor[] doctors)
            {
                _doctors = doctors.ToList();
            }

            public List<Doctor> AddedDoctors { get; } = new();

            public Task<IReadOnlyList<Doctor>> GetAllAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult<IReadOnlyList<Doctor>>(_doctors);
            }

            public Task<Doctor?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(_doctors.FirstOrDefault(doctor => doctor.Id == id));
            }

            public Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(_doctors.Any(doctor => doctor.Id == id));
            }

            public Task AddAsync(Doctor doctor, CancellationToken cancellationToken = default)
            {
                AddedDoctors.Add(doctor);
                _doctors.Add(doctor);
                return Task.CompletedTask;
            }

            public Task UpdateAsync(Doctor doctor, CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }

            public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
            {
                _doctors.RemoveAll(doctor => doctor.Id == id);
                return Task.CompletedTask;
            }
        }
    }
}
