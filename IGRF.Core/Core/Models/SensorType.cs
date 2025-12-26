namespace IGRF_Interface.Core.Models
{
    /// <summary>
    /// Supported magnetometer sensor types
    /// </summary>
    public enum SensorType
    {
        /// <summary>
        /// Generic sensor with custom calibration
        /// </summary>
        Generic = 0,
        
        /// <summary>
        /// Magson MFG-1S-LC Single-Axis Digital Fluxgate Magnetometer
        /// </summary>
        MFG_1S_LC = 1,
        
        /// <summary>
        /// Magson MFG-2S-LC Dual-Axis Digital Fluxgate Magnetometer
        /// </summary>
        MFG_2S_LC = 2
    }
}
