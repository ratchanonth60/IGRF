using System;
using System.IO.Ports;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using IGRF_Interface.Core.Algorithms;
using IGRF_Interface.Core.Interfaces;
using IGRF_Interface.Core.Services;
using IGRF_Interface.Infrastructure.Communication;
using IGRF_Interface.Infrastructure.Interfaces;
using IGRF.Avalonia.Services;

namespace IGRF.Avalonia.ViewModels
{
    /// <summary>
    /// MainViewModel - Core
    /// Contains core services, timers, and constructor
    ///
    /// Split into partial classes for maintainability:
    /// - MainViewModel.PID.cs - PID parameters and control
    /// - MainViewModel.Sensor.cs - Sensor connection and type selection
    /// - MainViewModel.Config.cs - Save/Load configuration
    /// - MainViewModel.Navigation.cs - View navigation
    /// - MainViewModel.DataHandling.cs - Sensor data processing
    /// - MainViewModel.Logging.cs - Data logging
    /// - MainViewModel.Satellite.cs - Satellite tracking and IGRF
    /// - MainViewModel.Map.cs - Geomagnetic map loading
    /// </summary>
    public partial class MainViewModel : ViewModelBase
    {
        // ===== Core Services (DI) =====
        private readonly ISensorService _sensorService;
        private readonly ICalculationService _calcService;
        private readonly INavigationService _navigationService;
        private readonly GlobePipeService _globePipeService;

        // Infrastructure (DI)
        private readonly ISerialPortManager _sensorManager;
        private readonly ITcpClientManager _tcpManager;

        // Controller (Sender) Port
        private SerialPort? _controllerPort;

        // Algorithms
        private readonly PidController _pidX,
            _pidY,
            _pidZ;

        // Timers
        private readonly DispatcherTimer _timerUiUpdate;
        private readonly DispatcherTimer _timerSender;
        private readonly DispatcherTimer _timerPidX,
            _timerPidY,
            _timerPidZ;

        // Constants
        private const byte HEADER_BYTE = 0xA0;
        private const int UI_REFRESH_RATE_MS = 50;
        private const int SENDER_INTERVAL = 100;

        private readonly byte[] _txBuffer = new byte[15];

        // PID Output values
        private double _outputX,
            _outputY,
            _outputZ;

        /// <summary>
        /// Constructor - Full DI enabled
        /// </summary>
        public MainViewModel(
            ISensorService sensorService,
            ICalculationService calcService,
            INavigationService navigationService,
            GlobePipeService globePipeService,
            ISerialPortManager sensorManager,
            ITcpClientManager tcpManager
        )
        {
            // DI Services
            _sensorService = sensorService;
            _calcService = calcService;
            _navigationService = navigationService;
            _globePipeService = globePipeService;
            _sensorManager = sensorManager;
            _tcpManager = tcpManager;

            // Init Algorithms (using default constructor + property setters)
            _pidX = new PidController
            {
                Kp = KpX,
                Ki = KiX,
                Kd = KdX,
                MinOutput = MinOutputX,
                MaxOutput = MaxOutputX,
            };
            _pidY = new PidController
            {
                Kp = KpY,
                Ki = KiY,
                Kd = KdY,
                MinOutput = MinOutputY,
                MaxOutput = MaxOutputY,
            };
            _pidZ = new PidController
            {
                Kp = KpZ,
                Ki = KiZ,
                Kd = KdZ,
                MinOutput = MinOutputZ,
                MaxOutput = MaxOutputZ,
            };

            // Init Timers
            _timerUiUpdate = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(UI_REFRESH_RATE_MS),
            };
            _timerSender = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(SENDER_INTERVAL),
            };
            _timerSender.Tick += TimerSender_Tick;

            _timerPidX = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(10) };
            _timerPidX.Tick += (s, e) => RunPidLogic(_pidX, SetpointX, MagX, "X");
            _timerPidY = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(10) };
            _timerPidY.Tick += (s, e) => RunPidLogic(_pidY, SetpointY, MagY, "Y");
            _timerPidZ = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(10) };
            _timerPidZ.Tick += (s, e) => RunPidLogic(_pidZ, SetpointZ, MagZ, "Z");

            // Wire up sensor events
            _sensorManager.OnPacketReceived += HandleSensorPacket;
            _tcpManager.OnDataReceived += HandleMfgDataPacket;
            _tcpManager.OnConnectionChanged += (connected) =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    SensorStatus = connected ? "ON" : "OFF";
                    OnPropertyChanged(nameof(SensorColor));
                });
            };

            // Subscribe to navigation changes
            _navigationService.CurrentViewChanged += (view) =>
            {
                CurrentView = view as global::Avalonia.Controls.UserControl;
            };

            // Initialize components
            InitializeNavigation();
            InitializeSensorTypes();
            InitializeSatellites();

            // Load config and refresh ports
            LoadConfig();
            _ = RefreshPorts();
        }

        private void TimerSender_Tick(object? sender, EventArgs e)
        {
            if (_controllerPort == null || !_controllerPort.IsOpen)
                return;

            try
            {
                _txBuffer[0] = HEADER_BYTE;

                // Pack X/Y/Z outputs as 4-byte floats
                Buffer.BlockCopy(BitConverter.GetBytes((float)_outputX), 0, _txBuffer, 1, 4);
                Buffer.BlockCopy(BitConverter.GetBytes((float)_outputY), 0, _txBuffer, 5, 4);
                Buffer.BlockCopy(BitConverter.GetBytes((float)_outputZ), 0, _txBuffer, 9, 4);

                // Checksum
                byte checksum = 0;
                for (int i = 0; i < 13; i++)
                    checksum ^= _txBuffer[i];
                _txBuffer[13] = checksum;
                _txBuffer[14] = 0x0A; // LF

                _controllerPort.Write(_txBuffer, 0, 15);
            }
            catch (Exception ex)
            {
                LogStatus = $"TX Error: {ex.Message}";
            }
        }
    }
}
