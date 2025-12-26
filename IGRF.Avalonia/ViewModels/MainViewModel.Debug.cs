using System;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Threading;

namespace IGRF.Avalonia.ViewModels
{
    /// <summary>
    /// MainViewModel - Debug Logic
    /// Contains debug logging, raw data display, and custom command sending
    /// </summary>
    public partial class MainViewModel
    {
        // --- Debug Log ---
        private readonly StringBuilder _debugLogBuilder = new StringBuilder();
        private const int MAX_LOG_LENGTH = 50000; // Limit log size
        
        [ObservableProperty] private string _debugLogText = "Debug console initialized...\n";
        [ObservableProperty] private bool _debugAutoScroll = true;
        [ObservableProperty] private string _lastRawPacketHex = "(No packets received)";
        [ObservableProperty] private string _customCommand = "";
        [ObservableProperty] private string _mfgConnectionStatus = "Not Connected";
        
        /// <summary>
        /// Add a line to the debug log
        /// </summary>
        public void DebugLog(string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            var logLine = $"[{timestamp}] {message}\n";
            
            Dispatcher.UIThread.Post(() =>
            {
                _debugLogBuilder.Append(logLine);
                
                // Trim if too long
                if (_debugLogBuilder.Length > MAX_LOG_LENGTH)
                {
                    _debugLogBuilder.Remove(0, _debugLogBuilder.Length - MAX_LOG_LENGTH / 2);
                }
                
                DebugLogText = _debugLogBuilder.ToString();
            });
        }
        
        /// <summary>
        /// Update raw packet display (hex format)
        /// </summary>
        public void UpdateRawPacketDisplay(byte[] packet)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < packet.Length; i++)
            {
                sb.AppendFormat("{0:X2} ", packet[i]);
                if ((i + 1) % 16 == 0)
                {
                    sb.AppendLine();
                }
            }
            LastRawPacketHex = sb.ToString().TrimEnd();
        }
        
        [RelayCommand]
        private void ClearDebugLog()
        {
            _debugLogBuilder.Clear();
            DebugLogText = "Log cleared.\n";
        }
        
        [RelayCommand]
        private async System.Threading.Tasks.Task SendCustomCommand()
        {
            if (string.IsNullOrWhiteSpace(CustomCommand))
            {
                DebugLog("ERROR: No command entered");
                return;
            }
            
            if (_tcpManager == null || !_tcpManager.IsConnected)
            {
                DebugLog("ERROR: Not connected to MFG sensor");
                return;
            }
            
            DebugLog($"Sending command: {CustomCommand}");
            bool success = await _tcpManager.SendCommandAsync(CustomCommand);
            
            if (success)
            {
                DebugLog("Command sent successfully");
            }
            else
            {
                DebugLog("Failed to send command");
            }
        }
        
        /// <summary>
        /// Update MFG connection status for debug display
        /// </summary>
        private void UpdateMfgConnectionStatus()
        {
            if (_tcpManager != null && _tcpManager.IsConnected)
            {
                MfgConnectionStatus = $"Connected to {_mfgIpAddress}:{_mfgPort}";
            }
            else
            {
                MfgConnectionStatus = "Not Connected";
            }
        }
    }
}
