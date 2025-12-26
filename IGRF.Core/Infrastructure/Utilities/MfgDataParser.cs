using System;
using System.Runtime.InteropServices;

namespace IGRF_Interface.Infrastructure.Utilities
{
    /// <summary>
    /// MFG Data Structure Parser
    /// Parses binary data from MFG Digital Fluxgate Magnetometer
    /// Based on manual Section 2.1: mag_data_struct
    /// </summary>
    public class MfgDataParser
    {
        // Data type constants
        public const int TYPE_DAT = 1;  // Measurement data
        public const int TYPE_REP = 2;  // Command reply
        public const int TYPE_POS = 3;  // GPS position
        public const int TYPE_SDS = 4;  // SD card status
        public const int TYPE_LOG = 5;  // Log messages
        public const int TYPE_CCN = 6;  // Capture counter
        public const int TYPE_HTS = 7;  // Heater status

        /// <summary>
        /// MFG data structure matching C struct
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct MagDataStruct
        {
            public int DataType;
            
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public int[] L;
            
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 14)]
            public float[] F;
        }

        /// <summary>
        /// Parse binary packet from MFG sensor
        /// </summary>
        /// <param name="packet">Binary data packet (72 bytes)</param>
        /// <returns>Parsed data structure, or null if invalid</returns>
        public static MagDataStruct? Parse(byte[] packet)
        {
            if (packet == null || packet.Length < 72)
                return null;

            try
            {
                // Parse manually to ensure Little Endian (MFG format)
                var data = new MagDataStruct
                {
                    L = new int[3],
                    F = new float[14]
                };

                int offset = 0;
                
                // DataType (4 bytes)
                data.DataType = BitConverter.ToInt32(packet, offset);
                offset += 4;
                
                // L[3] (3 x 4 bytes = 12 bytes)
                for (int i = 0; i < 3; i++)
                {
                    data.L[i] = BitConverter.ToInt32(packet, offset);
                    offset += 4;
                }
                
                // F[14] (14 x 4 bytes = 56 bytes)
                for (int i = 0; i < 14; i++)
                {
                    data.F[i] = BitConverter.ToSingle(packet, offset);
                    offset += 4;
                }
                
                return data;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Extract magnetic field data from parsed structure
        /// Returns array of [X, Y, Z] in nanoTesla
        /// </summary>
        public static float[]? GetMagneticField(MagDataStruct data, int sensorIndex = 1)
        {
            if (data.DataType != TYPE_DAT)
                return null;

            if (sensorIndex == 1)
            {
                // Sensor 1: f[8], f[9], f[10]
                return new[] { data.F[8], data.F[9], data.F[10] };
            }
            else if (sensorIndex == 2)
            {
                // Sensor 2: f[11], f[12], f[13]
                return new[] { data.F[11], data.F[12], data.F[13] };
            }
            
            return null;
        }

        /// <summary>
        /// Get temperature readings from data
        /// </summary>
        public static (float sensor1Temp, float electronicsTemp, float sensor2Temp)? GetTemperatures(MagDataStruct data)
        {
            if (data.DataType != TYPE_DAT)
                return null;

            return (data.F[0], data.F[1], data.F[7]);
        }

        /// <summary>
        /// Get GPS position from data
        /// </summary>
        public static (double latitude, double longitude)? GetGpsPosition(MagDataStruct data)
        {
            if (data.DataType != TYPE_POS)
                return null;

            return (data.F[0], data.F[1]);
        }
    }
}
