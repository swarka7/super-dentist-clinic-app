using Microsoft.Extensions.DependencyInjection;
using SuperDentist.Application.Services;
using SuperDentist.Core.Services;

namespace SuperDentist.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddSuperDentistApplication(this IServiceCollection services)
        {
            services.AddSingleton(TimeProvider.System);
            services.AddSingleton<ICurrentActorProvider, LocalCurrentActorProvider>();
            services.AddSingleton<IAuditService, AuditService>();
            services.AddSingleton<IDoctorService, DoctorService>();
            services.AddSingleton<IPatientService, PatientService>();
            services.AddSingleton<ITreatmentService, TreatmentService>();
            services.AddSingleton<IAppointmentService, AppointmentService>();
            services.AddSingleton<IPatientTreatmentService, PatientTreatmentService>();
            services.AddSingleton<IClinicQueryService, ClinicQueryService>();
            services.AddSingleton<IDashboardQueryService, DashboardQueryService>();

            return services;
        }
    }
}
