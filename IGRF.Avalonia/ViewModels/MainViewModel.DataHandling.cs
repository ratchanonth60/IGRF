using System;
using System.IO;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using IGRF_Interface.Infrastructure.Utilities;
using IGRF_Interface.Core.Models;

namespace IGRF.Avalonia.ViewModels
{
    /// <summary>
    /// MainViewModel - Data Handling Logic
    /// Contains sensor data processing, MFG packet handling, and data events
    /// </summary>
    public partial class MainViewModel
    {
        // --- Sensor 1 Data ---
        [ObservableProperty] private double _magX;
        [ObservableProperty] private double _magY;
        [ObservableProperty] private double _magZ;
        [ObservableProperty] private double _errorX;
        [ObservableProperty] private double _errorY;
        [ObservableProperty] private double _errorZ;
        [ObservableProperty] private double _errorPerX;
        [ObservableProperty] private double _errorPerY;
        [ObservableProperty] private double _errorPerZ;
        
        // --- Sensor 2 Data (Dual Mode) ---
        [ObservableProperty] private double _magX2;
        [ObservableProperty] private double _magY2;
        [ObservableProperty] private double _magZ2;
        [ObservableProperty] private double _errorX2;
        [ObservableProperty] private double _errorY2;
        [ObservableProperty] private double _errorZ2;
        
        // Data event for chart updates - Single sensor
        public event Action<double, double, double>? DataUpdated;
        
        // Data event for chart updates - Dual sensor (error1X, error1Y, error1Z, error2X, error2Y, error2Z)
        public event Action<double, double, double, double, double, double>? DualDataUpdated;

        private void HandleSensorPacket(byte[] packet)
        {
            var raw = _sensorService.ProcessData(packet);
            var processed = _calcService.ProcessSensorData(raw, SetpointX, SetpointY, SetpointZ);

            Dispatcher.UIThread.Post(() =>
            {
                MagX = processed.MagX;
                MagY = processed.MagY;
                MagZ = processed.MagZ;
                ErrorX = processed.ErrorX;
                ErrorY = processed.ErrorY;
                ErrorZ = processed.ErrorZ;
                
                ErrorPerX = SetpointX != 0 ? (ErrorX / SetpointX) * 100 : 0;
                ErrorPerY = SetpointY != 0 ? (ErrorY / SetpointY) * 100 : 0;
                ErrorPerZ = SetpointZ != 0 ? (ErrorZ / SetpointZ) * 100 : 0;
                
                DataUpdated?.Invoke(processed.MagX, processed.MagY, processed.MagZ);
            });

            if (IsLogging)
            {
                // Log S1 (Raw is not available here easily as we use processed struct, using processed for S1 for now)
                // Actually, for generic sensor, ProcessData returns raw. 
                LogDataPoint(processed.MagX, processed.MagY, processed.MagZ, 0, 0, 0);
            }
        }
        
