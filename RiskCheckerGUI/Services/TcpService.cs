using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using RiskCheckerGUI.Models;

namespace RiskCheckerGUI.Services
{
    public class TcpService
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private string _host;
        private int _port;
        private bool _isConnected;
        private CancellationTokenSource _cts;

        public event EventHandler<LogMessage> LogReceived;
        public event EventHandler<Position> PositionReceived;
        public event EventHandler<Capital> CapitalReceived;
        public event EventHandler<byte[]> IOBytesReceived;
        public event EventHandler<string> RewindCompleted;
        public event EventHandler<ConnectionEventArgs> ConnectionStatusChanged;

        public class ConnectionEventArgs : EventArgs
        {
            public bool IsConnected { get; set; }
            public string Message { get; set; }
        }

        public TcpService(string host, int port)
        {
            _host = host;
            _port = port;
            _isConnected = false;
            _cts = new CancellationTokenSource();
        }

        public string CurrentHost => _host;
        public int CurrentPort => _port;
        public bool IsConnected => _isConnected;

        public void UpdateConnection(string host, int port)
        {
            if (_client != null && _client.Connected)
            {
                Disconnect();
            }
            
            _host = host ?? throw new ArgumentNullException(nameof(host));
            _port = port;
        }

        public async Task ConnectAsync()
        {
            try
            {
                if (_isConnected)
                {
                    Disconnect();
                }

                _cts = new CancellationTokenSource();
                Debug.WriteLine($"Próba połączenia z serwerem {_host}:{_port}...");
                
                _client = new TcpClient();
                await _client.ConnectAsync(_host, _port);
                _stream = _client.GetStream();
                _isConnected = true;
                
                Debug.WriteLine($"Połączono z serwerem {_host}:{_port}");
                ConnectionStatusChanged?.Invoke(this, new ConnectionEventArgs 
                { 
                    IsConnected = true, 
                    Message = $"Connected to {_host}:{_port}" 
                });

                // Start reading messages
                _ = Task.Run(() => ReadMessagesAsync(_cts.Token), _cts.Token);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception w ConnectAsync: {ex.Message}\nStackTrace: {ex.StackTrace}");
                ConnectionStatusChanged?.Invoke(this, new ConnectionEventArgs 
                { 
                    IsConnected = false, 
                    Message = $"Connection error: {ex.Message}" 
                });
                throw;
            }
        }

