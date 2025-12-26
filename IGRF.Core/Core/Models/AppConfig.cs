using Newtonsoft.Json;
using System.IO;

namespace IGRF_Interface.Core.Models
{
    public class PidSettings
    {
        public double Kp { get; set; } = 0;
        public double Ki { get; set; } = 0;
        public double Kd { get; set; } = 0;
        public double MaxOutput { get; set; } = 100;
        public double MinOutput { get; set; } = -100;
    }

    public class AppConfig
    {
        public PidSettings PidX { get; set; } = new PidSettings();
        public PidSettings PidY { get; set; } = new PidSettings();
        public PidSettings PidZ { get; set; } = new PidSettings();
        
        // Sensor configuration
        public SensorType SelectedSensorType { get; set; } = SensorType.Generic;

        // Helper Methods  Save/Load
        public static void Save(AppConfig config, string path = "SystemConfig.json")
        {
            string json = JsonConvert.SerializeObject(config, Formatting.Indented);
            File.WriteAllText(path, json);
        }

        public static AppConfig Load(string path = "SystemConfig.json")
        {
            try
            {
                if (!File.Exists(path)) return new AppConfig(); // ???????????? ????????? Default (0,0,0)

                string json = File.ReadAllText(path);
                var config = JsonConvert.DeserializeObject<AppConfig>(json);
                return config ?? new AppConfig(); // ?????????????????????????
            }
            catch
            {
                return new AppConfig(); // ?????????? ????????? Default ??????
            }
        }
    }
}