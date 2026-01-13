#nullable enable

using System;
using IGRF_Interface.Core.Algorithms;
using IGRF_Interface.Core.Models;

namespace IGRF_Interface.Core.Interfaces
{
    /// <summary>
    /// Interface for calculation service - IGRF and sensor data processing
    /// </summary>
    public interface ICalculationService
    {
        /// <summary>
        /// Kalman filter for X axis
        /// </summary>
        KalmanFilter FilterX { get; }

        /// <summary>
        /// Kalman filter for Y axis
        /// </summary>
        KalmanFilter FilterY { get; }

        /// <summary>
        /// Kalman filter for Z axis
        /// </summary>
        KalmanFilter FilterZ { get; }

        /// <summary>
        /// Calculate IGRF magnetic field components
        /// </summary>
        (double X, double Y, double Z) CalculateIGRF(
            double lat,
            double lon,
            double altKm,
            DateTime time
        );

        /// <summary>
        /// Process raw sensor data with Kalman filtering
        /// </summary>
        ProcessedData ProcessSensorData(RawSensorData raw, double setX, double setY, double setZ);

        /// <summary>
        /// Reset Kalman filters
        /// </summary>
        void ResetFilters();
    }
}
