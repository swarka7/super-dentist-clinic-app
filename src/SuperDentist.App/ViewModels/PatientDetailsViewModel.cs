using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SuperDentist.App.Services;
using SuperDentist.Core;
using SuperDentist.Core.Services;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;

namespace SuperDentist.App.ViewModels
{
    public sealed partial class PatientDetailsViewModel : ViewModelBase
    {
        private readonly IPatientService _patientService;
        private readonly IDoctorService _doctorService;
        private readonly IPrintService _printService;

        public PatientDetailsViewModel(IPatientService patientService, IDoctorService doctorService, IPrintService printService)
        {
            _patientService = patientService;
            _doctorService = doctorService;
            _printService = printService;

            Doctors = new ObservableCollection<Doctor>();
            Patients = new ObservableCollection<Patient>();

            LoadCommand = new AsyncRelayCommand(LoadAsync);
            FilterCommand = new AsyncRelayCommand(FilterAsync);
            PrintCommand = new RelayCommand<Visual>(Print);

            _ = LoadAsync();
        }

        public ObservableCollection<Doctor> Doctors { get; }
        public ObservableCollection<Patient> Patients { get; }

        [ObservableProperty]
        private string selectedDoctorId = string.Empty;

        public IAsyncRelayCommand LoadCommand { get; }
        public IAsyncRelayCommand FilterCommand { get; }
        public IRelayCommand<Visual> PrintCommand { get; }

        private async Task LoadAsync()
        {
            Doctors.Clear();
            foreach (var doctor in await _doctorService.GetAllAsync().ConfigureAwait(true))
            {
                Doctors.Add(doctor);
            }

            await FilterAsync().ConfigureAwait(true);
        }

        private async Task FilterAsync()
        {
            Patients.Clear();
            var patients = await _patientService.GetAllAsync().ConfigureAwait(true);
            if (string.IsNullOrWhiteSpace(SelectedDoctorId))
            {
                foreach (var patient in patients)
                {
                    Patients.Add(patient);
                }
                return;
            }

            foreach (var patient in patients.Where(p => p.DoctorId == SelectedDoctorId))
            {
                Patients.Add(patient);
            }
        }

        private void Print(Visual? visual)
        {
            if (visual == null)
            {
                return;
            }

            _printService.PrintVisual(visual, "Patients by doctor");
        }
    }
}
