using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using RiskCheckerGUI.Models;

namespace RiskCheckerGUI.Services
{
    public class TcpService
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private string _host;
        private int _port;

        public event EventHandler<LogMessage> LogReceived;
        public event EventHandler<Position> PositionReceived;
        public event EventHandler<Capital> CapitalReceived;
        public event EventHandler<byte[]> IOBytesReceived;
        public event EventHandler<string> RewindCompleted;

        public TcpService(string host, int port)
        {
            _host = host;
            _port = port;
        }

        public async Task ConnectAsync()
        {
            _client = new TcpClient();
            await _client.ConnectAsync(_host, _port);
            _stream = _client.GetStream();

            // Start listening for messages
            _ = Task.Run(ReceiveMessagesAsync);
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
        }

        private async Task ReceiveMessagesAsync()
        {
            var buffer = new byte[4096];
            while (_client.Connected)
            {
                try
                {
                    int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                        break;

                    ProcessMessage(buffer, bytesRead);
                }
                catch (Exception ex)
                {
                    // Handle exception (log, retry connection, etc.)
                    Console.WriteLine($"Error receiving message: {ex.Message}");
                    break;
                }
            }
        }

        private void ProcessMessage(byte[] buffer, int bytesRead)
        {
            // Implement message parsing based on the protocol specification
            // This is a simplified example - you'll need to handle the full protocol
            if (bytesRead < 16) // At least a header
                return;

            // Parse header
            string session = Encoding.ASCII.GetString(buffer, 0, 10);
            uint sequenceNumber = BitConverter.ToUInt32(buffer, 10);
            ushort blockCount = BitConverter.ToUInt16(buffer, 14);

            int offset = 16; // Start of blocks

            for (int i = 0; i < blockCount && offset < bytesRead; i++)
            {
                // Parse block header
                ushort length = BitConverter.ToUInt16(buffer, offset);
                offset += 2;

                // Make sure there's enough data
                if (offset + 1 > bytesRead)
                    break;

                char messageType = (char)buffer[offset];
                offset += 1;

                switch (messageType)
                {
                    case 'D': // Debug log
                    case 'I': // Info log
                    case 'W': // Warning log
                    case 'E': // Error log
                        ProcessLogMessage(buffer, ref offset, messageType);
                        break;
                    case 'P': // Position
                        ProcessPositionMessage(buffer, ref offset);
                        break;
                    case 'C': // Capital
                        ProcessCapitalMessage(buffer, ref offset);
                        break;
                    case 'B': // I/O bytes
                        ProcessIOBytesMessage(buffer, ref offset);
                        break;
                    case 'r': // Rewind complete
                        OnRewindCompleted();
                        break;
                    // Handle other message types as needed
                }

                // Move to the next block
                offset += length - 1; // -1 because we already incremented for the message type
            }
        }

        private void ProcessLogMessage(byte[] buffer, ref int offset, char messageType)
        {
            ushort length = BitConverter.ToUInt16(buffer, offset);
            offset += 2;

            string message = Encoding.UTF8.GetString(buffer, offset, length);

            LogReceived?.Invoke(this, new LogMessage
            {
                Type = (LogType)messageType,
                Message = message
            });
        }

        private void ProcessPositionMessage(byte[] buffer, ref int offset)
        {
            // Position message is 25 bytes total
            if (offset + 24 > buffer.Length)
                return;

            string isin = Encoding.ASCII.GetString(buffer, offset, 12);
            offset += 12;

            int net = BitConverter.ToInt32(buffer, offset);
            offset += 4;

            int openLong = BitConverter.ToInt32(buffer, offset);
            offset += 4;

            int openShort = BitConverter.ToInt32(buffer, offset);
            offset += 4;

            PositionReceived?.Invoke(this, new Position
            {
                ISIN = isin.TrimEnd('\0'),
                Net = net,
                OpenLong = openLong,
                OpenShort = openShort
            });
        }

        private void ProcessCapitalMessage(byte[] buffer, ref int offset)
        {
            // Capital message is 25 bytes total
            if (offset + 24 > buffer.Length)
                return;

            double openCapital = BitConverter.ToDouble(buffer, offset);
            offset += 8;

            double accruedCapital = BitConverter.ToDouble(buffer, offset);
            offset += 8;

            double totalCapital = BitConverter.ToDouble(buffer, offset);
            offset += 8;

            CapitalReceived?.Invoke(this, new Capital
            {
                OpenCapital = openCapital,
                AccruedCapital = accruedCapital,
                TotalCapital = totalCapital
            });
        }

        private void ProcessIOBytesMessage(byte[] buffer, ref int offset)
        {
            ushort length = BitConverter.ToUInt16(buffer, offset);
            offset += 2;

            byte[] message = new byte[length];
            Array.Copy(buffer, offset, message, 0, length);

            IOBytesReceived?.Invoke(this, message);
        }

        private void OnRewindCompleted()
        {
            RewindCompleted?.Invoke(this, "Rewind completed");
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
            _stream?.Close();
            _client?.Close();
        }

        public void UpdateConnection(string host, int port)
        {
            if (_client != null && _client.Connected)
            {
                Disconnect();
            }
            
            _host = host ?? throw new ArgumentNullException(nameof(host));
            _port = port;
        }




    }


}