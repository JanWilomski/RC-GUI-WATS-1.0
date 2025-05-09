using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using RiskCheckerGUI.Models;

namespace RiskCheckerGUI.Services
{
    public class UdpService
    {
        private UdpClient _client;
        private readonly string _multicastGroup;
        private readonly int _port;
        private bool _isRunning;

        public event EventHandler<LogMessage> LogReceived;
        public event EventHandler<Position> PositionReceived;
        public event EventHandler<Capital> CapitalReceived;
        public event EventHandler<byte[]> IOBytesReceived;

        public UdpService(string multicastGroup, int port)
        {
            _multicastGroup = multicastGroup;
            _port = port;
        }

        public void Start()
        {
            if (_isRunning)
                return;

            _client = new UdpClient();
            _client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _client.Client.Bind(new IPEndPoint(IPAddress.Any, _port));
            _client.JoinMulticastGroup(IPAddress.Parse(_multicastGroup));

            _isRunning = true;
            _ = Task.Run(ReceiveMessagesAsync);
        }

        private async Task ReceiveMessagesAsync()
        {
            while (_isRunning)
            {
                try
                {
                    UdpReceiveResult result = await _client.ReceiveAsync();
                    ProcessMessage(result.Buffer);
                }
                catch (Exception ex)
                {
                    // Handle exception (log, retry, etc.)
                    Console.WriteLine($"Error receiving UDP message: {ex.Message}");
                }
            }
        }

        private void ProcessMessage(byte[] buffer)
        {
            // Similar to TcpService.ProcessMessage, but for UDP
            // The message format is the same as in TCP
            if (buffer.Length < 16) // At least a header
                return;

            // Parse header
            string session = Encoding.ASCII.GetString(buffer, 0, 10);
            uint sequenceNumber = BitConverter.ToUInt32(buffer, 10);
            ushort blockCount = BitConverter.ToUInt16(buffer, 14);

            int offset = 16; // Start of blocks

            for (int i = 0; i < blockCount && offset < buffer.Length; i++)
            {
                // Parse block header
                ushort length = BitConverter.ToUInt16(buffer, offset);
                offset += 2;

                // Make sure there's enough data
                if (offset + 1 > buffer.Length)
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
                    // Handle other message types as needed
                }

                // Move to the next block
                offset += length - 1; // -1 because we already incremented for the message type
            }
        }

        // The processing methods are the same as in TcpService
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

        public void Stop()
        {
            _isRunning = false;
            if (_client != null)
            {
                _client.DropMulticastGroup(IPAddress.Parse(_multicastGroup));
                _client.Close();
                _client = null;
            }
        }
    }
}
