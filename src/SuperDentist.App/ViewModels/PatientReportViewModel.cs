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
    public sealed partial class PatientReportViewModel : ViewModelBase
    {
        private readonly IPatientService _patientService;
        private readonly IPrintService _printService;

        public PatientReportViewModel(IPatientService patientService, IPrintService printService)
        {
            _patientService = patientService;
            _printService = printService;

            PatientsInTreatment = new ObservableCollection<Patient>();
            LoadCommand = new AsyncRelayCommand(LoadAsync);
            PrintCommand = new RelayCommand<Visual>(Print);

            _ = LoadAsync();
        }

        public ObservableCollection<Patient> PatientsInTreatment { get; }

        public IAsyncRelayCommand LoadCommand { get; }
        public IRelayCommand<Visual> PrintCommand { get; }

        private async Task LoadAsync()
        {
            PatientsInTreatment.Clear();
            var patients = await _patientService.GetAllAsync().ConfigureAwait(true);
            foreach (var patient in patients.Where(p => p.TreatmentStatus == "Yes"))
            {
                PatientsInTreatment.Add(patient);
            }
        }

        private void Print(Visual? visual)
        {
            if (visual == null)
            {
                return;
            }

            _printService.PrintVisual(visual, "Patients in treatment");
        }
    }
}
