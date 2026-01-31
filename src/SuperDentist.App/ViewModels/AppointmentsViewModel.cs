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
    public sealed partial class AppointmentsViewModel : ViewModelBase
    {
        private readonly IAppointmentService _appointmentService;
        private readonly IPatientService _patientService;
        private readonly IDoctorService _doctorService;
        private readonly ITreatmentService _treatmentService;
        private readonly IMessageService _messageService;
        private readonly ICollectionView _appointmentsView;

        public AppointmentsViewModel(
            IAppointmentService appointmentService,
            IPatientService patientService,
            IDoctorService doctorService,
            ITreatmentService treatmentService,
            IMessageService messageService)
        {
            _appointmentService = appointmentService;
            _patientService = patientService;
            _doctorService = doctorService;
            _treatmentService = treatmentService;
            _messageService = messageService;

            Appointments = new ObservableCollection<Appointment>();
            Patients = new ObservableCollection<Patient>();
            Doctors = new ObservableCollection<Doctor>();
            Treatments = new ObservableCollection<Treatment>();

            _appointmentsView = CollectionViewSource.GetDefaultView(Appointments);
            _appointmentsView.Filter = FilterAppointments;

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

        public ObservableCollection<Appointment> Appointments { get; }
        public ObservableCollection<Patient> Patients { get; }
        public ObservableCollection<Doctor> Doctors { get; }
        public ObservableCollection<Treatment> Treatments { get; }

        [ObservableProperty]
        private Appointment? selectedAppointment;

        [Required(ErrorMessage = "Patient ID is required.")]
        [ObservableProperty]
        private string patientId = string.Empty;

        [Required(ErrorMessage = "Doctor ID is required.")]
        [ObservableProperty]
        private string doctorId = string.Empty;

        [Required(ErrorMessage = "Date is required.")]
        [RegularExpression(@"^\d{4}-\d{2}-\d{2}$", ErrorMessage = "Date must be YYYY-MM-DD.")]
        [ObservableProperty]
        private string appointmentDate = DateTime.Today.ToString("yyyy-MM-dd");

        [Required(ErrorMessage = "Time is required.")]
        [RegularExpression(@"^\d{2}:\d{2}$", ErrorMessage = "Time must be HH:MM.")]
        [ObservableProperty]
        private string appointmentTime = "09:00";

        [ObservableProperty]
        private string treatmentNumber = string.Empty;

        [ObservableProperty]
        private string searchText = string.Empty;

        public IAsyncRelayCommand LoadCommand { get; }
        public IAsyncRelayCommand AddCommand { get; }
        public IAsyncRelayCommand UpdateCommand { get; }
        public IAsyncRelayCommand DeleteCommand { get; }
        public IRelayCommand ClearCommand { get; }

        partial void OnSelectedAppointmentChanged(Appointment? value)
        {
            if (value == null)
            {
                UpdateCommand.NotifyCanExecuteChanged();
                DeleteCommand.NotifyCanExecuteChanged();
                return;
            }

            PatientId = value.PatientId;
            DoctorId = value.DoctorId;
            AppointmentDate = value.Date;
            AppointmentTime = value.Time;
            TreatmentNumber = value.TreatmentNumber;
            UpdateCommand.NotifyCanExecuteChanged();
            DeleteCommand.NotifyCanExecuteChanged();
        }

        partial void OnSearchTextChanged(string value) => _appointmentsView.Refresh();

        private bool FilterAppointments(object item)
        {
            if (item is not Appointment appointment)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(SearchText))
            {
                return true;
            }

            string query = SearchText.Trim().ToLowerInvariant();
            return appointment.PatientId.Contains(query) ||
                   appointment.DoctorId.Contains(query) ||
                   appointment.Date.Contains(query);
        }

        private async Task LoadAsync()
        {
            IsBusy = true;
            try
            {
                Appointments.Clear();
                foreach (var appointment in await _appointmentService.GetAllAsync().ConfigureAwait(true))
                {
                    Appointments.Add(appointment);
                }

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

            var appointment = new Appointment
            {
                PatientId = PatientId,
                DoctorId = DoctorId,
                Date = AppointmentDate,
                Time = AppointmentTime,
                TreatmentNumber = TreatmentNumber
            };

            var result = await _appointmentService.AddAsync(appointment).ConfigureAwait(true);
            if (!result.Success)
            {
                _messageService.ShowError(result.ErrorMessage ?? "Unable to add appointment.");
                return;
            }

            Appointments.Add(appointment);
            ClearForm();
        }

        private async Task UpdateAsync()
        {
            if (SelectedAppointment == null)
            {
                _messageService.ShowError("Select an appointment to update.");
                return;
            }

            ValidateAllProperties();
            if (HasErrors)
            {
                _messageService.ShowError("Please fix validation errors before saving.");
                return;
            }

            SelectedAppointment.PatientId = PatientId;
            SelectedAppointment.DoctorId = DoctorId;
            SelectedAppointment.Date = AppointmentDate;
            SelectedAppointment.Time = AppointmentTime;
            SelectedAppointment.TreatmentNumber = TreatmentNumber;

            var result = await _appointmentService.UpdateAsync(SelectedAppointment).ConfigureAwait(true);
            if (!result.Success)
            {
                _messageService.ShowError(result.ErrorMessage ?? "Unable to update appointment.");
                return;
            }

            _appointmentsView.Refresh();
        }

        private async Task DeleteAsync()
        {
            if (SelectedAppointment == null)
            {
                _messageService.ShowError("Select an appointment to delete.");
                return;
            }

            if (!_messageService.ShowConfirmation("Delete this appointment?"))
            {
                return;
            }

            var result = await _appointmentService.DeleteAsync(SelectedAppointment.PatientId).ConfigureAwait(true);
            if (!result.Success)
            {
                _messageService.ShowError(result.ErrorMessage ?? "Unable to delete appointment.");
                return;
            }

            Appointments.Remove(SelectedAppointment);
            ClearForm();
        }

        private void ClearForm()
        {
            RunWithoutValidation(() =>
            {
                SelectedAppointment = null;
                PatientId = string.Empty;
                DoctorId = string.Empty;
                AppointmentDate = DateTime.Today.ToString("yyyy-MM-dd");
                AppointmentTime = "09:00";
                TreatmentNumber = string.Empty;
            });
            ResetValidation();
        }

        private bool CanSave() => !HasErrors
                                && !string.IsNullOrWhiteSpace(PatientId)
                                && !string.IsNullOrWhiteSpace(DoctorId)
                                && !string.IsNullOrWhiteSpace(AppointmentDate)
                                && !string.IsNullOrWhiteSpace(AppointmentTime);
        private bool CanUpdate() => SelectedAppointment != null && CanSave();
        private bool CanDelete() => SelectedAppointment != null;
    }
}
