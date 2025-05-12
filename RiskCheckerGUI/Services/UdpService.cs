using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using RiskCheckerGUI.Models;

namespace RiskCheckerGUI.Services
{
    public class UdpService
    {
        private UdpClient _client;
        private string _multicastGroup;
        private int _port;
        private bool _isRunning;
        private CancellationTokenSource _cts;

        public event EventHandler<LogMessage> LogReceived;
        public event EventHandler<Position> PositionReceived;
        public event EventHandler<Capital> CapitalReceived;
        public event EventHandler<byte[]> IOBytesReceived;

        public UdpService(string multicastGroup, int port)
        {
            _multicastGroup = multicastGroup;
            _port = port;
            _isRunning = false;
            _cts = new CancellationTokenSource();
        }

        public string CurrentMulticastGroup => _multicastGroup;
        public int CurrentPort => _port;
        public bool IsRunning => _isRunning;

        public void UpdateConnection(string multicastGroup, int port)
        {
            if (_isRunning)
            {
                Stop();
            }
            
            _multicastGroup = multicastGroup ?? throw new ArgumentNullException(nameof(multicastGroup));
            _port = port;
        }

        public void Start()
        {
            try
            {
                if (_isRunning)
                    return;

                _cts = new CancellationTokenSource();
                Debug.WriteLine($"Uruchamianie nasłuchiwania UDP na multicast {_multicastGroup}:{_port}...");
                
                _client = new UdpClient();
                _client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                _client.Client.Bind(new IPEndPoint(IPAddress.Any, _port));
                
                Debug.WriteLine("Dołączanie do grupy multicast...");
                _client.JoinMulticastGroup(IPAddress.Parse(_multicastGroup));
                
                Debug.WriteLine("Poprawnie dołączono do grupy multicast");
                _isRunning = true;
                
                // Start reading messages
                _ = Task.Run(() => ReceiveMessagesAsync(_cts.Token), _cts.Token);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Błąd inicjalizacji UDP: {ex.Message}\nStackTrace: {ex.StackTrace}");
                throw;
            }
        }

