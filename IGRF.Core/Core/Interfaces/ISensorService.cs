#nullable enable

using IGRF_Interface.Core.Models;

namespace IGRF_Interface.Core.Interfaces
{
    /// <summary>
    /// Interface for sensor data processing service
    /// </summary>
    public interface ISensorService
    {
        /// <summary>
        /// Current sensor configuration
        /// </summary>
        SensorConfig Config { get; }

        /// <summary>
        /// Zero offset reference values
        /// </summary>
        double ReferenceX { get; set; }
        double ReferenceY { get; set; }
        double ReferenceZ { get; set; }

        /// <summary>
        /// Change the sensor type and update calibration parameters
        /// </summary>
        void SetSensorType(SensorType sensorType);

        /// <summary>
        /// Process raw sensor packet data for Generic sensor (7-byte serial format)
        /// </summary>
        RawSensorData ProcessData(byte[] packet);

        /// <summary>
        /// Process data from MFG magnetometer (TCP socket binary structure)
        /// </summary>
        RawSensorData ProcessMFGData(float[] magData);

        /// <summary>
        /// Set current sensor values as zero reference point
        /// </summary>
        void SetZero(double currentX, double currentY, double currentZ);
    }
}
