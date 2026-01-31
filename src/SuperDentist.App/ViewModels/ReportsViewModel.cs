using CommunityToolkit.Mvvm.Input;
using SuperDentist.App.Services;
using System.Windows.Input;

namespace SuperDentist.App.ViewModels
{
    public sealed class ReportsViewModel : ViewModelBase
    {
        private readonly INavigationService _navigationService;

        public ReportsViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService;

            ShowTreatmentReportCommand = new RelayCommand(() => _navigationService.NavigateTo<TreatmentReportViewModel>());
            ShowPatientsByDoctorCommand = new RelayCommand(() => _navigationService.NavigateTo<PatientDetailsViewModel>());
            ShowTodayAppointmentsCommand = new RelayCommand(() => _navigationService.NavigateTo<TodayAppointmentsViewModel>());
            ShowPatientsInTreatmentCommand = new RelayCommand(() => _navigationService.NavigateTo<PatientReportViewModel>());
        }

        public ICommand ShowTreatmentReportCommand { get; }
        public ICommand ShowPatientsByDoctorCommand { get; }
        public ICommand ShowTodayAppointmentsCommand { get; }
        public ICommand ShowPatientsInTreatmentCommand { get; }
    }
}
