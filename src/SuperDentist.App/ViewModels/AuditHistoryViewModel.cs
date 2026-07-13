using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SuperDentist.App.Services;
using SuperDentist.Core;
using SuperDentist.Core.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace SuperDentist.App.ViewModels
{
    public sealed partial class AuditHistoryViewModel : ViewModelBase
    {
        private readonly IAuditService _auditService;
        private readonly IMessageService _messageService;

        [ObservableProperty]
        private string selectedEntityType = "All";

        [ObservableProperty]
        private string selectedOperation = "All";

        [ObservableProperty]
        private string actorFilter = string.Empty;

        [ObservableProperty]
        private string entityIdFilter = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SelectedOldValues))]
        [NotifyPropertyChangedFor(nameof(SelectedNewValues))]
        private AuditEntryDisplayItem? selectedEntry;

        public AuditHistoryViewModel(
            IAuditService auditService,
            IMessageService messageService)
        {
            _auditService = auditService;
            _messageService = messageService;

            EntityTypes = new[] { "All" }.Concat(AuditEntityTypes.All).ToArray();
            Operations = new[] { "All" }.Concat(Enum.GetNames<AuditOperation>()).ToArray();
            SearchCommand = new AsyncRelayCommand(LoadAsync);
            ClearFiltersCommand = new AsyncRelayCommand(ClearFiltersAsync);

            SearchCommand.Execute(null);
        }

        public ObservableCollection<AuditEntryDisplayItem> Entries { get; } = new();
        public IReadOnlyList<string> EntityTypes { get; }
        public IReadOnlyList<string> Operations { get; }
        public IAsyncRelayCommand SearchCommand { get; }
        public IAsyncRelayCommand ClearFiltersCommand { get; }

        public string SelectedOldValues => PrettyJson(SelectedEntry?.Entry.OldValues);
        public string SelectedNewValues => PrettyJson(SelectedEntry?.Entry.NewValues);

        private async Task LoadAsync()
        {
            IsBusy = true;
            try
            {
                var query = new AuditQuery
                {
                    EntityType = SelectedEntityType == "All" ? null : SelectedEntityType,
                    EntityId = NullIfWhiteSpace(EntityIdFilter),
                    Actor = NullIfWhiteSpace(ActorFilter),
                    Operation = Enum.TryParse(SelectedOperation, out AuditOperation operation)
                        ? operation
                        : null,
                    Limit = 250
                };

                IReadOnlyList<AuditEntry> entries = await _auditService.SearchAsync(query).ConfigureAwait(true);
                Entries.Clear();
                foreach (AuditEntry entry in entries)
                {
                    Entries.Add(new AuditEntryDisplayItem(entry));
                }

                SelectedEntry = Entries.FirstOrDefault();
            }
            catch (Exception)
            {
                _messageService.ShowError("Unable to load audit history. Please check the application logs.");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ClearFiltersAsync()
        {
            SelectedEntityType = "All";
            SelectedOperation = "All";
            ActorFilter = string.Empty;
            EntityIdFilter = string.Empty;
            await LoadAsync().ConfigureAwait(true);
        }

        private static string? NullIfWhiteSpace(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static string PrettyJson(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return "No values recorded.";
            }

            try
            {
                using JsonDocument document = JsonDocument.Parse(json);
                return JsonSerializer.Serialize(document.RootElement, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
            }
            catch (JsonException)
            {
                return json;
            }
        }
    }

    public sealed class AuditEntryDisplayItem
    {
        public AuditEntryDisplayItem(AuditEntry entry)
        {
            Entry = entry;
        }

        public AuditEntry Entry { get; }
        public string EntityType => Entry.EntityType;
        public string EntityId => Entry.EntityId;
        public AuditOperation Operation => Entry.Operation;
        public string Actor => Entry.Actor;
        public string CorrelationId => Entry.CorrelationId;
        public string TimestampUtc => Entry.TimestampUtc.ToString("yyyy-MM-dd HH:mm:ss 'UTC'");
        public string TimestampLocal => Entry.TimestampUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss zzz");
    }
}
