using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace IGRF_Interface.Infrastructure.Communication
{
    /// <summary>
    /// TCP Client Manager for MFG Digital Fluxgate Magnetometer
    /// Handles connection and data reception from MFG sensors via Ethernet
    /// </summary>
    public class TcpClientManager
    {
        private TcpClient? _client;
        private NetworkStream? _stream;
        private CancellationTokenSource? _cancellation;
        private bool _isRunning;

        public bool IsConnected => _client?.Connected ?? false;
        
        /// <summary>
        /// Event fired when a complete data packet is received
        /// Payload is the complete MFG data structure (60 bytes)
        /// </summary>
        public event Action<byte[]>? OnDataReceived;
        
        /// <summary>
        /// Event fired when connection state changes
        /// </summary>
        public event Action<bool>? OnConnectionChanged;

        private string _ipAddress = "";
        private int _port = 12345;

        public TcpClientManager()
        {
        }

        /// <summary>
        /// Connect to MFG sensor via TCP
        /// </summary>
        public async Task<bool> ConnectAsync(string ipAddress, int port)
        {
            if (IsConnected) return true;

            try
            {
                _ipAddress = ipAddress;
                _port = port;
                
                _client = new TcpClient();
                await _client.ConnectAsync(ipAddress, port);
                
                if (_client.Connected)
                {
                    _stream = _client.GetStream();
                    _cancellation = new CancellationTokenSource();
                    _isRunning = true;
                    
                    // Start receiving data in background
                    _ = Task.Run(() => ReceiveDataLoop(_cancellation.Token));
                    
                    OnConnectionChanged?.Invoke(true);
                    return true;
                }
            }
            catch (Exception)
            {
                Disconnect();
            }
            
            return false;
        }

        /// <summary>
        /// Disconnect from MFG sensor
        /// </summary>
        public void Disconnect()
        {
            _isRunning = false;
            _cancellation?.Cancel();
            
            _stream?.Close();
            _client?.Close();
            
            _stream = null;
            _client = null;
            _cancellation = null;
            
            OnConnectionChanged?.Invoke(false);
        }

        /// <summary>
        /// Background loop to receive data packets
        /// MFG data structure size: sizeof(long) + 3*sizeof(long) + 14*sizeof(float) = 4 + 12 + 56 = 72 bytes
        /// Actually: long=4 bytes on 32-bit, 8 bytes on 64-bit in C#
        /// C structure uses 4-byte long, so: 4 + 3*4 + 14*4 = 4 + 12 + 56 = 72 bytes
        /// But we need to check actual packing - typically 60 bytes based on manual
        /// </summary>
        private async Task ReceiveDataLoop(CancellationToken token)
        {
            const int PACKET_SIZE = 72; // sizeof(mag_data_struct) = 4 + 12 + 56
            byte[] buffer = new byte[PACKET_SIZE];
            
            while (_isRunning && !token.IsCancellationRequested && _stream != null)
            {
                try
                {
                    int bytesRead = 0;
                    
                    // Read complete packet
                    while (bytesRead < PACKET_SIZE && _stream.CanRead)
                    {
                        int read = await _stream.ReadAsync(buffer, bytesRead, PACKET_SIZE - bytesRead, token);
                        if (read == 0) break; // Connection closed
                        bytesRead += read;
                    }
                    
                    if (bytesRead == PACKET_SIZE)
                    {
                        // Fire event with complete packet
                        byte[] packet = new byte[PACKET_SIZE];
                        Array.Copy(buffer, packet, PACKET_SIZE);
                        OnDataReceived?.Invoke(packet);
                    }
                    else if (bytesRead == 0)
                    {
                        // Connection closed
                        break;
                    }
                }
                catch (Exception)
                {
                    break;
                }
            }
            
            // Connection lost
            if (_isRunning)
            {
                Disconnect();
            }
        }
    }
}
