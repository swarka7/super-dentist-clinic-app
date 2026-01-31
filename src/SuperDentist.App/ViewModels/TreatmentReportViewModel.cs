using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SuperDentist.App.Services;
using SuperDentist.Core;
using SuperDentist.Core.Services;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Media;

namespace SuperDentist.App.ViewModels
{
    public sealed class TreatmentReportViewModel : ViewModelBase
    {
        private readonly ITreatmentService _treatmentService;
        private readonly IPrintService _printService;

        public TreatmentReportViewModel(ITreatmentService treatmentService, IPrintService printService)
        {
            _treatmentService = treatmentService;
            _printService = printService;

            Treatments = new ObservableCollection<Treatment>();
            LoadCommand = new AsyncRelayCommand(LoadAsync);
            PrintCommand = new RelayCommand<Visual>(Print);

            _ = LoadAsync();
        }

        public ObservableCollection<Treatment> Treatments { get; }

        public IAsyncRelayCommand LoadCommand { get; }
        public IRelayCommand<Visual> PrintCommand { get; }

        private async Task LoadAsync()
        {
            Treatments.Clear();
            foreach (var treatment in await _treatmentService.GetAllAsync().ConfigureAwait(true))
            {
                Treatments.Add(treatment);
            }
        }

        private void Print(Visual? visual)
        {
            if (visual == null)
            {
                return;
            }

            _printService.PrintVisual(visual, "Treatment report");
        }
    }
}
