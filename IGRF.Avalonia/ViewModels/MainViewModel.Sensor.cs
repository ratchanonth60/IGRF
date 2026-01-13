using System;
using System.IO.Ports;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using Avalonia.Threading;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IGRF_Interface.Core.Models;

namespace IGRF.Avalonia.ViewModels
{
    /// <summary>
    /// MainViewModel - Sensor Connection Logic
    /// Contains sensor type selection, port management, and sensor data handling
    /// </summary>
    public partial class MainViewModel
    {
        // --- Sensor Type Selection ---
        [ObservableProperty] private ObservableCollection<SensorTypeItem> _sensorTypes = new();
        [ObservableProperty] private SensorTypeItem? _selectedSensorType;
        
        // --- MFG TCP Connection ---
        [ObservableProperty] private string _mfgIpAddress = "192.168.124.41";
        [ObservableProperty] private int _mfgPort = 12345;
        
        // Computed property: Is current sensor MFG type?
        public bool IsMfgSensor => SelectedSensorType?.Type == SensorType.MFG_1S_LC || 
                                   SelectedSensorType?.Type == SensorType.MFG_2S_LC;
        
        // Computed property: Is current sensor Generic (Serial)?
        public bool IsSerialSensor => SelectedSensorType?.Type == SensorType.Generic;
        
        // --- Port Selection ---
        [ObservableProperty] private ObservableCollection<string> _portList = new();
        [ObservableProperty] private string? _selectedSensorPort;
        [ObservableProperty] private string? _selectedControllerPort;
        
        // --- Connection Status ---
        [ObservableProperty] private string _sensorStatus = "OFF";
        [ObservableProperty] private string _controllerStatus = "OFF";
        
        public SolidColorBrush SensorColor =>
            SensorStatus == "ON" ? new SolidColorBrush(Color.FromRgb(0, 255, 0)) :
            SensorStatus == "OFF" ? new SolidColorBrush(Color.FromRgb(255, 0, 0)) :
            new SolidColorBrush(Color.FromRgb(255, 165, 0));
            
        public SolidColorBrush ControllerColor =>
            ControllerStatus == "ON" ? new SolidColorBrush(Color.FromRgb(0, 255, 0)) :
            new SolidColorBrush(Color.FromRgb(255, 0, 0));
        
        // --- Sensor Type Item Helper Class ---
        public class SensorTypeItem
        {
            public string Name { get; set; } = "";
            public SensorType Type { get; set; }
        }
        
        private void InitializeSensorTypes()
        {
            SensorTypes.Add(new SensorTypeItem { Name = "Generic Sensor (Serial)", Type = SensorType.Generic });
            SensorTypes.Add(new SensorTypeItem { Name = "MFG-1S-LC (Single Axis)", Type = SensorType.MFG_1S_LC });
            SensorTypes.Add(new SensorTypeItem { Name = "MFG-2S-LC (Dual Axis)", Type = SensorType.MFG_2S_LC });
            SelectedSensorType = SensorTypes[0];
        }
        
        partial void OnSelectedSensorTypeChanged(SensorTypeItem? value)
        {
            if (value == null) return;
            _sensorService.SetSensorType(value.Type);
            OnPropertyChanged(nameof(IsMfgSensor));
            OnPropertyChanged(nameof(IsSerialSensor));
            OnPropertyChanged(nameof(IsDualSensor));
            LogStatus = $"Sensor type changed to: {value.Name}";
        }

        [RelayCommand]
        public async Task RefreshPorts()
        {
            PortList.Clear();
            
            try
            {
                var ports = await Task.Run(() =>
                {
                    var list = new System.Collections.Generic.List<string>();
                    try
                    {
                        using (var searcher = new System.Management.ManagementObjectSearcher(
                            "SELECT * FROM Win32_PnPEntity WHERE Caption LIKE '%(COM%'"))
                        {
                            foreach (var item in searcher.Get())
                            {
                                string caption = item["Caption"]?.ToString() ?? "";
                                if (!string.IsNullOrEmpty(caption))
                                    list.Add(caption);
                            }
                        }
                    }
                    catch { }
                    
                    if (list.Count == 0)
                        list.AddRange(SerialPort.GetPortNames());
                    
                    return list.Distinct().OrderBy(x => x).ToList();
                });
                
                foreach (var p in ports)
                    PortList.Add(p);
            }
            catch
            {
                foreach (var p in SerialPort.GetPortNames())
                    PortList.Add(p);
            }
            
            if (PortList.Count > 0)
            {
                SelectedSensorPort = PortList[0];
                SelectedControllerPort = PortList[0];
            }
        }

        [RelayCommand]
        public async Task ToggleSensor()
        {
            if (IsMfgSensor)
            {
                if (_tcpManager.IsConnected)
                {
                    _tcpManager.Disconnect();
                    SensorStatus = "OFF";
                }
                else
                {
                    SensorStatus = "CONNECTING...";
                    bool connected = await _tcpManager.ConnectAsync(MfgIpAddress, MfgPort);
                    
                    if (!connected)
                    {
                        SensorStatus = "ERROR";
                        LogStatus = $"Failed to connect to {MfgIpAddress}:{MfgPort}";
                        await Task.Delay(1500);
                        SensorStatus = "OFF";
                    }
                }
            }
            else
            {
                if (_sensorManager.IsOpen)
                {
                    _sensorManager.Disconnect();
                    SensorStatus = "OFF";
                }
                else
                {
                    if (string.IsNullOrEmpty(SelectedSensorPort)) return;
                    _sensorManager.Connect(SelectedSensorPort, 115200);
                    SensorStatus = "ON";
                }
            }
            
            OnPropertyChanged(nameof(SensorColor));
        }

        [RelayCommand]
        public void ToggleController()
        {
            try
            {
                if (_controllerPort != null && _controllerPort.IsOpen)
                {
                    _timerSender.Stop();
                    _controllerPort.Close();
                    ControllerStatus = "OFF";
                }
                else
                {
                    if (string.IsNullOrEmpty(SelectedControllerPort)) return;
                    
                    string portName = ExtractPortName(SelectedControllerPort);
                    _controllerPort = new SerialPort(portName, 115200);
                    _controllerPort.Open();
                    _timerSender.Start();
                    ControllerStatus = "ON";
                }
            }
            catch (Exception ex)
            {
                LogStatus = $"Controller error: {ex.Message}";
                ControllerStatus = "ERROR";
            }
            
            OnPropertyChanged(nameof(ControllerColor));
        }
        
        private string ExtractPortName(string portDisplay)
        {
            var match = System.Text.RegularExpressions.Regex.Match(portDisplay, @"COM\d+");
            return match.Success ? match.Value : portDisplay;
        }
    }
}
