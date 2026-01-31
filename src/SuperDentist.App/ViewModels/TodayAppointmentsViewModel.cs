using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SuperDentist.App.Services;
using SuperDentist.Core;
using SuperDentist.Core.Services;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Media;

namespace SuperDentist.App.ViewModels
{
    public sealed partial class TodayAppointmentsViewModel : ViewModelBase
    {
        private readonly IAppointmentService _appointmentService;
        private readonly IPrintService _printService;

        public TodayAppointmentsViewModel(IAppointmentService appointmentService, IPrintService printService)
        {
            _appointmentService = appointmentService;
            _printService = printService;

            Appointments = new ObservableCollection<Appointment>();
            LoadCommand = new AsyncRelayCommand(LoadAsync);
            PrintCommand = new RelayCommand<Visual>(Print);

            _ = LoadAsync();
        }

        public ObservableCollection<Appointment> Appointments { get; }

        public IAsyncRelayCommand LoadCommand { get; }
        public IRelayCommand<Visual> PrintCommand { get; }

        private async Task LoadAsync()
        {
            Appointments.Clear();
            string today = DateTime.Today.ToString("yyyy-MM-dd");
            foreach (var appointment in await _appointmentService.GetByDateAsync(today).ConfigureAwait(true))
            {
                Appointments.Add(appointment);
            }
        }

        private void Print(Visual? visual)
        {
            if (visual == null)
            {
                return;
            }

            _printService.PrintVisual(visual, "Today's appointments");
        }
    }
}
