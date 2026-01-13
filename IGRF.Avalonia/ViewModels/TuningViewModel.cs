using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace IGRF.Avalonia.ViewModels
{
    /// <summary>
    /// ViewModel for Tuning View
    /// Contains PID parameters and chart data events
    /// </summary>
    public partial class TuningViewModel : ViewModelBase
    {
        // PID X parameters
        [ObservableProperty] private double _kpX = 1.0;
        [ObservableProperty] private double _kiX = 0.0;
        [ObservableProperty] private double _kdX = 0.0;
        [ObservableProperty] private double _setpointX = 0.0;
        [ObservableProperty] private double _minOutputX = -100;
        [ObservableProperty] private double _maxOutputX = 100;

        // PID Y parameters
        [ObservableProperty] private double _kpY = 1.0;
        [ObservableProperty] private double _kiY = 0.0;
        [ObservableProperty] private double _kdY = 0.0;
        [ObservableProperty] private double _setpointY = 0.0;
        [ObservableProperty] private double _minOutputY = -100;
        [ObservableProperty] private double _maxOutputY = 100;

        // PID Z parameters
        [ObservableProperty] private double _kpZ = 1.0;
        [ObservableProperty] private double _kiZ = 0.0;
        [ObservableProperty] private double _kdZ = 0.0;
        [ObservableProperty] private double _setpointZ = 0.0;
        [ObservableProperty] private double _minOutputZ = -100;
        [ObservableProperty] private double _maxOutputZ = 100;

        // Current sensor values for display
        [ObservableProperty] private double _magX;
        [ObservableProperty] private double _magY;
        [ObservableProperty] private double _magZ;

        // PID Enable flags
        [ObservableProperty] private bool _enablePidX;
        [ObservableProperty] private bool _enablePidY;
        [ObservableProperty] private bool _enablePidZ;

        /// <summary>
        /// Event for chart data update (single sensor mode)
        /// </summary>
        public event Action<double, double, double>? DataUpdated;

        /// <summary>
        /// Event for chart data update (dual sensor mode)
        /// </summary>
        public event Action<double, double, double, double, double, double>? DualDataUpdated;

        public TuningViewModel()
        {
        }

        /// <summary>
        /// Raise data updated event for charts (single sensor)
        /// </summary>
        public void RaiseDataUpdated(double x, double y, double z)
        {
            MagX = x;
            MagY = y;
            MagZ = z;
            DataUpdated?.Invoke(x, y, z);
        }

        /// <summary>
        /// Raise data updated event for charts (dual sensor)
        /// </summary>
        public void RaiseDualDataUpdated(double x1, double y1, double z1, double x2, double y2, double z2)
        {
            MagX = x1;
            MagY = y1;
            MagZ = z1;
            DualDataUpdated?.Invoke(x1, y1, z1, x2, y2, z2);
        }

        [RelayCommand]
        private void ResetPidX()
        {
            KpX = 1.0;
            KiX = 0.0;
            KdX = 0.0;
        }

        [RelayCommand]
        private void ResetPidY()
        {
            KpY = 1.0;
            KiY = 0.0;
            KdY = 0.0;
        }

        [RelayCommand]
        private void ResetPidZ()
        {
            KpZ = 1.0;
            KiZ = 0.0;
            KdZ = 0.0;
        }
    }
}
