#nullable enable

using System.Threading.Tasks;
using IGRF_Interface.Core.Services;

namespace IGRF_Interface.Core.Interfaces
{
    /// <summary>
    /// Interface for Space-Track.org API service
    /// </summary>
    public interface ISpaceTrackService
    {
        /// <summary>
        /// Is currently logged in
        /// </summary>
        bool IsLoggedIn { get; }

        /// <summary>
        /// Last status message
        /// </summary>
        string LastStatus { get; }

        /// <summary>
        /// Login to Space-Track.org
        /// </summary>
        Task<bool> LoginAsync(string username, string password);

        /// <summary>
        /// Fetch TLE data for satellite
        /// </summary>
        Task<SatelliteInfo?> FetchTleAsync(string noradId);
    }
}
