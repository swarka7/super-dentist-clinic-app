using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SuperDentist.App.Services;
using SuperDentist.Core;
using SuperDentist.Core.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using System.Windows.Data;

namespace SuperDentist.App.ViewModels
{
    public sealed partial class PatientTreatmentsViewModel : ViewModelBase
    {
        private readonly IPatientTreatmentService _patientTreatmentService;
        private readonly IPatientService _patientService;
        private readonly ITreatmentService _treatmentService;
        private readonly IMessageService _messageService;
        private readonly ICollectionView _patientTreatmentsView;

        public PatientTreatmentsViewModel(
            IPatientTreatmentService patientTreatmentService,
            IPatientService patientService,
            ITreatmentService treatmentService,
            IMessageService messageService)
        {
            _patientTreatmentService = patientTreatmentService;
            _patientService = patientService;
            _treatmentService = treatmentService;
            _messageService = messageService;

            PatientTreatments = new ObservableCollection<PatientTreatment>();
            Patients = new ObservableCollection<Patient>();
            Treatments = new ObservableCollection<Treatment>();
            YesNoOptions = new ObservableCollection<string> { "Yes", "No" };

            _patientTreatmentsView = CollectionViewSource.GetDefaultView(PatientTreatments);

            LoadCommand = new AsyncRelayCommand(LoadAsync);
            AddCommand = new AsyncRelayCommand(AddAsync, CanSave);
            UpdateCommand = new AsyncRelayCommand(UpdateAsync, CanUpdate);
            DeleteCommand = new AsyncRelayCommand(DeleteAsync, CanDelete);
            ClearCommand = new RelayCommand(ClearForm);

            ErrorsChanged += (_, __) =>
            {
                AddCommand.NotifyCanExecuteChanged();
                UpdateCommand.NotifyCanExecuteChanged();
            };

            PropertyChanged += (_, __) =>
            {
                AddCommand.NotifyCanExecuteChanged();
                UpdateCommand.NotifyCanExecuteChanged();
            };

            _ = LoadAsync();
        }

        public ObservableCollection<PatientTreatment> PatientTreatments { get; }
        public ObservableCollection<Patient> Patients { get; }
        public ObservableCollection<Treatment> Treatments { get; }
        public ObservableCollection<string> YesNoOptions { get; }

        [ObservableProperty]
        private PatientTreatment? selectedPatientTreatment;

        [Required(ErrorMessage = "Patient ID is required.")]
        [ObservableProperty]
        private string patientId = string.Empty;

        [Required(ErrorMessage = "Treatment number is required.")]
        [ObservableProperty]
        private string treatmentNumber = string.Empty;

        [ObservableProperty]
        private string isCompleted = "No";

        [ObservableProperty]
        private string isPaid = "No";

        [ObservableProperty]
        private string startDate = DateTime.Today.ToString("yyyy-MM-dd");

        public IAsyncRelayCommand LoadCommand { get; }
        public IAsyncRelayCommand AddCommand { get; }
        public IAsyncRelayCommand UpdateCommand { get; }
        public IAsyncRelayCommand DeleteCommand { get; }
        public IRelayCommand ClearCommand { get; }

        partial void OnSelectedPatientTreatmentChanged(PatientTreatment? value)
        {
            if (value == null)
            {
                UpdateCommand.NotifyCanExecuteChanged();
                DeleteCommand.NotifyCanExecuteChanged();
                return;
            }

            PatientId = value.PatientId;
            TreatmentNumber = value.TreatmentNumber;
            IsCompleted = value.IsCompleted;
            IsPaid = value.IsPaid;
            StartDate = value.StartDate;
            UpdateCommand.NotifyCanExecuteChanged();
            DeleteCommand.NotifyCanExecuteChanged();
        }

        private async Task LoadAsync()
        {
            IsBusy = true;
            try
            {
                PatientTreatments.Clear();
                foreach (var item in await _patientTreatmentService.GetAllAsync().ConfigureAwait(true))
                {
                    PatientTreatments.Add(item);
                }

                Patients.Clear();
                foreach (var patient in await _patientService.GetAllAsync().ConfigureAwait(true))
                {
                    Patients.Add(patient);
                }

                Treatments.Clear();
                foreach (var treatment in await _treatmentService.GetAllAsync().ConfigureAwait(true))
                {
                    Treatments.Add(treatment);
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task AddAsync()
        {
            ValidateAllProperties();
            if (HasErrors)
            {
                _messageService.ShowError("Please fix validation errors before saving.");
                return;
            }

            var treatment = new PatientTreatment
            {
                PatientId = PatientId,
                TreatmentNumber = TreatmentNumber,
                IsCompleted = IsCompleted,
                IsPaid = IsPaid,
                StartDate = StartDate
            };

            var result = await _patientTreatmentService.AddAsync(treatment).ConfigureAwait(true);
            if (!result.Success)
            {
                _messageService.ShowError(result.ErrorMessage ?? "Unable to add patient treatment.");
                return;
            }

            PatientTreatments.Add(treatment);
            ClearForm();
        }

        private async Task UpdateAsync()
        {
            if (SelectedPatientTreatment == null)
            {
                _messageService.ShowError("Select a patient treatment to update.");
                return;
            }

            ValidateAllProperties();
            if (HasErrors)
            {
                _messageService.ShowError("Please fix validation errors before saving.");
                return;
            }

            SelectedPatientTreatment.PatientId = PatientId;
            SelectedPatientTreatment.TreatmentNumber = TreatmentNumber;
            SelectedPatientTreatment.IsCompleted = IsCompleted;
            SelectedPatientTreatment.IsPaid = IsPaid;
            SelectedPatientTreatment.StartDate = StartDate;

            var result = await _patientTreatmentService.UpdateAsync(SelectedPatientTreatment).ConfigureAwait(true);
            if (!result.Success)
            {
                _messageService.ShowError(result.ErrorMessage ?? "Unable to update patient treatment.");
                return;
            }

            _patientTreatmentsView.Refresh();
        }

        private async Task DeleteAsync()
        {
            if (SelectedPatientTreatment == null)
            {
                _messageService.ShowError("Select a patient treatment to delete.");
                return;
            }

            if (!_messageService.ShowConfirmation("Delete this patient treatment record?"))
            {
                return;
            }

            var result = await _patientTreatmentService.DeleteAsync(SelectedPatientTreatment.PatientId).ConfigureAwait(true);
            if (!result.Success)
            {
                _messageService.ShowError(result.ErrorMessage ?? "Unable to delete patient treatment.");
                return;
            }

            PatientTreatments.Remove(SelectedPatientTreatment);
            ClearForm();
        }

        private void ClearForm()
        {
            RunWithoutValidation(() =>
            {
                SelectedPatientTreatment = null;
                PatientId = string.Empty;
                TreatmentNumber = string.Empty;
                IsCompleted = "No";
                IsPaid = "No";
                StartDate = DateTime.Today.ToString("yyyy-MM-dd");
            });
            ResetValidation();
        }

        private bool CanSave() => !HasErrors
                                && !string.IsNullOrWhiteSpace(PatientId)
                                && !string.IsNullOrWhiteSpace(TreatmentNumber);
        private bool CanUpdate() => SelectedPatientTreatment != null && CanSave();
        private bool CanDelete() => SelectedPatientTreatment != null;
    }
}
