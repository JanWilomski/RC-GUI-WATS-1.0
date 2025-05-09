using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using RiskCheckerGUI.Helpers;
using RiskCheckerGUI.Models;
using RiskCheckerGUI.Services;

namespace RiskCheckerGUI.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        private readonly TcpService _tcpService;
        private ObservableCollection<Control> _controls;
        private Control _selectedControl;
        private string _controlScope;
        private ControlType _controlType;
        private string _controlValue;

        public ObservableCollection<Control> Controls
        {
            get => _controls;
            set => SetProperty(ref _controls, value);
        }

        public Control SelectedControl
        {
            get => _selectedControl;
            set
            {
                if (SetProperty(ref _selectedControl, value) && value != null)
                {
                    // Aktualizuj pola formularza
                    ControlScope = value.Scope;
                    ControlType = value.ControlName;
                    ControlValue = value.Value;
                }
            }
        }

        public string ControlScope
        {
            get => _controlScope;
            set => SetProperty(ref _controlScope, value);
        }

        public ControlType ControlType
        {
            get => _controlType;
            set => SetProperty(ref _controlType, value);
        }

        public string ControlValue
        {
            get => _controlValue;
            set => SetProperty(ref _controlValue, value);
        }

        public RelayCommand AddControlCommand { get; }
        public RelayCommand UpdateControlCommand { get; }
        public RelayCommand DeleteControlCommand { get; }
        public RelayCommand GetControlsHistoryCommand { get; }

        public SettingsViewModel(TcpService tcpService)
        {
            _tcpService = tcpService;
            _controls = new ObservableCollection<Control>();

            // Inicjalizacja komend
            AddControlCommand = new RelayCommand(async _ => await AddControlAsync());
            UpdateControlCommand = new RelayCommand(async _ => await UpdateControlAsync(), _ => SelectedControl != null);
            DeleteControlCommand = new RelayCommand(async _ => await DeleteControlAsync(), _ => SelectedControl != null);
            GetControlsHistoryCommand = new RelayCommand(async _ => await GetControlsHistoryAsync());

            // Subskrypcja zdarzeń
            // Na razie zostawiamy to puste - zaimplementujemy później
        }

        private async Task AddControlAsync()
        {
            try
            {
                var control = new Control
                {
                    Scope = ControlScope,
                    ControlName = ControlType,
                    Value = ControlValue
                };

                // Wysłanie kontroli do serwera
                await _tcpService.SendControlAsync(control);

                // Dodanie do lokalnej kolekcji
                Controls.Add(control);

                // Wyczyść formularz
                ClearForm();
            }
            catch (Exception ex)
            {
                // Obsługa błędów - na razie tylko logujemy
                Console.WriteLine($"Error adding control: {ex.Message}");
            }
        }

        private async Task UpdateControlAsync()
        {
            if (SelectedControl == null)
                return;

            try
            {
                // Aktualizacja wybranej kontroli
                SelectedControl.Scope = ControlScope;
                SelectedControl.ControlName = ControlType;
                SelectedControl.Value = ControlValue;

                // Wysłanie zaktualizowanej kontroli do serwera
                await _tcpService.SendControlAsync(SelectedControl);

                // Odświeżenie widoku
                var index = Controls.IndexOf(SelectedControl);
                Controls.Remove(SelectedControl);
                Controls.Insert(index, SelectedControl);
            }
            catch (Exception ex)
            {
                // Obsługa błędów
                Console.WriteLine($"Error updating control: {ex.Message}");
            }
        }

        private async Task DeleteControlAsync()
        {
            if (SelectedControl == null)
                return;

            try
            {
                // Usunięcie kontroli poprzez wysłanie kontroli z pustą wartością
                var deleteControl = new Control
                {
                    Scope = SelectedControl.Scope,
                    ControlName = SelectedControl.ControlName,
                    Value = string.Empty // Pusta wartość oznacza usunięcie kontroli
                };

                await _tcpService.SendControlAsync(deleteControl);

                // Usunięcie z lokalnej kolekcji
                Controls.Remove(SelectedControl);
                ClearForm();
            }
            catch (Exception ex)
            {
                // Obsługa błędów
                Console.WriteLine($"Error deleting control: {ex.Message}");
            }
        }

        private async Task GetControlsHistoryAsync()
        {
            try
            {
                // Pobieranie historii kontroli
                await _tcpService.SendGetControlsHistoryAsync();
                
                // Reszta implementacji zależy od sposobu obsługi odpowiedzi
                // Na razie zostawiamy to puste - zaimplementujemy później
            }
            catch (Exception ex)
            {
                // Obsługa błędów
                Console.WriteLine($"Error getting controls history: {ex.Message}");
            }
        }

        private void ClearForm()
        {
            ControlScope = string.Empty;
            ControlType = 0; // Domyślny typ
            ControlValue = string.Empty;
            SelectedControl = null;
        }
    }
}