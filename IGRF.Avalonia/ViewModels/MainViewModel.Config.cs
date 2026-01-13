using System.IO;
using CommunityToolkit.Mvvm.Input;
using IGRF_Interface.Core.Models;

namespace IGRF.Avalonia.ViewModels
{
    /// <summary>
    /// MainViewModel - Configuration Logic
    /// Contains save/load config, master reset functionality
    /// </summary>
    public partial class MainViewModel
    {
        [RelayCommand]
        public void SaveConfig()
        {
            var config = new AppConfig
            {
                PidX = new PidSettings { Kp = KpX, Ki = KiX, Kd = KdX, MinOutput = MinOutputX, MaxOutput = MaxOutputX },
                PidY = new PidSettings { Kp = KpY, Ki = KiY, Kd = KdY, MinOutput = MinOutputY, MaxOutput = MaxOutputY },
                PidZ = new PidSettings { Kp = KpZ, Ki = KiZ, Kd = KdZ, MinOutput = MinOutputZ, MaxOutput = MaxOutputZ }
            };
            
            AppConfig.Save(config);
            LogStatus = "Configuration saved successfully!";
        }
        
        [RelayCommand]
        public void LoadConfig()
        {
            var config = AppConfig.Load();
            
            KpX = config.PidX.Kp;
            KiX = config.PidX.Ki;
            KdX = config.PidX.Kd;
            MinOutputX = config.PidX.MinOutput;
            MaxOutputX = config.PidX.MaxOutput;
            
            KpY = config.PidY.Kp;
            KiY = config.PidY.Ki;
            KdY = config.PidY.Kd;
            MinOutputY = config.PidY.MinOutput;
            MaxOutputY = config.PidY.MaxOutput;
            
            KpZ = config.PidZ.Kp;
            KiZ = config.PidZ.Ki;
            KdZ = config.PidZ.Kd;
            MinOutputZ = config.PidZ.MinOutput;
            MaxOutputZ = config.PidZ.MaxOutput;
            
            UpdatePidParams();
            UpdatePidBounds();
            
            LogStatus = "Configuration loaded successfully!";
        }
        
        [RelayCommand]
        public void MasterReset()
        {
            KpX = KpY = KpZ = 1.0;
            KiX = KiY = KiZ = 0.0;
            KdX = KdY = KdZ = 0.0;
            
            _calcService.ResetFilters();
            UpdatePidParams();
            
            LogStatus = "Master reset complete!";
        }
    }
}
