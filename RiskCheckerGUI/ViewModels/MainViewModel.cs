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
            _tcpService = new TcpService(Host, TcpPort);
            _udpService = new UdpService(MulticastGroup, UdpPort);

            // Create child view models, ale nie inicjalizuj połączenia
            MessagesViewModel = new MessagesViewModel(_tcpService, _udpService);
            SettingsViewModel = new SettingsViewModel(_tcpService);
            FiltersViewModel = new FiltersViewModel();
            InstrumentsViewModel = new InstrumentsViewModel();

            // Create commands
            ConnectCommand = new RelayCommand(async _ => await ConnectAsync(), _ => !IsConnected);
            DisconnectCommand = new RelayCommand(_ => Disconnect(), _ => IsConnected);
        }

        private async Task ConnectAsync()
        {
            try
            {
                // Update services with current settings
                _tcpService.UpdateConnection(Host, TcpPort);
                _udpService.UpdateConnection(MulticastGroup, UdpPort);

                // Connect to services
                await _tcpService.ConnectAsync();
                _udpService.Start();
                
                IsConnected = true;
            }
            catch (Exception ex)
            {
                // Handle connection error
                System.Windows.MessageBox.Show($"Connection error: {ex.Message}", "Error", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
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