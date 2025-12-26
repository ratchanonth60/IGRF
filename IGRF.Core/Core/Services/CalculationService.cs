using System;
using IGRF_Interface.Core.Models;
using IGRF_Interface.Core.Algorithms;

namespace IGRF_Interface.Core.Services
{
    public class CalculationService
    {
        // Kalman Filters for smoothing sensor data
        private KalmanFilter _filterX = new KalmanFilter(0, 1, 1, 100);
        private KalmanFilter _filterY = new KalmanFilter(0, 1, 1, 100);
        private KalmanFilter _filterZ = new KalmanFilter(0, 1, 1, 100);
        
        // Flag to initialize filters with first data value
        private bool _filtersInitialized = false;

        // Public access for UI to tune filter parameters
        public KalmanFilter FilterX => _filterX;
        public KalmanFilter FilterY => _filterY;
        public KalmanFilter FilterZ => _filterZ;

        public CalculationService()
        {
             // Geo.dll API is unknown - using simplified calculation instead
        }

        public (double X, double Y, double Z) CalculateIGRF(double lat, double lon, double altKm, DateTime time)
        {
             // TODO: Find correct Geo.dll API
             // For now, return realistic Earth magnetic field values based on location
             // Typical values: 20,000-60,000 nT total field
             
             // Simplified calculation - varies by latitude
             double latRad = lat * Math.PI / 180.0;
             
             // Horizontal component varies with latitude
             double horizontal = 30000 * Math.Cos(latRad);
             double vertical = 40000 * Math.Sin(latRad);
             
             // Split horizontal into X (north) and Y (east) components
             double X = horizontal * 0.9;  // Mostly northward
             double Y = horizontal * 0.1;  // Small eastward component  
             double Z = vertical;           // Downward (positive down)
             
             return (X, Y, Z);
        }

        // Process Raw Data -> ProcessedData (Pure Logic)
        public ProcessedData ProcessSensorData(RawSensorData raw, double setX, double setY, double setZ)
        {
            var data = new ProcessedData();

            // Auto-initialize filters with first real data value
            if (!_filtersInitialized)
            {
                _filterX.Reset(raw.MagX, 1);
                _filterY.Reset(raw.MagY, 1);
                _filterZ.Reset(raw.MagZ, 1);
                _filtersInitialized = true;
            }

            // 1. Filter
            data.MagX = _filterX.Filter(raw.MagX);
            data.MagY = _filterY.Filter(raw.MagY);
            data.MagZ = _filterZ.Filter(raw.MagZ);

            // 2. Calculate Error (can be negative for directional feedback)
            data.ErrorX = setX - data.MagX;
            data.ErrorY = setY - data.MagY;
            data.ErrorZ = setZ - data.MagZ;

            // 3. Calculate %
            data.ErrorPerX = CalculatePercent(data.ErrorX, setX);
            data.ErrorPerY = CalculatePercent(data.ErrorY, setY);
            data.ErrorPerZ = CalculatePercent(data.ErrorZ, setZ);

            return data;
        }

        public void ResetFilters()
        {
            _filterX.Reset(0, 1);
            _filterY.Reset(0, 1);
            _filterZ.Reset(0, 1);
            _filtersInitialized = false;  // Allow re-initialization
        }

        private double CalculatePercent(double error, double setpoint)
        {
            return setpoint != 0 ? (error / Math.Abs(setpoint)) * 100 : 0;
        }
    }
}