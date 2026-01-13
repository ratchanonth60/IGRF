using System;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace IGRF.Avalonia.ViewModels
{
    /// <summary>
    /// MainViewModel - Sensor Info Logic
    /// Contains temperature, GPS, and status data from MFG sensors
    /// </summary>
    public partial class MainViewModel
    {
        // --- Temperature Properties ---
        [ObservableProperty] private double _sensorTemp1;
        [ObservableProperty] private double _electronicsTemp;
        [ObservableProperty] private double _sensorTemp2;
        
        // --- GPS Properties ---
        [ObservableProperty] private double _gpsLatitude;
        [ObservableProperty] private double _gpsLongitude;
        [ObservableProperty] private string _gpsStatus = "No GPS";
        [ObservableProperty] private SolidColorBrush _gpsStatusColor = new SolidColorBrush(Color.FromRgb(80, 80, 80));
        
        // --- Status Properties ---
        [ObservableProperty] private int _statusWord;
        [ObservableProperty] private string _lastUpdateTime = "--:--:--";
        [ObservableProperty] private double _dataRate;
        [ObservableProperty] private long _packetCount;
        [ObservableProperty] private string _sensorMode = "Idle";
        
        // --- Dual Sensor Detection ---
        public bool IsDualSensor => SelectedSensorType?.Type == IGRF_Interface.Core.Models.SensorType.MFG_2S_LC;
        
        // --- Data Rate Calculation ---
        private DateTime _lastPacketTime = DateTime.MinValue;
        private int _packetsInSecond = 0;
        private DateTime _rateCalculationTime = DateTime.Now;
        
        private void UpdateSensorInfo(IGRF_Interface.Infrastructure.Utilities.MfgDataParser.MagDataStruct data)
        {
            // Update Timestamp
            var timestamp = DateTimeOffset.FromUnixTimeSeconds(data.L[0]).LocalDateTime;
            LastUpdateTime = timestamp.ToString("HH:mm:ss.fff");
            
            // Update Status Word
            StatusWord = data.L[1];
            
            // Update Temperatures (from TYPE_DAT)
            if (data.DataType == IGRF_Interface.Infrastructure.Utilities.MfgDataParser.TYPE_DAT)
            {
                SensorTemp1 = data.F[0];
                ElectronicsTemp = data.F[1];
                SensorTemp2 = data.F[7];
            }
            
            // Update Packet Count
            PacketCount++;
            
            // Calculate Data Rate
            _packetsInSecond++;
            var now = DateTime.Now;
            if ((now - _rateCalculationTime).TotalSeconds >= 1.0)
            {
                DataRate = _packetsInSecond / (now - _rateCalculationTime).TotalSeconds;
                _packetsInSecond = 0;
                _rateCalculationTime = now;
            }
            
            // Update Sensor Mode
            if (SensorStatus == "ON")
            {
                SensorMode = IsDualSensor ? "Dual Axis" : "Single Axis";
            }
            else
            {
                SensorMode = "Idle";
            }
        }
        
        private void UpdateGpsPosition(IGRF_Interface.Infrastructure.Utilities.MfgDataParser.MagDataStruct data)
        {
            // GPS data comes from TYPE_POS packets
            if (data.DataType == IGRF_Interface.Infrastructure.Utilities.MfgDataParser.TYPE_POS)
            {
                GpsLatitude = data.F[0];
                GpsLongitude = data.F[1];
                GpsStatus = "GPS Lock";
                GpsStatusColor = new SolidColorBrush(Color.FromRgb(0, 180, 100)); // Green
            }
        }
        
        private void UpdateGpsStatus(bool hasGps)
        {
            if (hasGps)
            {
                GpsStatus = "GPS Lock";
                GpsStatusColor = new SolidColorBrush(Color.FromRgb(0, 180, 100)); // Green
            }
            else
            {
                GpsStatus = "No GPS";
                GpsStatusColor = new SolidColorBrush(Color.FromRgb(80, 80, 80)); // Gray
            }
        }
    }
}
