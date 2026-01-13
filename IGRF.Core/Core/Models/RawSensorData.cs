namespace IGRF_Interface.Core.Models
{
    /// <summary>
    /// Raw sensor data structure (before filtering)
    /// </summary>
    public class RawSensorData
    {
        public double x { get; set; }
        public double y { get; set; }
        public double z { get; set; }

        // Alias properties for compatibility with different naming conventions
        public double MagX { get => x; set => x = value; }
        public double MagY { get => y; set => y = value; }
        public double MagZ { get => z; set => z = value; }

        // Constructor with values
        public RawSensorData(double xVal, double yVal, double zVal)
        {
            x = xVal;
            y = yVal;
            z = zVal;
        }

        // Default constructor
        public RawSensorData() { }
    }
}
