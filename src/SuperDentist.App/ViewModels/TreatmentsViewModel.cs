using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SuperDentist.App.Services;
using SuperDentist.Core;
using SuperDentist.Core.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using System.Windows.Data;

namespace SuperDentist.App.ViewModels
{
    public sealed partial class TreatmentsViewModel : ViewModelBase
    {
        private readonly ITreatmentService _treatmentService;
        private readonly IMessageService _messageService;
        private readonly ICollectionView _treatmentsView;

        public TreatmentsViewModel(ITreatmentService treatmentService, IMessageService messageService)
        {
            _treatmentService = treatmentService;
            _messageService = messageService;

            Treatments = new ObservableCollection<Treatment>();
            _treatmentsView = CollectionViewSource.GetDefaultView(Treatments);
            _treatmentsView.Filter = FilterTreatments;

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

        public ObservableCollection<Treatment> Treatments { get; }

        [ObservableProperty]
        private Treatment? selectedTreatment;

        [Required(ErrorMessage = "Treatment number is required.")]
        [ObservableProperty]
        private string treatmentNumber = string.Empty;

        [ObservableProperty]
        private string treatmentType = string.Empty;

        [Required(ErrorMessage = "Price is required.")]
        [RegularExpression(@"^\d+$", ErrorMessage = "Price must be numeric.")]
        [ObservableProperty]
        private string priceText = string.Empty;

        [ObservableProperty]
        private string tools = string.Empty;

        [ObservableProperty]
        private string searchText = string.Empty;

        public IAsyncRelayCommand LoadCommand { get; }
        public IAsyncRelayCommand AddCommand { get; }
        public IAsyncRelayCommand UpdateCommand { get; }
        public IAsyncRelayCommand DeleteCommand { get; }
        public IRelayCommand ClearCommand { get; }

        partial void OnSelectedTreatmentChanged(Treatment? value)
        {
            if (value == null)
            {
                UpdateCommand.NotifyCanExecuteChanged();
                DeleteCommand.NotifyCanExecuteChanged();
                return;
            }

            TreatmentNumber = value.Number;
            TreatmentType = value.Type;
            PriceText = value.Price.ToString();
            Tools = value.Tools;
            UpdateCommand.NotifyCanExecuteChanged();
            DeleteCommand.NotifyCanExecuteChanged();
        }

        partial void OnSearchTextChanged(string value) => _treatmentsView.Refresh();

        private bool FilterTreatments(object item)
        {
            if (item is not Treatment treatment)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(SearchText))
            {
                return true;
            }

            string query = SearchText.Trim().ToLowerInvariant();
            return treatment.Number.ToLowerInvariant().Contains(query) ||
                   treatment.Type.ToLowerInvariant().Contains(query);
        }

        private async Task LoadAsync()
        {
            IsBusy = true;
            try
            {
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

            if (!TryParseInt(PriceText, 0, 10000, out int priceValue))
            {
                _messageService.ShowError("Price must be a valid number between 0 and 10000.");
                return;
            }

            var treatment = new Treatment
            {
                Number = TreatmentNumber,
                Type = TreatmentType,
                Price = priceValue,
                Tools = Tools
            };

            var result = await _treatmentService.AddAsync(treatment).ConfigureAwait(true);
            if (!result.Success)
            {
                _messageService.ShowError(result.ErrorMessage ?? "Unable to add treatment.");
                return;
            }

            Treatments.Add(treatment);
            ClearForm();
        }

        private async Task UpdateAsync()
        {
            if (SelectedTreatment == null)
            {
                _messageService.ShowError("Select a treatment to update.");
                return;
            }

            ValidateAllProperties();
            if (HasErrors)
            {
                _messageService.ShowError("Please fix validation errors before saving.");
                return;
            }

            if (!TryParseInt(PriceText, 0, 10000, out int priceValue))
            {
                _messageService.ShowError("Price must be a valid number between 0 and 10000.");
                return;
            }

            SelectedTreatment.Number = TreatmentNumber;
            SelectedTreatment.Type = TreatmentType;
            SelectedTreatment.Price = priceValue;
            SelectedTreatment.Tools = Tools;

            var result = await _treatmentService.UpdateAsync(SelectedTreatment).ConfigureAwait(true);
            if (!result.Success)
            {
                _messageService.ShowError(result.ErrorMessage ?? "Unable to update treatment.");
                return;
            }

            _treatmentsView.Refresh();
        }

        private async Task DeleteAsync()
        {
            if (SelectedTreatment == null)
            {
                _messageService.ShowError("Select a treatment to delete.");
                return;
            }

            if (!_messageService.ShowConfirmation($"Delete treatment {SelectedTreatment.Number}?"))
            {
                return;
            }

            var result = await _treatmentService.DeleteAsync(SelectedTreatment.Number).ConfigureAwait(true);
            if (!result.Success)
            {
                _messageService.ShowError(result.ErrorMessage ?? "Unable to delete treatment.");
                return;
            }

            Treatments.Remove(SelectedTreatment);
            ClearForm();
        }

        private void ClearForm()
        {
            RunWithoutValidation(() =>
            {
                SelectedTreatment = null;
                TreatmentNumber = string.Empty;
                TreatmentType = string.Empty;
                PriceText = string.Empty;
                Tools = string.Empty;
            });
            ResetValidation();
        }

        private bool CanSave() => !HasErrors
                                && !string.IsNullOrWhiteSpace(TreatmentNumber)
                                && !string.IsNullOrWhiteSpace(PriceText);
        private bool CanUpdate() => SelectedTreatment != null && CanSave();
        private bool CanDelete() => SelectedTreatment != null;
    }
}
