using System;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace IGRF.Avalonia.ViewModels
{
    /// <summary>
    /// MainViewModel - Logging Logic
    /// Contains logging control, zeroing, and log status
    /// </summary>
    public partial class MainViewModel
    {
        // --- Logging State ---
        [ObservableProperty] private bool _isLogging;
        [ObservableProperty] private string _logStatus = "Ready";
        [ObservableProperty] private int _logCountDisplay;
        
        private string _logFileName = "";
        private int _logCount = 0;

        [RelayCommand]
        public void ToggleLogging()
        {
            if (IsLogging)
            {
                IsLogging = false;
                LogStatus = $"Logging stopped. Total: {_logCount} rows.";
            }
            else
            {
                _logFileName = $"sensor_log_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                
                try
                {
                    string header = "Timestamp,MagX,MagY,MagZ,SetX,SetY,SetZ,OutX,OutY,OutZ,Kp,Ki,Kd\n";
                    File.WriteAllText(_logFileName, header);
                    
                    _logCount = 0;
                    LogCountDisplay = 0;
                    IsLogging = true;
                    LogStatus = $"Logging to: {_logFileName}";
                }
                catch (Exception ex)
                {
                    LogStatus = $"Failed to start logging: {ex.Message}";
                }
            }
        }

        [RelayCommand]
        public void ZerorizeSensor()
        {
            _sensorService.SetZero(MagX, MagY, MagZ);
            LogStatus = $"Sensor zeroed at X:{MagX:F2}, Y:{MagY:F2}, Z:{MagZ:F2}";
        }
    }
}
