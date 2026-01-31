using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SuperDentist.App.Services;
using SuperDentist.Core;
using SuperDentist.Core.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;

namespace SuperDentist.App.ViewModels
{
    public sealed partial class DoctorsViewModel : ViewModelBase
    {
        private readonly IDoctorService _doctorService;
        private readonly IMessageService _messageService;
        private readonly ICollectionView _doctorsView;

        public DoctorsViewModel(IDoctorService doctorService, IMessageService messageService)
        {
            _doctorService = doctorService;
            _messageService = messageService;

            Doctors = new ObservableCollection<Doctor>();
            _doctorsView = CollectionViewSource.GetDefaultView(Doctors);
            _doctorsView.Filter = FilterDoctors;

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

        public ObservableCollection<Doctor> Doctors { get; }

        [ObservableProperty]
        private Doctor? selectedDoctor;

        [Required(ErrorMessage = "Doctor ID is required.")]
        [RegularExpression(@"^\d{9}$", ErrorMessage = "Doctor ID must be 9 digits.")]
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
        private string phone = string.Empty;

        [ObservableProperty]
        private string address = string.Empty;

        [EmailAddress(ErrorMessage = "Email format is invalid.")]
        [ObservableProperty]
        private string email = string.Empty;

        [ObservableProperty]
        private string specialization = string.Empty;

        [Required(ErrorMessage = "Salary is required.")]
        [RegularExpression(@"^\d+$", ErrorMessage = "Salary must be numeric.")]
        [ObservableProperty]
        private string salaryText = string.Empty;

        [ObservableProperty]
        private string searchText = string.Empty;

        public IAsyncRelayCommand LoadCommand { get; }
        public IAsyncRelayCommand AddCommand { get; }
        public IAsyncRelayCommand UpdateCommand { get; }
        public IAsyncRelayCommand DeleteCommand { get; }
        public IRelayCommand ClearCommand { get; }

        partial void OnSelectedDoctorChanged(Doctor? value)
        {
            if (value == null)
            {
                UpdateCommand.NotifyCanExecuteChanged();
                DeleteCommand.NotifyCanExecuteChanged();
                return;
            }

            DoctorId = value.Id;
            FirstName = value.FirstName;
            LastName = value.LastName;
            Phone = value.Phone;
            Address = value.Address;
            Email = value.Email;
            Specialization = value.Specialization;
            SalaryText = value.Salary.ToString(CultureInfo.InvariantCulture);
            UpdateCommand.NotifyCanExecuteChanged();
            DeleteCommand.NotifyCanExecuteChanged();
        }

        partial void OnSearchTextChanged(string value) => _doctorsView.Refresh();

        private bool FilterDoctors(object item)
        {
            if (item is not Doctor doctor)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(SearchText))
            {
                return true;
            }

            string query = SearchText.Trim().ToLowerInvariant();
            return doctor.Id.Contains(query) ||
                   doctor.FirstName.ToLowerInvariant().Contains(query) ||
                   doctor.LastName.ToLowerInvariant().Contains(query) ||
                   doctor.Email.ToLowerInvariant().Contains(query);
        }

        private async Task LoadAsync()
        {
            IsBusy = true;
            try
            {
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

            if (!TryParseInt(SalaryText, 0, 100000, out int salaryValue))
            {
                _messageService.ShowError("Salary must be a valid number between 0 and 100000.");
                return;
            }

            var doctor = new Doctor
            {
                Id = DoctorId,
                FirstName = FirstName,
                LastName = LastName,
                Phone = Phone,
                Address = Address,
                Email = Email,
                Specialization = Specialization,
                Salary = salaryValue
            };

            var result = await _doctorService.AddAsync(doctor).ConfigureAwait(true);
            if (!result.Success)
            {
                _messageService.ShowError(result.ErrorMessage ?? "Unable to add doctor.");
                return;
            }

            Doctors.Add(doctor);
            ClearForm();
        }

        private async Task UpdateAsync()
        {
            if (SelectedDoctor == null)
            {
                _messageService.ShowError("Select a doctor to update.");
                return;
            }

            ValidateAllProperties();
            if (HasErrors)
            {
                _messageService.ShowError("Please fix validation errors before saving.");
                return;
            }

            if (!TryParseInt(SalaryText, 0, 100000, out int salaryValue))
            {
                _messageService.ShowError("Salary must be a valid number between 0 and 100000.");
                return;
            }

            SelectedDoctor.Id = DoctorId;
            SelectedDoctor.FirstName = FirstName;
            SelectedDoctor.LastName = LastName;
            SelectedDoctor.Phone = Phone;
            SelectedDoctor.Address = Address;
            SelectedDoctor.Email = Email;
            SelectedDoctor.Specialization = Specialization;
            SelectedDoctor.Salary = salaryValue;

            var result = await _doctorService.UpdateAsync(SelectedDoctor).ConfigureAwait(true);
            if (!result.Success)
            {
                _messageService.ShowError(result.ErrorMessage ?? "Unable to update doctor.");
                return;
            }

            _doctorsView.Refresh();
        }

        private async Task DeleteAsync()
        {
            if (SelectedDoctor == null)
            {
                _messageService.ShowError("Select a doctor to delete.");
                return;
            }

            if (!_messageService.ShowConfirmation($"Delete doctor {SelectedDoctor.FirstName} {SelectedDoctor.LastName}?"))
            {
                return;
            }

            var result = await _doctorService.DeleteAsync(SelectedDoctor.Id).ConfigureAwait(true);
            if (!result.Success)
            {
                _messageService.ShowError(result.ErrorMessage ?? "Unable to delete doctor.");
                return;
            }

            Doctors.Remove(SelectedDoctor);
            ClearForm();
        }

        private void ClearForm()
        {
            RunWithoutValidation(() =>
            {
                SelectedDoctor = null;
                DoctorId = string.Empty;
                FirstName = string.Empty;
                LastName = string.Empty;
                Phone = string.Empty;
                Address = string.Empty;
                Email = string.Empty;
                Specialization = string.Empty;
                SalaryText = string.Empty;
            });
            ResetValidation();
        }

        private bool CanSave() => !HasErrors && !string.IsNullOrWhiteSpace(DoctorId)
                                && !string.IsNullOrWhiteSpace(FirstName)
                                && !string.IsNullOrWhiteSpace(LastName)
                                && !string.IsNullOrWhiteSpace(SalaryText);
        private bool CanUpdate() => SelectedDoctor != null && CanSave();
        private bool CanDelete() => SelectedDoctor != null;
    }
}