        private async Task ReceiveMessagesAsync(CancellationToken token)
        {
            Debug.WriteLine("Rozpoczęto nasłuchiwanie UDP...");
            
            try
            {
                while (!token.IsCancellationRequested && _client != null)
                {
                    try
                    {
                        Debug.WriteLine("Oczekiwanie na dane UDP...");
                        UdpReceiveResult result = await _client.ReceiveAsync();
                        Debug.WriteLine($"Odebrano {result.Buffer.Length} bajtów danych UDP z {result.RemoteEndPoint}");
                        
                        // Użyj MemoryStream i BinaryReader do parsowania bufora
                        using (var ms = new MemoryStream(result.Buffer))
                        using (var reader = new BinaryReader(ms, Encoding.ASCII))
                        {
                            // Read header (16 bytes)
                            byte[] sessionBytes = reader.ReadBytes(10);
                            if (sessionBytes.Length < 10) continue;
                            
                            string session = Encoding.ASCII.GetString(sessionBytes).TrimEnd('\0');
                            uint sequence = reader.ReadUInt32();
                            ushort blockCount = reader.ReadUInt16();

                            Debug.WriteLine($"UDP Message: Session={session}, Seq={sequence}, BlockCount={blockCount}");

                            if (blockCount == 0)
                            {
                                Debug.WriteLine($"UDP Heartbeat: Session={session}, Seq={sequence}");
                                continue;
                            }

                            for (int i = 0; i < blockCount; i++)
                            {
                                if (ms.Position >= ms.Length) break;
                                
                                ushort blockLength = reader.ReadUInt16();
                                byte[] payload = reader.ReadBytes(blockLength);
                                if (payload.Length == 0) continue;
                                
                                char messageType = (char)payload[0];
                                Debug.WriteLine($"UDP Block {i}: Type={messageType}, Length={blockLength}");
                                
                                switch (messageType)
                                {
                                    case 'P': 
                                        ProcessPositionMessage(payload); 
                                        break;
                                    case 'C': 
                                        ProcessCapitalMessage(payload); 
                                        break;
                                    case 'D':
                                    case 'I':
                                    case 'W':
                                    case 'E': 
                                        ProcessLogMessage(payload); 
                                        break;
                                    case 'B': 
                                        ProcessIOBytesMessage(payload); 
                                        break;
                                    default: 
                                        Debug.WriteLine($"Unknown UDP message type: {messageType}"); 
                                        break;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Błąd odbierania UDP: {ex.Message}");
                        // Nie przerywaj pętli, próbuj dalej
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fatal error w pętli UDP: {ex.Message}\nStackTrace: {ex.StackTrace}");
            }
            finally
            {
                _isRunning = false;
                Debug.WriteLine("Zakończono nasłuchiwanie UDP");
            }
        }

        private void ProcessPositionMessage(byte[] payload)
        {
            try
            {
                // Position message: 1 byte type + 12 bytes ISIN + 3 x 4-byte ints
                string isin = Encoding.ASCII.GetString(payload, 1, 12).Trim('\0');
                int net = BitConverter.ToInt32(payload, 13);
                int openLong = BitConverter.ToInt32(payload, 17);
                int openShort = BitConverter.ToInt32(payload, 21);
                
                Debug.WriteLine($"UDP Position: ISIN={isin}, Net={net}, Long={openLong}, Short={openShort}");
                
                var position = new Position
                {
                    ISIN = isin,
                    Net = net,
                    OpenLong = openLong,
                    OpenShort = openShort,
                    Timestamp = DateTime.Now
                };
                
                PositionReceived?.Invoke(this, position);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error processing UDP position message: {ex.Message}");
            }
        }

        private void ProcessCapitalMessage(byte[] payload)
        {
            try
            {
                // Capital message: 1 byte type + 3 doubles
                double openCapital = BitConverter.ToDouble(payload, 1);
                double accruedCapital = BitConverter.ToDouble(payload, 9);
                double totalCapital = BitConverter.ToDouble(payload, 17);
                
                Debug.WriteLine($"UDP Capital: Open={openCapital}, Accrued={accruedCapital}, Total={totalCapital}");
                
                var capital = new Capital
                {
                    OpenCapital = openCapital,
                    AccruedCapital = accruedCapital,
                    TotalCapital = totalCapital,
                    Timestamp = DateTime.Now
                };
                
                CapitalReceived?.Invoke(this, capital);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error processing UDP capital message: {ex.Message}");
            }
        }

        private void ProcessLogMessage(byte[] payload)
        {
            try
            {
                // Log message: 1 byte type + 2-byte length + message
                char level = (char)payload[0];
                ushort length = BitConverter.ToUInt16(payload, 1);
                string message = Encoding.ASCII.GetString(payload, 3, length);
                
                Debug.WriteLine($"UDP Log [{level}]: {message}");
                
                var logMessage = new LogMessage
                {
                    Type = (LogType)level,
                    Message = message,
                    Timestamp = DateTime.Now
                };
                
                LogReceived?.Invoke(this, logMessage);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error processing UDP log message: {ex.Message}");
            }
        }

        private void ProcessIOBytesMessage(byte[] payload)
        {
            try
            {
                // I/O bytes message: 1 byte type + 2-byte length + message
                ushort length = BitConverter.ToUInt16(payload, 1);
                byte[] message = new byte[length];
                Array.Copy(payload, 3, message, 0, length);
                
                // For debugging
                string messageStr = Encoding.ASCII.GetString(message);
                Debug.WriteLine($"UDP IO: {messageStr}");
                
                IOBytesReceived?.Invoke(this, message);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error processing UDP IO bytes message: {ex.Message}");
            }
        }

        public void Stop()
        {
            try
            {
                _cts.Cancel();
                if (_client != null)
                {
                    _client.DropMulticastGroup(IPAddress.Parse(_multicastGroup));
                    _client.Close();
                    _client = null;
                }
                _isRunning = false;
                Debug.WriteLine("Zatrzymano klienta UDP");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during UDP stop: {ex.Message}");
            }
        }
    }
}