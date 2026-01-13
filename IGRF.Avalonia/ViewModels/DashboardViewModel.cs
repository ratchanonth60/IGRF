using CommunityToolkit.Mvvm.ComponentModel;
using Avalonia.Media;

namespace IGRF.Avalonia.ViewModels
{
    /// <summary>
    /// ViewModel for Dashboard View
    /// Contains sensor data display properties
    /// </summary>
    public partial class DashboardViewModel : ViewModelBase
    {
        // Sensor data properties (bound from MainViewModel via shared state)
        [ObservableProperty] private double _magX;
        [ObservableProperty] private double _magY;
        [ObservableProperty] private double _magZ;
        [ObservableProperty] private double _magTotal;

        // Sensor 2 data (for dual mode)
        [ObservableProperty] private double _magX2;
        [ObservableProperty] private double _magY2;
        [ObservableProperty] private double _magZ2;
        [ObservableProperty] private double _magTotal2;

        // Temperature data
        [ObservableProperty] private double _temperature1;
        [ObservableProperty] private double _temperature2;

        // Status properties
        [ObservableProperty] private string _sensorStatus = "OFF";
        [ObservableProperty] private string _controllerStatus = "OFF";

        public SolidColorBrush SensorColor =>
            SensorStatus == "ON" ? new SolidColorBrush(Color.FromRgb(0, 255, 0)) :
            SensorStatus == "OFF" ? new SolidColorBrush(Color.FromRgb(255, 0, 0)) :
            new SolidColorBrush(Color.FromRgb(255, 165, 0));

        public SolidColorBrush ControllerColor =>
            ControllerStatus == "ON" ? new SolidColorBrush(Color.FromRgb(0, 255, 0)) :
            new SolidColorBrush(Color.FromRgb(255, 0, 0));

        public DashboardViewModel()
        {
        }

        /// <summary>
        /// Update sensor data from shared state
        /// </summary>
        public void UpdateSensorData(double x, double y, double z, double total)
        {
            MagX = x;
            MagY = y;
            MagZ = z;
            MagTotal = total;
        }

        /// <summary>
        /// Update dual sensor data from shared state
        /// </summary>
        public void UpdateDualSensorData(double x1, double y1, double z1, double total1,
                                          double x2, double y2, double z2, double total2)
        {
            MagX = x1;
            MagY = y1;
            MagZ = z1;
            MagTotal = total1;
            MagX2 = x2;
            MagY2 = y2;
            MagZ2 = z2;
            MagTotal2 = total2;
        }
    }
}
