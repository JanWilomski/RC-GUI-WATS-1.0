using System;
using System.Threading.Tasks;
using System.Windows;
using System.Diagnostics;
using System.Net.Sockets;
using System.Windows.Input;
using RiskCheckerGUI.Helpers;
using RiskCheckerGUI.Services;

namespace RiskCheckerGUI.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly TcpService _tcpService;
        private readonly UdpService _udpService;

        private string _host = "172.31.136.7";  // Domyślny host
        private int _tcpPort = 19083;           // Domyślny port TCP
        private string _multicastGroup = "239.0.0.1";  // Domyślny adres multicast
        private int _udpPort = 19084;           // Domyślny port UDP
        private bool _isConnected;
        private string _statusMessage = "Not connected";
        private bool _offlineMode;

        #region Properties

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
            set
            {
                if (SetProperty(ref _isConnected, value))
                {
                    // Odświeżenie stanu komend
                    CommandManager.InvalidateRequerySuggested();
                    Debug.WriteLine($"IsConnected zmienione na: {value}");
                }
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public bool OfflineMode
        {
            get => _offlineMode;
            set
            {
                if (SetProperty(ref _offlineMode, value))
                {
                    // Jeśli włączono tryb offline, załaduj przykładowe dane
                    if (value)
                    {
                        MessagesViewModel.LoadSampleData();
                        StatusMessage = "Offline mode enabled";
                    }
                    else
                    {
                        StatusMessage = "Offline mode disabled";
                        MessagesViewModel.ClearData();
                    }
                }
            }
        }

        #endregion

        #region Commands

        public RelayCommand ConnectCommand { get; }
        public RelayCommand DisconnectCommand { get; }
        public RelayCommand RefreshDataCommand { get; }
        public RelayCommand TestConnectionCommand { get; }

        #endregion

        #region ViewModels

        public MessagesViewModel MessagesViewModel { get; }
        public SettingsViewModel SettingsViewModel { get; }
        public FiltersViewModel FiltersViewModel { get; }
        public InstrumentsViewModel InstrumentsViewModel { get; }

        #endregion

        public MainViewModel()
        {
            // Inicjalizacja TcpService i UdpService
            _tcpService = new TcpService(Host, TcpPort);
            _udpService = new UdpService(MulticastGroup, UdpPort);

            // Podpięcie się pod zdarzenia zmiany statusu połączenia
            _tcpService.ConnectionStatusChanged += OnConnectionStatusChanged;

            // Inicjalizacja ViewModeli dla zakładek
            MessagesViewModel = new MessagesViewModel(_tcpService, _udpService);
            SettingsViewModel = new SettingsViewModel(_tcpService);
            FiltersViewModel = new FiltersViewModel();
            InstrumentsViewModel = new InstrumentsViewModel();

            // Inicjalizacja komend
            ConnectCommand = new RelayCommand(async _ => await ConnectAsync(), _ => !IsConnected);
            DisconnectCommand = new RelayCommand(_ => Disconnect(), _ => IsConnected);
            RefreshDataCommand = new RelayCommand(async _ => await RefreshDataAsync(), _ => IsConnected);
            TestConnectionCommand = new RelayCommand(_ => TestConnection());
        }

        private void OnConnectionStatusChanged(object sender, TcpService.ConnectionEventArgs e)
        {
            IsConnected = e.IsConnected;
            StatusMessage = e.Message;
            
            // Odświeżenie stanu komend
            CommandManager.InvalidateRequerySuggested();
        }

        private async Task ConnectAsync()
        {
            try
            {
                if (OfflineMode)
                {
                    StatusMessage = "Working in offline mode";
                    IsConnected = true;
                    return;
                }

                // Aktualizacja ustawień serwisów
                _tcpService.UpdateConnection(Host, TcpPort);
                _udpService.UpdateConnection(MulticastGroup, UdpPort);

                StatusMessage = "Connecting...";
                
                // Połączenie z serwerem
                await _tcpService.ConnectAsync();
                
                // Po udanym połączeniu TCP możemy uruchomić UDP
                _udpService.Start();
                
                // Poproś o historię wiadomości
                await _tcpService.SendRewindAsync(0);
                
                // Zażądaj historii kontroli
                await _tcpService.SendGetControlsHistoryAsync();
                
                // Połączenie zakończone sukcesem - updateView jest obsługiwane przez event
            }
            catch (Exception ex)
            {
                // Obsługa błędu połączenia
                IsConnected = false;
                StatusMessage = $"Connection error: {ex.Message}";
                MessageBox.Show($"Failed to connect: {ex.Message}\n\nPlease check server address and port.", 
                    "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Disconnect()
        {
            _tcpService.Disconnect();
            _udpService.Stop();
            IsConnected = false;
            StatusMessage = "Disconnected";
        }

        private async Task RefreshDataAsync()
        {
            try
            {
                if (!IsConnected)
                {
                    MessageBox.Show("Cannot refresh data. Not connected to server.\nPlease connect first.", 
                        "Not Connected", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                StatusMessage = "Refreshing data...";
                
                // Poproś o ponowne przesłanie danych
                await _tcpService.SendRewindAsync(0);
                
                StatusMessage = "Data refresh requested";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Refresh error: {ex.Message}";
                MessageBox.Show($"Failed to refresh data: {ex.Message}", 
                    "Refresh Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TestConnection()
        {
            try
            {
                StatusMessage = "Testing connection...";
                
                using (var tcpClient = new TcpClient())
                {
                    // Ustaw timeout
                    var connectTask = tcpClient.ConnectAsync(Host, TcpPort);
                    var timeoutTask = Task.Delay(5000); // 5 sekund timeout
                    
                    var completedTask = Task.WhenAny(connectTask, timeoutTask).GetAwaiter().GetResult();
                    
                    if (completedTask == timeoutTask)
                    {
                        StatusMessage = "Connection test failed: Timeout";
                        MessageBox.Show($"Connection test to {Host}:{TcpPort} timed out after 5 seconds.", 
                            "Connection Test", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    
                    if (tcpClient.Connected)
                    {
                        StatusMessage = "Connection test successful";
                        MessageBox.Show($"Successfully connected to {Host}:{TcpPort}.", 
                            "Connection Test", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        StatusMessage = "Connection test failed";
                        MessageBox.Show($"Failed to connect to {Host}:{TcpPort}.", 
                            "Connection Test", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Connection test error: {ex.Message}";
                MessageBox.Show($"Connection test error: {ex.Message}", 
                    "Connection Test Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Metoda do zwolnienia zasobów przy zamykaniu aplikacji
        public void Cleanup()
        {
            Disconnect();
            
            // Odpiąć handler
            _tcpService.ConnectionStatusChanged -= OnConnectionStatusChanged;
            
            // Posprzątać w ViewModelach
            MessagesViewModel.Cleanup();
        }
    }
}