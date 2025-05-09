using System.Collections.ObjectModel;
using RiskCheckerGUI.Models;

namespace RiskCheckerGUI.ViewModels
{
    public class FiltersViewModel : ViewModelBase
    {
        private ObservableCollection<FilterSettings> _filters;
        private FilterSettings _selectedFilter;
        private string _messageTypeFilter;
        private string _symbolFilter;
        private string _isinFilter;
        private bool _showDebugMessages;
        private bool _showInfoMessages;
        private bool _showWarningMessages;
        private bool _showErrorMessages;

        public ObservableCollection<FilterSettings> Filters
        {
            get => _filters;
            set => SetProperty(ref _filters, value);
        }

        public FilterSettings SelectedFilter
        {
            get => _selectedFilter;
            set
            {
                if (SetProperty(ref _selectedFilter, value) && value != null)
                {
                    // Aktualizuj pola formularza
                    MessageTypeFilter = value.MessageTypeFilter;
                    SymbolFilter = value.SymbolFilter;
                    IsinFilter = value.IsinFilter;
                    ShowDebugMessages = value.ShowDebugMessages;
                    ShowInfoMessages = value.ShowInfoMessages;
                    ShowWarningMessages = value.ShowWarningMessages;
                    ShowErrorMessages = value.ShowErrorMessages;
                }
            }
        }

        public string MessageTypeFilter
        {
            get => _messageTypeFilter;
            set => SetProperty(ref _messageTypeFilter, value);
        }

        public string SymbolFilter
        {
            get => _symbolFilter;
            set => SetProperty(ref _symbolFilter, value);
        }

        public string IsinFilter
        {
            get => _isinFilter;
            set => SetProperty(ref _isinFilter, value);
        }

        public bool ShowDebugMessages
        {
            get => _showDebugMessages;
            set => SetProperty(ref _showDebugMessages, value);
        }

        public bool ShowInfoMessages
        {
            get => _showInfoMessages;
            set => SetProperty(ref _showInfoMessages, value);
        }

        public bool ShowWarningMessages
        {
            get => _showWarningMessages;
            set => SetProperty(ref _showWarningMessages, value);
        }

        public bool ShowErrorMessages
        {
            get => _showErrorMessages;
            set => SetProperty(ref _showErrorMessages, value);
        }

        public FiltersViewModel()
        {
            // Inicjalizacja kolekcji filtrów
            _filters = new ObservableCollection<FilterSettings>();
            
            // Domyślne ustawienia filtrów
            _showDebugMessages = false;
            _showInfoMessages = true;
            _showWarningMessages = true;
            _showErrorMessages = true;
        }

        // Metoda do dodawania nowego filtru
        public void AddFilter()
        {
            var filter = new FilterSettings
            {
                Name = $"Filter {Filters.Count + 1}",
                MessageTypeFilter = MessageTypeFilter,
                SymbolFilter = SymbolFilter,
                IsinFilter = IsinFilter,
                ShowDebugMessages = ShowDebugMessages,
                ShowInfoMessages = ShowInfoMessages,
                ShowWarningMessages = ShowWarningMessages,
                ShowErrorMessages = ShowErrorMessages
            };

            Filters.Add(filter);
            ClearForm();
        }

        // Metoda do aktualizacji istniejącego filtru
        public void UpdateFilter()
        {
            if (SelectedFilter != null)
            {
                SelectedFilter.MessageTypeFilter = MessageTypeFilter;
                SelectedFilter.SymbolFilter = SymbolFilter;
                SelectedFilter.IsinFilter = IsinFilter;
                SelectedFilter.ShowDebugMessages = ShowDebugMessages;
                SelectedFilter.ShowInfoMessages = ShowInfoMessages;
                SelectedFilter.ShowWarningMessages = ShowWarningMessages;
                SelectedFilter.ShowErrorMessages = ShowErrorMessages;

                // Odświeżenie widoku
                var index = Filters.IndexOf(SelectedFilter);
                Filters.Remove(SelectedFilter);
                Filters.Insert(index, SelectedFilter);
            }
        }

        // Metoda do usuwania filtru
        public void DeleteFilter()
        {
            if (SelectedFilter != null)
            {
                Filters.Remove(SelectedFilter);
                ClearForm();
            }
        }

        private void ClearForm()
        {
            MessageTypeFilter = string.Empty;
            SymbolFilter = string.Empty;
            IsinFilter = string.Empty;
            ShowDebugMessages = false;
            ShowInfoMessages = true;
            ShowWarningMessages = true;
            ShowErrorMessages = true;
            SelectedFilter = null;
        }
    }
}