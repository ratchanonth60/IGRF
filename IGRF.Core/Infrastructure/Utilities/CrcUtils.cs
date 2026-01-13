using System;
using System.Linq;

namespace IGRF_Interface.Infrastructure.Utilities
{
    public static class CrcUtils
    {
        /// <summary>
        /// Calculates Modbus RTU CRC16 (Zero Allocation)
        /// </summary>
        public static ushort CalculateCrc(byte[] data, int length)
        {
            ushort crc = 0xFFFF;
            for (int pos = 0; pos < length; pos++)
            {
                crc ^= (UInt16)data[pos];
                for (int i = 8; i != 0; i--)
                {
                    if ((crc & 0x0001) != 0)
                    {
                        crc >>= 1;
                        crc ^= 0xA001;
                    }
                    else
                    {
                        crc >>= 1;
                    }
                }
            }
            return crc;
        }

        /// <summary>
        /// Writes Float to buffer at offset (Manual bit-shift implementation to avoid BitConverter allocation if needed, 
        /// but wrapper around BitConverter is cleaner for readability, optimization focused on CRC first).
        /// </summary>
        public static void WriteFloat(byte[] buffer, int offset, float value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            Buffer.BlockCopy(bytes, 0, buffer, offset, 4);
        }
    }
}