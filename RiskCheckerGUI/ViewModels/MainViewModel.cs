using System;
using System.Threading.Tasks;
using RiskCheckerGUI.Helpers;
using RiskCheckerGUI.Services;

namespace RiskCheckerGUI.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly TcpService _tcpService;
        private readonly UdpService _udpService;

        private string _host = "localhost";
        private int _tcpPort = 8000;
        private string _multicastGroup = "239.0.0.1";
        private int _udpPort = 8001;
        private bool _isConnected;

        public string Host
        {
            get => _host;
            set => SetProperty(ref _host, value);
        }

        public int TcpPort
        {
            get => _tcpPort;
            set => SetProperty(ref _tcpPort, value);
        }

        public string MulticastGroup
        {
            get => _multicastGroup;
            set => SetProperty(ref _multicastGroup, value);
        }

        public int UdpPort
        {
            get => _udpPort;
            set => SetProperty(ref _udpPort, value);
        }

        public bool IsConnected
        {
            get => _isConnected;
            set => SetProperty(ref _isConnected, value);
        }

        public RelayCommand ConnectCommand { get; }
        public RelayCommand DisconnectCommand { get; }

        // Child view models
        public MessagesViewModel MessagesViewModel { get; }
        public SettingsViewModel SettingsViewModel { get; }
        public FiltersViewModel FiltersViewModel { get; }
        public InstrumentsViewModel InstrumentsViewModel { get; }

        public MainViewModel()
        {
            // Skonfiguruj domyślne ustawienia połączenia
            Host = "172.31.136.7";
            TcpPort = 19083;
            
            // Jeśli używany jest multicast, zaktualizuj te ustawienia
            // na podstawie dokumentacji lub zapytaj dostawcę serwera
            MulticastGroup = "239.0.0.1"; // Przykładowy adres multicast
            UdpPort = 19084; // Przykładowy port, zapytaj o właściwy
            
            // Tworzenie usług z zaktualizowanymi ustawieniami
            _tcpService = new TcpService(Host, TcpPort);
            _udpService = new UdpService(MulticastGroup, UdpPort);
            
            // Tworzenie view modeli
            MessagesViewModel = new MessagesViewModel(_tcpService, _udpService);
            SettingsViewModel = new SettingsViewModel(_tcpService);
            FiltersViewModel = new FiltersViewModel();
            InstrumentsViewModel = new InstrumentsViewModel();
            
            // Tworzenie komend
            ConnectCommand = new RelayCommand(async _ => await ConnectAsync(), _ => !IsConnected);
            DisconnectCommand = new RelayCommand(_ => Disconnect(), _ => IsConnected);
        }

        private async Task ConnectAsync()
        {
            try
            {
                // Aktualizacja ustawień serwisów
                _tcpService.UpdateConnection(Host, TcpPort);
                _udpService.UpdateConnection(MulticastGroup, UdpPort);

                // Informacja o próbie połączenia
                StatusMessage = "Connecting...";
                
                // Połączenie z serwerem
                await _tcpService.ConnectAsync();
                _udpService.Start();
                
                IsConnected = true;
                StatusMessage = $"Connected to {Host}:{TcpPort}";
            }
            catch (Exception ex)
            {
                // Obsługa błędu połączenia
                StatusMessage = $"Connection error: {ex.Message}";
                System.Windows.MessageBox.Show($"Failed to connect: {ex.Message}\n\nPlease check server address and port.", 
                    "Connection Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        // Dodaj właściwość do statusu
        private string _statusMessage = "Not connected";
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        private void Disconnect()
        {
            _tcpService.Disconnect();
            _udpService.Stop();
            IsConnected = false;
        }

        // Clean up resources
        public void Cleanup()
        {
            Disconnect();
            MessagesViewModel.Cleanup();
        }
    }
}