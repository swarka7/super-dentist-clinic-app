using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SuperDentist.App.Services;
using SuperDentist.Core;
using SuperDentist.Core.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;

namespace SuperDentist.App.ViewModels
{
    public sealed partial class PatientsViewModel : ViewModelBase
    {
        private readonly IPatientService _patientService;
        private readonly IDoctorService _doctorService;
        private readonly IMessageService _messageService;
        private readonly ICollectionView _patientsView;

        public PatientsViewModel(IPatientService patientService, IDoctorService doctorService, IMessageService messageService)
        {
            _patientService = patientService;
            _doctorService = doctorService;
            _messageService = messageService;

            Patients = new ObservableCollection<Patient>();
            Doctors = new ObservableCollection<Doctor>();
            TreatmentStatuses = new ObservableCollection<string> { "Yes", "No" };

            _patientsView = CollectionViewSource.GetDefaultView(Patients);
            _patientsView.Filter = FilterPatients;

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

        public ObservableCollection<Patient> Patients { get; }
        public ObservableCollection<Doctor> Doctors { get; }
        public ObservableCollection<string> TreatmentStatuses { get; }

        [ObservableProperty]
        private Patient? selectedPatient;

        [Required(ErrorMessage = "Patient ID is required.")]
        [RegularExpression(@"^\d{9}$", ErrorMessage = "Patient ID must be 9 digits.")]
        [ObservableProperty]
        private string patientId = string.Empty;

        [Required(ErrorMessage = "Doctor ID is required.")]
        [ObservableProperty]
        private string doctorId = string.Empty;

        [Required(ErrorMessage = "First name is required.")]
        [MinLength(2, ErrorMessage = "First name must be at least 2 characters.")]
        [ObservableProperty]
        private string firstName = string.Empty;

        [Required(ErrorMessage = "Last name is required.")]
        [MinLength(2, ErrorMessage = "Last name must be at least 2 characters.")]
        [ObservableProperty]
        private string lastName = string.Empty;

        [ObservableProperty]
        private string address = string.Empty;

        [ObservableProperty]
        private string phone = string.Empty;

        [EmailAddress(ErrorMessage = "Email format is invalid.")]
        [ObservableProperty]
        private string email = string.Empty;

        [Required(ErrorMessage = "Age is required.")]
        [RegularExpression(@"^\d+$", ErrorMessage = "Age must be numeric.")]
        [ObservableProperty]
        private string ageText = string.Empty;

        [ObservableProperty]
        private string treatmentStatus = "No";

        [ObservableProperty]
        private string searchText = string.Empty;

        public IAsyncRelayCommand LoadCommand { get; }
        public IAsyncRelayCommand AddCommand { get; }
        public IAsyncRelayCommand UpdateCommand { get; }
        public IAsyncRelayCommand DeleteCommand { get; }
        public IRelayCommand ClearCommand { get; }

        partial void OnSelectedPatientChanged(Patient? value)
        {
            if (value == null)
            {
                UpdateCommand.NotifyCanExecuteChanged();
                DeleteCommand.NotifyCanExecuteChanged();
                return;
            }

            PatientId = value.Id;
            DoctorId = value.DoctorId;
            FirstName = value.FirstName;
            LastName = value.LastName;
            Address = value.Address;
            Phone = value.Phone;
            Email = value.Email;
            AgeText = value.Age.ToString();
            TreatmentStatus = string.IsNullOrWhiteSpace(value.TreatmentStatus) ? "No" : value.TreatmentStatus;
            UpdateCommand.NotifyCanExecuteChanged();
            DeleteCommand.NotifyCanExecuteChanged();
        }

        partial void OnSearchTextChanged(string value) => _patientsView.Refresh();

        private bool FilterPatients(object item)
        {
            if (item is not Patient patient)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(SearchText))
            {
                return true;
            }

            string query = SearchText.Trim().ToLowerInvariant();
            return patient.Id.Contains(query) ||
                   patient.FirstName.ToLowerInvariant().Contains(query) ||
                   patient.LastName.ToLowerInvariant().Contains(query) ||
                   patient.Email.ToLowerInvariant().Contains(query);
        }

