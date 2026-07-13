using Microsoft.Extensions.DependencyInjection;
using SuperDentist.Core.Repositories;
using SuperDentist.Core.Services;
using SuperDentist.Infrastructure.Data;
using SuperDentist.Infrastructure.Repositories;

namespace SuperDentist.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddSuperDentistInfrastructure(this IServiceCollection services)
        {
            services.AddSingleton<ISqliteConnectionFactory, SqliteConnectionFactory>();
            services.AddSingleton<SqliteDatabaseMigrator>();
            services.AddSingleton<IDatabaseInitializer, SqliteDatabaseInitializer>();

            services.AddSingleton<IDoctorRepository, SqliteDoctorRepository>();
            services.AddSingleton<IPatientRepository, SqlitePatientRepository>();
            services.AddSingleton<ITreatmentRepository, SqliteTreatmentRepository>();
            services.AddSingleton<IAppointmentRepository, SqliteAppointmentRepository>();
            services.AddSingleton<IPatientTreatmentRepository, SqlitePatientTreatmentRepository>();

            return services;
        }
    }
}
