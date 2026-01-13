using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using IGRF_Interface.Core.Interfaces;

namespace IGRF_Interface.Core.Services
{
    public class SatelliteCacheService : ISatelliteCacheService
    {
        private readonly string _cacheFilePath;

        public SatelliteCacheService()
        {
            // Store in AppData/Local/IGRF_Demo
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "IGRF_Demo");
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            _cacheFilePath = Path.Combine(folder, "satellites_cache.json");
        }

        public async Task SaveSatellitesAsync(IEnumerable<SatelliteInfo> satellites)
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(satellites, options);
                await File.WriteAllTextAsync(_cacheFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving satellite cache: {ex.Message}");
            }
        }

        public async Task<List<SatelliteInfo>> LoadSatellitesAsync()
        {
            try
            {
                if (File.Exists(_cacheFilePath))
                {
                    // Check if file is too old (e.g., > 7 days), maybe force refresh?
                    // For now, just load it.
                    string json = await File.ReadAllTextAsync(_cacheFilePath);
                    var list = JsonSerializer.Deserialize<List<SatelliteInfo>>(json);
                    return list ?? new List<SatelliteInfo>();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading satellite cache: {ex.Message}");
            }
            return new List<SatelliteInfo>();
        }

        public List<SatelliteInfo> GetDefaultSatellites()
        {
            return new List<SatelliteInfo>
            {
                new SatelliteInfo { Name = "-- Manual --", ID = "0" },
                new SatelliteInfo
                {
                    Name = "ISS (ZARYA)",
                    ID = "25544", 
                    // Fresh TLE 2024
                    Line1 = "1 25544U 98067A   24361.57468131  .00018596  00000+0  34421-3 0  9997",
                    Line2 = "2 25544  51.6394 135.2678 0002872 263.8569 227.1704 15.49479383488737"
                },
                new SatelliteInfo
                {
                    Name = "NOAA 19",
                    ID = "33591",
                    Line1 = "1 33591U 09005A   24361.42866632  .00000282  00000+0  17081-3 0  9998",
                    Line2 = "2 33591  99.0494 256.7645 0014022 188.7512 171.3259 14.13171804822709"
                },
                new SatelliteInfo
                {
                    Name = "THEOS (Thailand)",
                    ID = "33414",
                    // Older TLE but checksum valid
                    Line1 = "1 33414U 08049A   23354.19561081  .00000624  00000-0  20478-3 0  9997",
                    Line2 = "2 33414  98.7180 348.4239 0001648  82.4939 277.6534 14.22744837798365"
                }
            };
        }
    }
}
