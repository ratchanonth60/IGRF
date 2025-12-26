using IGRF_Interface.Core.Models;
using System;

namespace IGRF_Interface.Core.Services
{
    /// <summary>
    /// Service for processing raw sensor data from magnetometer
    /// Supports multiple sensor types including Generic (Serial) and MFG (TCP Socket)
    /// </summary>
    public class SensorService
    {
        private SensorConfig _config;

        // Calibration constants (used for Generic sensor only)
        private double _alpha;
        private double _beta;
        private double _gamma;
        private double _sigma;
        private double _conversionFactor;

        /// <summary>
        /// Zero offset reference values for each axis
        /// </summary>
        public double ReferenceX { get; set; }
        public double ReferenceY { get; set; }
        public double ReferenceZ { get; set; }

        /// <summary>
        /// Current sensor configuration
        /// </summary>
        public SensorConfig Config => _config;

        public SensorService(SensorType sensorType = SensorType.Generic)
        {
            SetSensorType(sensorType);
        }

        /// <summary>
        /// Change the sensor type and update calibration parameters
        /// </summary>
        public void SetSensorType(SensorType sensorType)
        {
            _config = SensorConfig.GetConfig(sensorType);
            
            if (!_config.IsPreCalibrated)
            {
                _alpha = _config.Alpha;
                _beta = _config.Beta;
                _gamma = _config.Gamma;
                _sigma = _config.Sigma;
                _conversionFactor = _config.ConversionFactor;
            }
        }

        /// <summary>
        /// Process raw sensor packet data for Generic sensor (7-byte serial format)
        /// </summary>
        /// <param name="packet">7-byte sensor data packet</param>
        /// <returns>Calibrated magnetic field data in nanoTesla</returns>
        public RawSensorData ProcessData(byte[] packet)
        {
            if (packet == null || packet.Length < 7) 
                return new RawSensorData();

            if (_config.Type == SensorType.Generic)
            {
                return ProcessGenericSerialData(packet);
            }
            
            // For MFG sensors, use ProcessMFGData instead
            return new RawSensorData();
        }

        /// <summary>
        /// Process data from Generic serial sensor with calibration
        /// </summary>
        private RawSensorData ProcessGenericSerialData(byte[] packet)
        {
            // Note: Data is Big Endian (High Byte first) based on original logic {packet[1], packet[0]}
            
            // X-axis (Bytes 0-1)
            short magX_raw = (short)((packet[0] << 8) | packet[1]);
            double magX_nT = ((magX_raw * _conversionFactor - _alpha) * _sigma) - ReferenceX;

            // Y-axis (Bytes 2-3)
            short magY_raw = (short)((packet[2] << 8) | packet[3]);
            double magY_nT = (magY_raw * _conversionFactor - _beta) - ReferenceY;

            // Z-axis (Bytes 4-5)
            short magZ_raw = (short)((packet[4] << 8) | packet[5]);
            double magZ_nT = (magZ_raw * _conversionFactor - _gamma) - ReferenceZ;

            return new RawSensorData
            {
                MagX = magX_nT,
                MagY = magY_nT,
                MagZ = magZ_nT
            };
        }

        /// <summary>
        /// Process data from MFG magnetometer (TCP socket binary structure)
        /// Data comes pre-calibrated in nanoTesla units
        /// </summary>
        /// <param name="floatArray">Float array from MFG data structure (f[14])</param>
        /// <returns>Calibrated magnetic field data in nanoTesla</returns>
        public RawSensorData ProcessMFGData(float[] magData)
        {
            if (magData == null || magData.Length < 3)
                return new RawSensorData();

            // MFG data is already calibrated in nT at indices f[8], f[9], f[10]
            return new RawSensorData
            {
                MagX = magData[0] - ReferenceX,
                MagY = magData[1] - ReferenceY,
                MagZ = magData[2] - ReferenceZ
            };
        }

        /// <summary>
        /// Set current sensor values as zero reference point
        /// </summary>
        public void SetZero(double currentX, double currentY, double currentZ)
        {
            ReferenceX += currentX;
            ReferenceY += currentY;
            ReferenceZ += currentZ;
        }
    }

    /// <summary>
    /// Raw sensor data structure
    /// </summary>
    public struct RawSensorData
    {
        public double MagX { get; set; }
        public double MagY { get; set; }
        public double MagZ { get; set; }
    }
}
