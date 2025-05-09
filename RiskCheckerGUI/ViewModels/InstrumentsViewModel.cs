using System.Collections.ObjectModel;
using System.Threading.Tasks;
using RiskCheckerGUI.Helpers;
using RiskCheckerGUI.Models;

namespace RiskCheckerGUI.ViewModels
{
    public class InstrumentsViewModel : ViewModelBase
    {
        private ObservableCollection<Instrument> _instruments;
        private Instrument _selectedInstrument;
        private string _filterText;

        public ObservableCollection<Instrument> Instruments
        {
            get => _instruments;
            set => SetProperty(ref _instruments, value);
        }

        public Instrument SelectedInstrument
        {
            get => _selectedInstrument;
            set => SetProperty(ref _selectedInstrument, value);
        }

        public string FilterText
        {
            get => _filterText;
            set
            {
                if (SetProperty(ref _filterText, value))
                {
                    ApplyFilter();
                }
            }
        }

        public RelayCommand RefreshCommand { get; }

        public InstrumentsViewModel()
        {
            _instruments = new ObservableCollection<Instrument>();
            _filterText = string.Empty;

            // Inicjalizacja komend
            RefreshCommand = new RelayCommand(_ => RefreshInstruments());
            
            // Wczytanie instrumentów
            LoadInstruments();
        }

        private void LoadInstruments()
        {
            // W prawdziwej implementacji wczytujemy instrumenty z pliku lub z serwera
            // Na razie dodajemy przykładowe dane
            Instruments.Add(new Instrument { ISIN = "PLPKO0000016", Symbol = "PKO", Name = "PKO BP", Class = "Akcja" });
            Instruments.Add(new Instrument { ISIN = "PLKGHM000017", Symbol = "KGH", Name = "KGHM", Class = "Akcja" });
            Instruments.Add(new Instrument { ISIN = "PLPEKAO00016", Symbol = "PEO", Name = "PEKAO", Class = "Akcja" });
            Instruments.Add(new Instrument { ISIN = "PLOPTTC00011", Symbol = "OPL", Name = "Orange Polska", Class = "Akcja" });
            Instruments.Add(new Instrument { ISIN = "PLTLKPL00017", Symbol = "TKO", Name = "Telekom Polska", Class = "Akcja" });
        }

        private void RefreshInstruments()
        {
            // Odświeżenie danych instrumentów
            Instruments.Clear();
            LoadInstruments();
        }

        private void ApplyFilter()
        {
            if (string.IsNullOrEmpty(FilterText))
            {
                // Jeśli filtr jest pusty, pokaż wszystkie instrumenty
                RefreshInstruments();
                return;
            }

            // Zastosuj filtr do instrumentów
            var filteredInstruments = Instruments.Where(i =>
                i.ISIN.Contains(FilterText, StringComparison.OrdinalIgnoreCase) ||
                i.Symbol.Contains(FilterText, StringComparison.OrdinalIgnoreCase) ||
                i.Name.Contains(FilterText, StringComparison.OrdinalIgnoreCase) ||
                i.Class.Contains(FilterText, StringComparison.OrdinalIgnoreCase)
            ).ToList();

            Instruments.Clear();
            foreach (var instrument in filteredInstruments)
            {
                Instruments.Add(instrument);
            }
        }
    }
}