using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Threading;

namespace IGRF.Avalonia.ViewModels
{
    /// <summary>
    /// MainViewModel - PID Controller Logic
    /// Contains PID parameters, axis selection, setpoints, and PID control commands
    /// </summary>
    public partial class MainViewModel
    {
        // --- PID Parameters (Per-Axis) ---
        [ObservableProperty] private string _selectedPidAxis = "X";
        
        // Per-axis PID gains
        [ObservableProperty] private double _kpX = 1.0;
        [ObservableProperty] private double _kiX = 0.0;
        [ObservableProperty] private double _kdX = 0.0;
        [ObservableProperty] private double _kpY = 1.0;
        [ObservableProperty] private double _kiY = 0.0;
        [ObservableProperty] private double _kdY = 0.0;
        [ObservableProperty] private double _kpZ = 1.0;
        [ObservableProperty] private double _kiZ = 0.0;
        [ObservableProperty] private double _kdZ = 0.0;
        
        // Computed properties for currently selected axis
        public double Kp
        {
            get => SelectedPidAxis switch { "X" => KpX, "Y" => KpY, "Z" => KpZ, _ => KpX };
            set { switch (SelectedPidAxis) { case "X": KpX = value; break; case "Y": KpY = value; break; case "Z": KpZ = value; break; } OnPropertyChanged(); }
        }
        public double Ki
        {
            get => SelectedPidAxis switch { "X" => KiX, "Y" => KiY, "Z" => KiZ, _ => KiX };
            set { switch (SelectedPidAxis) { case "X": KiX = value; break; case "Y": KiY = value; break; case "Z": KiZ = value; break; } OnPropertyChanged(); }
        }
        public double Kd
        {
            get => SelectedPidAxis switch { "X" => KdX, "Y" => KdY, "Z" => KdZ, _ => KdX };
            set { switch (SelectedPidAxis) { case "X": KdX = value; break; case "Y": KdY = value; break; case "Z": KdZ = value; break; } OnPropertyChanged(); }
        }
        
        // --- Setpoints ---
        [ObservableProperty] private double _setpointX;
        [ObservableProperty] private double _setpointY;
        [ObservableProperty] private double _setpointZ;
        [ObservableProperty] private bool _isAutoSetpoint = true;
        
        // --- PID Output Bounds ---
        [ObservableProperty] private double _minOutputX = -100.0;
        [ObservableProperty] private double _maxOutputX = 100.0;
        [ObservableProperty] private double _minOutputY = -100.0;
        [ObservableProperty] private double _maxOutputY = 100.0;
        [ObservableProperty] private double _minOutputZ = -100.0;
        [ObservableProperty] private double _maxOutputZ = 100.0;
        
        // --- Manual Setpoint Override ---
        [ObservableProperty] private double _manualSetpointX;
        [ObservableProperty] private double _manualSetpointY;
        [ObservableProperty] private double _manualSetpointZ;
        
        // --- Kalman Filter R Tuning ---
        [ObservableProperty] private double _filterRX = 1.0;
        [ObservableProperty] private double _filterRY = 1.0;
        [ObservableProperty] private double _filterRZ = 1.0;
        
        // --- PID Enable Flags ---
        [ObservableProperty] private bool _isPidXEnabled;
        [ObservableProperty] private bool _isPidYEnabled;
        [ObservableProperty] private bool _isPidZEnabled;
        
        // Partial Methods to update Logic
        partial void OnKpXChanged(double value) => UpdatePidParams();
        partial void OnKiXChanged(double value) => UpdatePidParams();
        partial void OnKdXChanged(double value) => UpdatePidParams();
        partial void OnKpYChanged(double value) => UpdatePidParams();
        partial void OnKiYChanged(double value) => UpdatePidParams();
        partial void OnKdYChanged(double value) => UpdatePidParams();
        partial void OnKpZChanged(double value) => UpdatePidParams();
        partial void OnKiZChanged(double value) => UpdatePidParams();
        partial void OnKdZChanged(double value) => UpdatePidParams();
        
        partial void OnSelectedPidAxisChanged(string value)
        {
            OnPropertyChanged(nameof(Kp));
            OnPropertyChanged(nameof(Ki));
            OnPropertyChanged(nameof(Kd));
        }

        private void UpdatePidParams()
        {
            if (_pidX == null || _pidY == null || _pidZ == null) return;
            _pidX.Kp = KpX; _pidX.Ki = KiX; _pidX.Kd = KdX;
            _pidY.Kp = KpY; _pidY.Ki = KiY; _pidY.Kd = KdY;
            _pidZ.Kp = KpZ; _pidZ.Ki = KiZ; _pidZ.Kd = KdZ;
        }
        
        private void UpdatePidBounds()
        {
            if (_pidX == null || _pidY == null || _pidZ == null) return;
            _pidX.MinOutput = MinOutputX; _pidX.MaxOutput = MaxOutputX;
            _pidY.MinOutput = MinOutputY; _pidY.MaxOutput = MaxOutputY;
            _pidZ.MinOutput = MinOutputZ; _pidZ.MaxOutput = MaxOutputZ;
        }
        
        [RelayCommand]
        public void SelectPidAxis(string axis)
        {
            SelectedPidAxis = axis;
        }

        [RelayCommand]
        public void TogglePid(string axis)
        {
            switch (axis)
            {
                case "X":
                    IsPidXEnabled = !IsPidXEnabled;
                    if (IsPidXEnabled) { _pidX.Reset(); _timerPidX.Start(); } else _timerPidX.Stop();
                    break;
                case "Y":
                    IsPidYEnabled = !IsPidYEnabled;
                    if (IsPidYEnabled) { _pidY.Reset(); _timerPidY.Start(); } else _timerPidY.Stop();
                    break;
                case "Z":
                    IsPidZEnabled = !IsPidZEnabled;
                    if (IsPidZEnabled) { _pidZ.Reset(); _timerPidZ.Start(); } else _timerPidZ.Stop();
                    break;
            }
        }
        
        [RelayCommand]
        public void SetTargetX()
        {
            SetpointX = ManualSetpointX;
            UpdatePidBounds();
            if (_timerSat != null && _timerSat.IsEnabled)
            {
                _timerSat.Stop();
                IsAutoSetpoint = false;
            }
        }
        
        [RelayCommand]
        public void SetTargetY()
        {
            SetpointY = ManualSetpointY;
            UpdatePidBounds();
            if (_timerSat != null && _timerSat.IsEnabled)
            {
                _timerSat.Stop();
                IsAutoSetpoint = false;
            }
        }
        
        [RelayCommand]
        public void SetTargetZ()
        {
            SetpointZ = ManualSetpointZ;
            UpdatePidBounds();
            if (_timerSat != null && _timerSat.IsEnabled)
            {
                _timerSat.Stop();
                IsAutoSetpoint = false;
            }
        }
        
        [RelayCommand]
        public void SetFilterR(string axis)
        {
            switch (axis)
            {
                case "X":
                    _calcService.FilterX.R_Val = FilterRX;
                    break;
                case "Y":
                    _calcService.FilterY.R_Val = FilterRY;
                    break;
                case "Z":
                    _calcService.FilterZ.R_Val = FilterRZ;
                    break;
            }
        }
        
        private void RunPidLogic(IGRF_Interface.Core.Algorithms.PidController pid, double setpoint, double measured, string axis)
        {
            double output = pid.Calculate(setpoint, measured);
            
            switch (axis)
            {
                case "X": _outputX = output; break;
                case "Y": _outputY = output; break;
                case "Z": _outputZ = output; break;
            }
        }
    }
}
