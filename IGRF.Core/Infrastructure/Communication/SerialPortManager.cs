using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;

namespace IGRF_Interface.Infrastructure.Communication
{
    public class SerialPortManager
    {
        private SerialPort _port;
        private List<byte> _buffer = new List<byte>();

        // **????? 1: ?????????????? (Lock Object) ????????????????????????????**
        private readonly object _bufferLock = new object();

        public bool IsSensorReady { get; private set; } = false;
        public event Action<byte[]> OnPacketReceived;
        public event Action<string> OnError; // Error event for UI notification
        public bool IsOpen => _port != null && _port.IsOpen;
        private int writeTimeout { get; set; } = 500;
        private int readTimeout { get; set; } = 500;
        public bool Connect(string portName, int baudRate = 9600)
        {
            Disconnect();
            try
            {
                _port = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One);
                _port.Handshake = Handshake.None;
                _port.DtrEnable = true;

                // *** ????? Timeout ??????????? ***
                _port.WriteTimeout = this.writeTimeout; // ???????????????? 0.5 ?? ???????????
                _port.ReadTimeout = this.readTimeout;

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
                // ?????????? Port ??????????????????????
                if (IsOpen && data != null && _port.BaseStream.CanWrite)
                {
                    _port.Write(data, 0, data.Length);
                }
            }
            catch (TimeoutException ex)
            {
                // ???????????? (Timeout) ???????????? ????????????????? Error
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

            // ?????????????????? ???????????????? DataReceived
            lock (_bufferLock)
            {
                IsSensorReady = false;
                _buffer.Clear();
            }
        }



        private void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            if (_port == null || !_port.IsOpen) return;

            try
            {
                int bytesToRead = _port.BytesToRead;
                if (bytesToRead == 0) return;

                byte[] temp = new byte[bytesToRead];
                _port.Read(temp, 0, bytesToRead);

                // **????? 2: ??? lock (...) ???????????????????????? _buffer ???????**
                // ??????????????????????????????????
                lock (_bufferLock)
                {
                    _buffer.AddRange(temp);

                    // ??????? Buffer ???????? Ram (??????????????????? ??????????)
                    if (_buffer.Count > 1000) _buffer.RemoveRange(0, _buffer.Count - 200);

                    // Logic 1: ??????? Handshake "OK"
                    if (!IsSensorReady)
                    {
                        // ????????? 0x4F, 0x4B ?????????
                        for (int i = 0; i < _buffer.Count - 1; i++)
                        {
                            if (_buffer[i] == 0x4F && _buffer[i + 1] == 0x4B)
                            {
                                IsSensorReady = true;
                                _buffer.RemoveRange(0, i + 2); // ?? OK ???
                                break;
                            }
                        }
                        // ???????????? OK ?????????? ????????????
                        return;
                    }

                    // Logic 2: ??? Packet ?????? (Data 6 bytes + CR 1 byte = 7 bytes)
                    // ??? while loop ?????????? ??????????
                    while (_buffer.Count >= 7)
                    {
                        int terminatorIndex = _buffer.IndexOf(13); // 0x0D

                        if (terminatorIndex != -1)
                        {
                            // ???????????? ????????????????????????? 6 ??? ?????????????? ?????????
                            if (terminatorIndex < 6)
                            {
                                _buffer.RemoveRange(0, terminatorIndex + 1);
                                continue; // ?????????
                            }

                            // ????????????? ??????????????
                            byte[] packet = _buffer.GetRange(terminatorIndex - 6, 7).ToArray();

                            // ?????????????? (?????????????????)
                            OnPacketReceived?.Invoke(packet);

                            // ???????????????????????? Buffer
                            _buffer.RemoveRange(0, terminatorIndex + 1);
                        }
                        else
                        {
                            // ???????????? Loop ????????????????????? break
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