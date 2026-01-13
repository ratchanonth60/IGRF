#nullable enable

using System.Collections.Generic;
using System.Threading.Tasks;
using IGRF_Interface.Core.Services;

namespace IGRF_Interface.Core.Interfaces
{
    /// <summary>
    /// Interface for satellite cache service
    /// </summary>
    public interface ISatelliteCacheService
    {
        /// <summary>
        /// Save satellites to cache file
        /// </summary>
        Task SaveSatellitesAsync(IEnumerable<SatelliteInfo> satellites);

        /// <summary>
        /// Load satellites from cache file
        /// </summary>
        Task<List<SatelliteInfo>> LoadSatellitesAsync();

        /// <summary>
        /// Get default satellite list
        /// </summary>
        List<SatelliteInfo> GetDefaultSatellites();
    }
}