        private async Task ReadMessagesAsync(CancellationToken token)
        {
            try
            {
                using (var reader = new BinaryReader(_stream, Encoding.ASCII, leaveOpen: true))
                {
                    while (!token.IsCancellationRequested && _client.Connected)
                    {
                        try
                        {
                            // Read header (16 bytes)
                            byte[] sessionBytes = reader.ReadBytes(10);
                            if (sessionBytes.Length < 10) break;
                            
                            string session = Encoding.ASCII.GetString(sessionBytes).TrimEnd('\0');
                            uint sequence = reader.ReadUInt32();
                            ushort blockCount = reader.ReadUInt16();

                            Debug.WriteLine($"Received message: Session={session}, Seq={sequence}, BlockCount={blockCount}");

                            if (blockCount == 0)
                            {
                                Debug.WriteLine($"Heartbeat: Session={session}, Seq={sequence}");
                                continue;
                            }

                            for (int i = 0; i < blockCount; i++)
                            {
                                ushort blockLength = reader.ReadUInt16();
                                byte[] payload = reader.ReadBytes(blockLength);
                                if (payload.Length == 0) continue;
                                
                                char messageType = (char)payload[0];
                                Debug.WriteLine($"Block {i}: Type={messageType}, Length={blockLength}");
                                
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
                                    case 'r':
                                        OnRewindCompleted();
                                        break;
                                    default: 
                                        Debug.WriteLine($"Unknown message type: {messageType}"); 
                                        break;
                                }
                            }
                        }
                        catch (EndOfStreamException)
                        {
                            Debug.WriteLine("End of stream reached");
                            break;
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Error during message reading: {ex.Message}");
                            if (!_client.Connected) break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fatal error in read loop: {ex.Message}\nStackTrace: {ex.StackTrace}");
            }
            finally
            {
                _isConnected = false;
                ConnectionStatusChanged?.Invoke(this, new ConnectionEventArgs 
                { 
                    IsConnected = false, 
                    Message = "Connection closed" 
                });
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
                
                Debug.WriteLine($"Position: ISIN={isin}, Net={net}, Long={openLong}, Short={openShort}");
                
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
                Debug.WriteLine($"Error processing position message: {ex.Message}");
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
                
                Debug.WriteLine($"Capital: Open={openCapital}, Accrued={accruedCapital}, Total={totalCapital}");
                
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
                Debug.WriteLine($"Error processing capital message: {ex.Message}");
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
                
                Debug.WriteLine($"Log [{level}]: {message}");
                
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
                Debug.WriteLine($"Error processing log message: {ex.Message}");
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
                Debug.WriteLine($"IO: {messageStr}");
                
                IOBytesReceived?.Invoke(this, message);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error processing IO bytes message: {ex.Message}");
            }
        }

        private void OnRewindCompleted()
        {
            Debug.WriteLine("Rewind completed");
            RewindCompleted?.Invoke(this, "Rewind completed");
        }

        public async Task SendControlAsync(Control control)
        {
            if (_client == null || !_client.Connected)
                throw new InvalidOperationException("Not connected to server");

            var controlMessage = BuildControlMessage(control);
            await _stream.WriteAsync(controlMessage, 0, controlMessage.Length);
        }

        public async Task SendGetControlsHistoryAsync()
        {
            if (_client == null || !_client.Connected)
                throw new InvalidOperationException("Not connected to server");

            var message = BuildGetControlsHistoryMessage();
            await _stream.WriteAsync(message, 0, message.Length);
        }

        public async Task SendShutdownAsync()
        {
            if (_client == null || !_client.Connected)
                throw new InvalidOperationException("Not connected to server");

            var message = BuildShutdownMessage();
            await _stream.WriteAsync(message, 0, message.Length);
        }

        public async Task SendRewindAsync(uint lastSeenSequence = 0)
        {
            if (_client == null || !_client.Connected)
                throw new InvalidOperationException("Not connected to server");

            var message = BuildRewindMessage(lastSeenSequence);
            await _stream.WriteAsync(message, 0, message.Length);
            Debug.WriteLine($"Sent rewind request with lastSeenSequence={lastSeenSequence}");
        }

        private byte[] BuildControlMessage(Control control)
        {
            // Build a Set Control (S) message
            string controlString = control.ToString();
            byte[] controlBytes = Encoding.UTF8.GetBytes(controlString);

            // Calculate total message size
            int totalSize = 16 + 2 + 1 + controlBytes.Length;
            byte[] message = new byte[totalSize];

            // Header
            Encoding.ASCII.GetBytes("SESSION   ", 0, 10, message, 0);
            BitConverter.GetBytes((uint)0).CopyTo(message, 10);  // Unsequenced message
            BitConverter.GetBytes((ushort)1).CopyTo(message, 14); // 1 block

            // Block header
            BitConverter.GetBytes((ushort)(1 + controlBytes.Length)).CopyTo(message, 16);

            // Message type
            message[18] = (byte)'S';

            // Control string
            Array.Copy(controlBytes, 0, message, 19, controlBytes.Length);

            return message;
        }

        private byte[] BuildGetControlsHistoryMessage()
        {
            // Build a Get Controls History (G) message
            // Header + block header + message type
            byte[] message = new byte[16 + 2 + 1];

            // Header
            Encoding.ASCII.GetBytes("SESSION   ", 0, 10, message, 0);
            BitConverter.GetBytes((uint)0).CopyTo(message, 10);  // Unsequenced message
            BitConverter.GetBytes((ushort)1).CopyTo(message, 14); // 1 block

            // Block header
            BitConverter.GetBytes((ushort)1).CopyTo(message, 16);

            // Message type
            message[18] = (byte)'G';

            return message;
        }

        private byte[] BuildShutdownMessage()
        {
            // Build a Shutdown (s) message
            byte[] message = new byte[16 + 2 + 1];

            // Header
            Encoding.ASCII.GetBytes("SESSION   ", 0, 10, message, 0);
            BitConverter.GetBytes((uint)0).CopyTo(message, 10);  // Unsequenced message
            BitConverter.GetBytes((ushort)1).CopyTo(message, 14); // 1 block

            // Block header
            BitConverter.GetBytes((ushort)1).CopyTo(message, 16);

            // Message type
            message[18] = (byte)'s';

            return message;
        }

        private byte[] BuildRewindMessage(uint lastSeenSequence)
        {
            // Build a Rewind (R) message
            byte[] message = new byte[16 + 2 + 1 + 4];

            // Header
            Encoding.ASCII.GetBytes("SESSION   ", 0, 10, message, 0);
            BitConverter.GetBytes((uint)0).CopyTo(message, 10);  // Unsequenced message
            BitConverter.GetBytes((ushort)1).CopyTo(message, 14); // 1 block

            // Block header
            BitConverter.GetBytes((ushort)(1 + 4)).CopyTo(message, 16);

            // Message type
            message[18] = (byte)'R';

            // Last seen sequence
            BitConverter.GetBytes(lastSeenSequence).CopyTo(message, 19);

            return message;
        }

        public void Disconnect()
        {
            try
            {
                _cts.Cancel();
                _stream?.Close();
                _client?.Close();
                _isConnected = false;
                
                Debug.WriteLine("Disconnected from server");
                ConnectionStatusChanged?.Invoke(this, new ConnectionEventArgs 
                { 
                    IsConnected = false, 
                    Message = "Disconnected" 
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during disconnect: {ex.Message}");
            }
        }
    }
}