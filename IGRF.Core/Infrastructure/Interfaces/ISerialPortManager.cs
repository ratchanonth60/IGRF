#nullable enable

using System;

namespace IGRF_Interface.Infrastructure.Interfaces
{
    /// <summary>
    /// Interface for serial port communication
    /// </summary>
    public interface ISerialPortManager
    {
        /// <summary>
        /// Is port currently open
        /// </summary>
        bool IsOpen { get; }

        /// <summary>
        /// Is sensor ready (received OK)
        /// </summary>
        bool IsSensorReady { get; }

        /// <summary>
        /// Event for received data packets
        /// </summary>
        event Action<byte[]>? OnPacketReceived;

        /// <summary>
        /// Event for errors
        /// </summary>
        event Action<string>? OnError;

        /// <summary>
        /// Connect to serial port
        /// </summary>
        bool Connect(string portName, int baudRate = 9600);

        /// <summary>
        /// Write data to port
        /// </summary>
        void Write(byte[] data);

        /// <summary>
        /// Disconnect from port
        /// </summary>
        void Disconnect();
    }
}
