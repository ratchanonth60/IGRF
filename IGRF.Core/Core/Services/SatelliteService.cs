using System;
using One_Sgp4;
using IGRF_Interface.Core.Interfaces;

namespace IGRF_Interface.Core.Services
{
    public class SatelliteResult
    {
        public double Lat { get; set; }
        public double Lon { get; set; }
        public double Alt { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }

    }
    // Helper Class for ComboBox
    public class SatelliteInfo
    {
        public string Name { get; set; }
        public string ID { get; set; }
        public string Line1 { get; set; }
        public string Line2 { get; set; }
        public string ObjectType { get; set; } = "UNKNOWN";
        public override string ToString() => $"{Name} ({ObjectType})";
    }

    public class SatelliteService : ISatelliteService
    {
        private Tle _currentTle;

        public void SetTLE(string name, string tle1, string tle2)
        {
            if (string.IsNullOrWhiteSpace(tle1) || string.IsNullOrWhiteSpace(tle2))
            {
                _currentTle = null;
                return;
            }

            try
            {
                // Wrapper Parse TLE
                // Expected signature: parseTle(string adr1, string adr2, string name)
                _currentTle = ParserTLE.parseTle(tle1, tle2, name);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TLE Parse Error for {name}: {ex.Message}");
                _currentTle = null;
            }
        }

        public SatelliteResult CalculatePosition(DateTime time)
        {
            if (_currentTle == null) return new SatelliteResult();

            EpochTime epoch = new EpochTime(time);
            var sgp4Data = SatFunctions.getSatPositionAtTime(_currentTle, epoch, Sgp4.wgsConstant.WGS_84);
            var subPoint = SatFunctions.calcSatSubPoint(epoch, sgp4Data, Sgp4.wgsConstant.WGS_84);

            return new SatelliteResult
            {
                Lat = subPoint.getLatitude(),
                Lon = subPoint.getLongitude(),
                Alt = subPoint.getHeight(),
                X = sgp4Data.getX(),
                Y = sgp4Data.getY(),
                Z = sgp4Data.getZ(),
            };
        }
    }
}