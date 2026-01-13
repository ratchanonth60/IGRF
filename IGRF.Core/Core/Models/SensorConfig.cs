namespace IGRF_Interface.Core.Models
{
    /// <summary>
    /// Configuration for different sensor types with their specific calibration constants
    /// </summary>
    public class SensorConfig
    {
        public SensorType Type { get; set; }
        public string DisplayName { get; set; }
        public bool UsesSerialPort { get; set; }
        public bool UsesTcpSocket { get; set; }
        public int TcpPort { get; set; }
        
        // Calibration constants (used for Serial sensors like Generic)
        public double Alpha { get; set; }
        public double Beta { get; set; }
        public double Gamma { get; set; }
        public double Sigma { get; set; }
        public double ConversionFactor { get; set; }
        
        // MFG-specific properties
        public bool IsPreCalibrated { get; set; }  // True if data comes in nT already
        public int DataXIndex { get; set; }
        public int DataYIndex { get; set; }
        public int DataZIndex { get; set; }

        public SensorConfig()
        {
            DisplayName = "";
        }

        /// <summary>
        /// Get configuration for specified sensor type
        /// </summary>
        public static SensorConfig GetConfig(SensorType type)
        {
            switch (type)
            {
                case SensorType.MFG_1S_LC:
                case SensorType.MFG_2S_LC:
                    return new SensorConfig
                    {
                        Type = type,
                        DisplayName = type == SensorType.MFG_1S_LC ? "MFG-1S-LC (Single)" : "MFG-2S-LC (Dual)",
                        UsesSerialPort = false,
                        UsesTcpSocket = true,
                        TcpPort = 12345,
                        IsPreCalibrated = true,
                        DataXIndex = 8,  // f[8] = BX1
                        DataYIndex = 9,  // f[9] = BY1
                        DataZIndex = 10  // f[10] = BZ1
                    };
                
                case SensorType.Generic:
                default:
                    return new SensorConfig
                    {
                        Type = SensorType.Generic,
                        DisplayName = "Generic Sensor (Serial)",
                        UsesSerialPort = true,
                        UsesTcpSocket = false,
                        Alpha = 1347.35,
                        Beta = 4061.63,
                        Gamma = -1418.96,
                        Sigma = 1,
                        ConversionFactor = 20.0 / 3.0,
                        IsPreCalibrated = false,
                        DataXIndex = 0,
                        DataYIndex = 2,
                        DataZIndex = 4
                    };
            }
        }
    }
}