        private async Task LoadAsync()
        {
            IsBusy = true;
            try
            {
                Patients.Clear();
                foreach (var patient in await _patientService.GetAllAsync().ConfigureAwait(true))
                {
                    Patients.Add(patient);
                }

                Doctors.Clear();
                foreach (var doctor in await _doctorService.GetAllAsync().ConfigureAwait(true))
                {
                    Doctors.Add(doctor);
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

            if (!TryParseInt(AgeText, 0, 120, out int ageValue))
            {
                _messageService.ShowError("Age must be a valid number between 0 and 120.");
                return;
            }

            var patient = new Patient
            {
                Id = PatientId,
                DoctorId = DoctorId,
                FirstName = FirstName,
                LastName = LastName,
                Address = Address,
                Phone = Phone,
                Email = Email,
                Age = ageValue,
                TreatmentStatus = TreatmentStatus
            };

            var result = await _patientService.AddAsync(patient).ConfigureAwait(true);
            if (!result.Success)
            {
                _messageService.ShowError(result.ErrorMessage ?? "Unable to add patient.");
                return;
            }

            Patients.Add(patient);
            ClearForm();
        }

        private async Task UpdateAsync()
        {
            if (SelectedPatient == null)
            {
                _messageService.ShowError("Select a patient to update.");
                return;
            }

            ValidateAllProperties();
            if (HasErrors)
            {
                _messageService.ShowError("Please fix validation errors before saving.");
                return;
            }

            if (!TryParseInt(AgeText, 0, 120, out int ageValue))
            {
                _messageService.ShowError("Age must be a valid number between 0 and 120.");
                return;
            }

            SelectedPatient.Id = PatientId;
            SelectedPatient.DoctorId = DoctorId;
            SelectedPatient.FirstName = FirstName;
            SelectedPatient.LastName = LastName;
            SelectedPatient.Address = Address;
            SelectedPatient.Phone = Phone;
            SelectedPatient.Email = Email;
            SelectedPatient.Age = ageValue;
            SelectedPatient.TreatmentStatus = TreatmentStatus;

            var result = await _patientService.UpdateAsync(SelectedPatient).ConfigureAwait(true);
            if (!result.Success)
            {
                _messageService.ShowError(result.ErrorMessage ?? "Unable to update patient.");
                return;
            }

            _patientsView.Refresh();
        }

        private async Task DeleteAsync()
        {
            if (SelectedPatient == null)
            {
                _messageService.ShowError("Select a patient to delete.");
                return;
            }

            if (!_messageService.ShowConfirmation($"Delete patient {SelectedPatient.FirstName} {SelectedPatient.LastName}?"))
            {
                return;
            }

            var result = await _patientService.DeleteAsync(SelectedPatient.Id).ConfigureAwait(true);
            if (!result.Success)
            {
                _messageService.ShowError(result.ErrorMessage ?? "Unable to delete patient.");
                return;
            }

            Patients.Remove(SelectedPatient);
            ClearForm();
        }

        private void ClearForm()
        {
            RunWithoutValidation(() =>
            {
                SelectedPatient = null;
                PatientId = string.Empty;
                DoctorId = string.Empty;
                FirstName = string.Empty;
                LastName = string.Empty;
                Address = string.Empty;
                Phone = string.Empty;
                Email = string.Empty;
                AgeText = string.Empty;
                TreatmentStatus = "No";
            });
            ResetValidation();
        }

        private bool CanSave() => !HasErrors
                                && !string.IsNullOrWhiteSpace(PatientId)
                                && !string.IsNullOrWhiteSpace(DoctorId)
                                && !string.IsNullOrWhiteSpace(FirstName)
                                && !string.IsNullOrWhiteSpace(LastName)
                                && !string.IsNullOrWhiteSpace(AgeText);
        private bool CanUpdate() => SelectedPatient != null && CanSave();
        private bool CanDelete() => SelectedPatient != null;
    }
}
