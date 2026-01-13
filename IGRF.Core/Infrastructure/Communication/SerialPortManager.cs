#nullable enable

using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using IGRF_Interface.Infrastructure.Interfaces;

namespace IGRF_Interface.Infrastructure.Communication
{
    public class SerialPortManager : ISerialPortManager
    {
        private SerialPort? _port;
        private readonly List<byte> _buffer = [];

        private readonly Lock _bufferLock = new();

        public bool IsSensorReady { get; private set; }
        public event Action<byte[]>? OnPacketReceived;
        public event Action<string>? OnError;
        public bool IsOpen => _port != null && _port.IsOpen;
        private int WriteTimeout { get; set; } = 500;
        private int ReadTimeout { get; set; } = 500;

        public bool Connect(string portName, int baudRate = 9600)
        {
            Disconnect();
            try
            {
                _port = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One)
                {
                    Handshake = Handshake.None,
                    DtrEnable = true,
                    WriteTimeout = WriteTimeout,
                    ReadTimeout = ReadTimeout,
                };

                _port.DataReceived += DataReceivedHandler;
                _port.Open();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw new Exception(portName, ex);
            }
        }

        public void Write(byte[] data)
        {
            try
            {
                if (IsOpen && data != null && _port!.BaseStream.CanWrite)
                {
                    _port.Write(data, 0, data.Length);
                }
            }
            catch (TimeoutException ex)
            {
                OnError?.Invoke($"Write timeout: {ex.Message}");
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"Write error: {ex.Message}");
            }
        }

        public void Disconnect()
        {
            if (_port != null)
            {
                if (_port.IsOpen)
                {
                    _port.DataReceived -= DataReceivedHandler;
                    try
                    {
                        _port.Close();
                    }
                    catch (Exception ex)
                    {
                        OnError?.Invoke($"Disconnect error: {ex.Message}");
                    }
                }
                _port.Dispose();
                _port = null;
            }

            lock (_bufferLock)
            {
                IsSensorReady = false;
                _buffer.Clear();
            }
        }

        private void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            if (_port == null || !_port.IsOpen)
                return;

            try
            {
                int bytesToRead = _port.BytesToRead;
                if (bytesToRead == 0)
                    return;

                byte[] temp = new byte[bytesToRead];
                _port.Read(temp, 0, bytesToRead);

                lock (_bufferLock)
                {
                    _buffer.AddRange(temp);

                    if (_buffer.Count > 1000)
                        _buffer.RemoveRange(0, _buffer.Count - 200);

                    if (!IsSensorReady)
                    {
                        for (int i = 0; i < _buffer.Count - 1; i++)
                        {
                            if (_buffer[i] == 0x4F && _buffer[i + 1] == 0x4B)
                            {
                                IsSensorReady = true;
                                _buffer.RemoveRange(0, i + 2);
                                break;
                            }
                        }
                        return;
                    }

                    while (_buffer.Count >= 7)
                    {
                        int terminatorIndex = _buffer.IndexOf(13); // 0x0D

                        if (terminatorIndex != -1)
                        {
                            if (terminatorIndex < 6)
                            {
                                _buffer.RemoveRange(0, terminatorIndex + 1);
                                continue;
                            }

                            byte[] packet = _buffer.GetRange(terminatorIndex - 6, 7).ToArray();

                            OnPacketReceived?.Invoke(packet);

                            _buffer.RemoveRange(0, terminatorIndex + 1);
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"Data receive error: {ex.Message}");
            }
        }
    }
}
