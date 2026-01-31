using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using SuperDentist.Core.Options;
using SuperDentist.Core.Repositories;
using SuperDentist.Core.Services;
using SuperDentist.Infrastructure.Data;
using SuperDentist.Infrastructure.Repositories;
using SuperDentist.Infrastructure.Services;
using SuperDentist.App.Services;
using SuperDentist.App.ViewModels;
using System;
using System.IO;
using System.Windows;

namespace SuperDentist.App
{
    public partial class App : Application
    {
        private IHost? _host;

        public static IServiceProvider Services => ((App)Current)._host!.Services;

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            _host = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration(builder =>
                {
                    builder.SetBasePath(AppContext.BaseDirectory)
                           .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                })
                .UseSerilog((context, services, configuration) =>
                {
                    string? logPath = context.Configuration["Logging:LogFilePath"];
                    if (string.IsNullOrWhiteSpace(logPath))
                    {
                        logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SuperDentist", "logs", "superdentist.log");
                    }

                    configuration
                        .MinimumLevel.Information()
                        .WriteTo.Debug()
                        .WriteTo.File(logPath, rollingInterval: RollingInterval.Day);
                })
                .ConfigureServices((context, services) =>
                {
                    services.Configure<DatabaseOptions>(context.Configuration.GetSection("Database"));

                    services.AddSingleton<ISqliteConnectionFactory, SqliteConnectionFactory>();
                    services.AddSingleton<IDatabaseInitializer, SqliteDatabaseInitializer>();

                    services.AddSingleton<IDoctorRepository, SqliteDoctorRepository>();
                    services.AddSingleton<IPatientRepository, SqlitePatientRepository>();
                    services.AddSingleton<ITreatmentRepository, SqliteTreatmentRepository>();
                    services.AddSingleton<IAppointmentRepository, SqliteAppointmentRepository>();
                    services.AddSingleton<IPatientTreatmentRepository, SqlitePatientTreatmentRepository>();

                    services.AddSingleton<IDoctorService, DoctorService>();
                    services.AddSingleton<IPatientService, PatientService>();
                    services.AddSingleton<ITreatmentService, TreatmentService>();
                    services.AddSingleton<IAppointmentService, AppointmentService>();
                    services.AddSingleton<IPatientTreatmentService, PatientTreatmentService>();

                    services.AddSingleton<IPrintService, PrintService>();
                    services.AddSingleton<IMessageService, MessageService>();

                    services.AddSingleton<INavigationService, NavigationService>();
                    services.AddSingleton<ShellViewModel>();

                    services.AddTransient<DoctorsViewModel>();
                    services.AddTransient<PatientsViewModel>();
                    services.AddTransient<TreatmentsViewModel>();
                    services.AddTransient<AppointmentsViewModel>();
                    services.AddTransient<PatientTreatmentsViewModel>();
                    services.AddTransient<ReportsViewModel>();
                    services.AddTransient<PatientReportViewModel>();
                    services.AddTransient<PatientDetailsViewModel>();
                    services.AddTransient<TreatmentReportViewModel>();
                    services.AddTransient<TodayAppointmentsViewModel>();

                    services.AddSingleton<MainWindow>();
                })
                .Build();

            DispatcherUnhandledException += (_, args) =>
            {
                Log.Error(args.Exception, "Unhandled UI exception");
                MessageBox.Show("An unexpected error occurred. Please check the logs for details.", "Unexpected Error", MessageBoxButton.OK, MessageBoxImage.Error);
                args.Handled = true;
            };

            try
            {
                var initializer = _host.Services.GetRequiredService<IDatabaseInitializer>();
                InitializationResult result = await initializer.InitializeAsync().ConfigureAwait(true);

                Log.Information("Database initialized. New={IsNew} Path={Path}", result.IsNewDatabase, result.DatabasePath);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Database initialization failed");
                MessageBox.Show("The database could not be initialized. Please check the logs for details.", "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(-1);
                return;
            }

            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            if (_host != null)
            {
                await _host.StopAsync(TimeSpan.FromSeconds(3)).ConfigureAwait(true);
                _host.Dispose();
            }

            Log.CloseAndFlush();
            base.OnExit(e);
        }
    }
}


