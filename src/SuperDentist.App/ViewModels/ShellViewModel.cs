using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SuperDentist.App.Services;
using System.Windows.Input;

namespace SuperDentist.App.ViewModels
{
    public sealed class ShellViewModel : ObservableObject
    {
        private readonly INavigationService _navigationService;

        public ShellViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService;

            if (_navigationService is ObservableObject observable)
            {
                observable.PropertyChanged += (_, args) =>
                {
                    if (args.PropertyName == nameof(INavigationService.CurrentViewModel))
                    {
                        OnPropertyChanged(nameof(CurrentViewModel));
                        OnPropertyChanged(nameof(IsDoctorsActive));
                        OnPropertyChanged(nameof(IsPatientsActive));
                        OnPropertyChanged(nameof(IsAppointmentsActive));
                        OnPropertyChanged(nameof(IsTreatmentsActive));
                        OnPropertyChanged(nameof(IsPatientTreatmentsActive));
                        OnPropertyChanged(nameof(IsTodayActive));
                        OnPropertyChanged(nameof(IsAuditHistoryActive));
                    }
                };
            }

            ShowDoctorsCommand = new RelayCommand(() => _navigationService.NavigateTo<DoctorsViewModel>());
            ShowPatientsCommand = new RelayCommand(() => _navigationService.NavigateTo<PatientsViewModel>());
            ShowTreatmentsCommand = new RelayCommand(() => _navigationService.NavigateTo<TreatmentsViewModel>());
            ShowAppointmentsCommand = new RelayCommand(() => _navigationService.NavigateTo<AppointmentsViewModel>());
            ShowPatientTreatmentsCommand = new RelayCommand(() => _navigationService.NavigateTo<PatientTreatmentsViewModel>());
            ShowReportsCommand = new RelayCommand(() => _navigationService.NavigateTo<ReportsViewModel>());
            ShowTodayAppointmentsCommand = new RelayCommand(() => _navigationService.NavigateTo<TodayAppointmentsViewModel>());
            ShowAuditHistoryCommand = new RelayCommand(() => _navigationService.NavigateTo<AuditHistoryViewModel>());

            _navigationService.NavigateTo<DoctorsViewModel>();
        }

        public ViewModelBase? CurrentViewModel => _navigationService.CurrentViewModel;
        public bool IsDoctorsActive => CurrentViewModel is DoctorsViewModel;
        public bool IsPatientsActive => CurrentViewModel is PatientsViewModel;
        public bool IsAppointmentsActive => CurrentViewModel is AppointmentsViewModel;
        public bool IsTreatmentsActive => CurrentViewModel is TreatmentsViewModel;
        public bool IsPatientTreatmentsActive => CurrentViewModel is PatientTreatmentsViewModel;
        public bool IsTodayActive => CurrentViewModel is TodayAppointmentsViewModel;
        public bool IsAuditHistoryActive => CurrentViewModel is AuditHistoryViewModel;

        public ICommand ShowDoctorsCommand { get; }
        public ICommand ShowPatientsCommand { get; }
        public ICommand ShowTreatmentsCommand { get; }
        public ICommand ShowAppointmentsCommand { get; }
        public ICommand ShowPatientTreatmentsCommand { get; }
        public ICommand ShowReportsCommand { get; }
        public ICommand ShowTodayAppointmentsCommand { get; }
        public ICommand ShowAuditHistoryCommand { get; }
    }
}