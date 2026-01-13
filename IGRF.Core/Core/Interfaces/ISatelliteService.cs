#nullable enable

using System;
using IGRF_Interface.Core.Services;

namespace IGRF_Interface.Core.Interfaces
{
    /// <summary>
    /// Interface for satellite position calculation service
    /// </summary>
    public interface ISatelliteService
    {
        /// <summary>
        /// Set TLE (Two-Line Element) data for satellite
        /// </summary>
        void SetTLE(string name, string tle1, string tle2);

        /// <summary>
        /// Calculate satellite position at given time
        /// </summary>
        SatelliteResult CalculatePosition(DateTime time);
    }
}