        private void HandleMfgDataPacket(byte[] packet)
        {
            System.Diagnostics.Debug.WriteLine($"[MFG] Received packet: {packet.Length} bytes");
            
            var data = MfgDataParser.Parse(packet);
            if (data == null) 
            {
                System.Diagnostics.Debug.WriteLine("[MFG] Parse failed - null result");
                Dispatcher.UIThread.Post(() => LogStatus = "MFG: Parse failed");
                return;
            }
            
            System.Diagnostics.Debug.WriteLine($"[MFG] DataType: {data.Value.DataType}");
            
            if (data.Value.DataType != MfgDataParser.TYPE_DAT) return;
            
            // Sensor 1: f[8], f[9], f[10]
            // Sensor 1: f[8], f[9], f[10]
            var magField1 = new float[] { data.Value.F[8], data.Value.F[9], data.Value.F[10] };
            System.Diagnostics.Debug.WriteLine($"[MFG] Sensor1: X={magField1[0]:F2}, Y={magField1[1]:F2}, Z={magField1[2]:F2}");
            
            var raw1 = _sensorService.ProcessMFGData(magField1);
            var processed1 = _calcService.ProcessSensorData(raw1, SetpointX, SetpointY, SetpointZ);
            
            // Check if dual sensor mode
            bool isDualSensor = SelectedSensorType?.Type == SensorType.MFG_2S_LC;
            
            ProcessedData? processed2 = null;
            float[]? magField2 = null;
            if (isDualSensor)
            {
                // Sensor 2: f[11], f[12], f[13]
                magField2 = new float[] { data.Value.F[11], data.Value.F[12], data.Value.F[13] };
                System.Diagnostics.Debug.WriteLine($"[MFG] Sensor2: X={magField2[0]:F2}, Y={magField2[1]:F2}, Z={magField2[2]:F2}");
                
                var raw2 = _sensorService.ProcessMFGData(magField2);
                processed2 = _calcService.ProcessSensorData(raw2, SetpointX, SetpointY, SetpointZ);
            }

            Dispatcher.UIThread.Post(() =>
            {
                // Update Sensor 1 data - Use RAW for display
                MagX = magField1[0];
                MagY = magField1[1];
                MagZ = magField1[2];
                ErrorX = processed1.ErrorX;
                ErrorY = processed1.ErrorY;
                ErrorZ = processed1.ErrorZ;
                
                ErrorPerX = SetpointX != 0 ? (ErrorX / SetpointX) * 100 : 0;
                ErrorPerY = SetpointY != 0 ? (ErrorY / SetpointY) * 100 : 0;
                ErrorPerZ = SetpointZ != 0 ? (ErrorZ / SetpointZ) * 100 : 0;
                
                if (isDualSensor && processed2 != null)
                {
                    // Update Sensor 2 data - Use RAW for display
                    if (magField2 != null)
                    {
                        MagX2 = magField2[0];
                        MagY2 = magField2[1];
                        MagZ2 = magField2[2];
                    }
                    ErrorX2 = processed2.ErrorX;
                    ErrorY2 = processed2.ErrorY;
                    ErrorZ2 = processed2.ErrorZ;
                    
                    if (magField2 != null)
                    {
                        // Fire dual data event - Plot RAW data directly (no calculation/filtering)
                        DualDataUpdated?.Invoke(
                            magField1[0], magField1[1], magField1[2],
                            magField2[0], magField2[1], magField2[2]);
                    }
                }
                else
                {
                    // Fire single data event
                    // Fire single data event - Plot RAW data directly
                    DataUpdated?.Invoke(magField1[0], magField1[1], magField1[2]);
                }
                
                // Update Sensor Info (Temperature, Status, etc.)
                UpdateSensorInfo(data.Value);
                
                // Check for GPS data in separate packet
                UpdateGpsPosition(data.Value);
            });

            if (IsLogging)
            {
                double x2 = magField2 != null ? magField2[0] : 0;
                double y2 = magField2 != null ? magField2[1] : 0;
                double z2 = magField2 != null ? magField2[2] : 0;
                
                LogDataPoint(magField1[0], magField1[1], magField1[2], x2, y2, z2);
            }
        }
        
        private void LogDataPoint(double magX, double magY, double magZ, double magX2, double magY2, double magZ2)
        {
            try
            {
                string timeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                string dataLine = $"{timeStamp},{magX:F2},{magY:F2},{magZ:F2},{magX2:F2},{magY2:F2},{magZ2:F2}," +
                                  $"{SetpointX:F2},{SetpointY:F2},{SetpointZ:F2}," +
                                  $"{_outputX:F2},{_outputY:F2},{_outputZ:F2}," +
                                  $"{Kp},{Ki},{Kd}\n";

                File.AppendAllText(_logFileName, dataLine);
                _logCount++;
                LogCountDisplay = _logCount;
            }
            catch (Exception ex)
            {
                LogStatus = $"Logging error: {ex.Message}";
                IsLogging = false;
            }
        }

        

    }
}
