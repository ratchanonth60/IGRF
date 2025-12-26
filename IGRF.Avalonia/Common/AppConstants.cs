namespace IGRF.Avalonia.Common
{
    /// <summary>
    /// Application-wide constants for colors, dimensions, and configuration values
    /// </summary>
    public static class AppConstants
    {
        /// <summary>
        /// Color scheme constants (hex values)
        /// </summary>
        public static class Colors
        {
            // Dark Theme
            public const string DarkBackground = "#1e1e1e";
            public const string AxisColor = "#b0b0b0";
            public const string GridColor = "#333333";
            
            // Plot Colors (Blue Monochromatic Theme)
            public const string CyanBlue = "#00BFFF";      // X-Axis
            public const string SkyBlue = "#4A9EFF";       // Y-Axis  
            public const string SlateBlue = "#7B68EE";     // Z-Axis
            public const string OrangeSatellite = "#FF4500";
            public const string Lime = "#00FF00";
            public const string Cyan = "#00FFFF";
            
            // Sensor 2 Colors (Warm Theme for dual mode)
            public const string Orange = "#FFA500";        // Sensor 2 lines
        }

        /// <summary>
        /// Map and geographic constants
        /// </summary>
        public static class MapSettings
        {
            public const int LatitudeRange = 180;
            public const int LongitudeRange = 360;
            public const int LatitudeMin = -90;
            public const int LatitudeMax = 90;
            public const int LongitudeMin = -180;
            public const int LongitudeMax = 180;
            public const int GridStep = 2;
            public const int GridRows = LatitudeRange / GridStep;  // 90
            public const int GridCols = LongitudeRange / GridStep;  // 180
        }

        /// <summary>
        /// ScottPlot configuration constants
        /// </summary>
        public static class PlotSettings
        {
            public const float SatelliteMarkerSize = 15f;
            public const float DefaultLineWidth = 1.5f;
            public const float DefaultLineWidthBold = 2f;
            public const int DataStreamerCapacity = 100;
        }

        /// <summary>
        /// Serial communication constants
        /// </summary>
        public static class Serial
        {
            public const byte HeaderByte = 0xAA;
            public const int TxBufferSize = 15;
            public const int RxPacketSize = 6;
            public const int DefaultBaudRate = 115200;
            public const int WriteTimeoutMs = 500;
        }

        /// <summary>
        /// UI update intervals (milliseconds)
        /// </summary>
        public static class Timing
        {
            public const int UiUpdateIntervalMs = 100;     // 10 Hz
            public const int SenderIntervalMs = 100;        // 10 Hz
            public const int SatelliteUpdateIntervalMs = 1000;  // 1 Hz
        }
    }
}
