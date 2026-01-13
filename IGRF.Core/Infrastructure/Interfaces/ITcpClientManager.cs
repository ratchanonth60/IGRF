#nullable enable

using System;
using System.Threading.Tasks;

namespace IGRF_Interface.Infrastructure.Interfaces
{
    /// <summary>
    /// Interface for TCP client communication with MFG sensors
    /// </summary>
    public interface ITcpClientManager
    {
        /// <summary>
        /// Is currently connected
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Event for received data packets
        /// </summary>
        event Action<byte[]>? OnDataReceived;

        /// <summary>
        /// Event for connection state changes
        /// </summary>
        event Action<bool>? OnConnectionChanged;

        /// <summary>
        /// Connect to MFG sensor via TCP
        /// </summary>
        Task<bool> ConnectAsync(string ipAddress, int port);

        /// <summary>
        /// Disconnect from MFG sensor
        /// </summary>
        void Disconnect();

        /// <summary>
        /// Send command to sensor
        /// </summary>
        Task<bool> SendCommandAsync(string command);

        /// <summary>
        /// Set sampling rate (0=100Hz, 1=50Hz, 2=10Hz, 3=1Hz)
        /// </summary>
        Task<bool> SetSamplingRateAsync(int rateCode);
    }
}
