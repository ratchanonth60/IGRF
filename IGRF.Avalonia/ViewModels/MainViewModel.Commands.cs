using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace IGRF.Avalonia.ViewModels
{
    /// <summary>
    /// MainViewModel - MFG Command Logic
    /// Contains commands for controlling MFG sensor (sampling rate, etc.)
    /// </summary>
    public partial class MainViewModel
    {
        // --- Sampling Rate Selection ---
        [ObservableProperty] private int _selectedSamplingRateIndex = 2; // Default: 10Hz
        
        /// <summary>
        /// Get the Hz value for display based on rate index
        /// </summary>
        public string SamplingRateDisplay => SelectedSamplingRateIndex switch
        {
            0 => "100 Hz",
            1 => "50 Hz",
            2 => "10 Hz",
            3 => "1 Hz",
            _ => "Unknown"
        };
        
        partial void OnSelectedSamplingRateIndexChanged(int value)
        {
            OnPropertyChanged(nameof(SamplingRateDisplay));
        }
        
        /// <summary>
        /// Command to set the sampling rate on MFG sensor
        /// </summary>
        [RelayCommand]
        private async Task SetSamplingRate(int rateCode)
        {
            if (_tcpManager == null || !_tcpManager.IsConnected) 
            {
                LogStatus = "Not connected to MFG sensor";
                return;
            }
            
            SelectedSamplingRateIndex = rateCode;
            bool success = await _tcpManager.SetSamplingRateAsync(rateCode);
            
            if (success)
            {
                LogStatus = $"Sampling rate set to {SamplingRateDisplay}";
            }
            else
            {
                LogStatus = "Failed to set sampling rate";
            }
        }
        
        /// <summary>
        /// Quick commands for each rate
        /// </summary>
        [RelayCommand]
        private async Task SetRate100Hz() => await SetSamplingRate(0);
        
        [RelayCommand]
        private async Task SetRate50Hz() => await SetSamplingRate(1);
        
        [RelayCommand]
        private async Task SetRate10Hz() => await SetSamplingRate(2);
        
        [RelayCommand]
        private async Task SetRate1Hz() => await SetSamplingRate(3);

        /// <summary>
        /// Launch the 3D Globe Visualization independently
        /// </summary>
        [RelayCommand]
        private void Open3DGlobe()
        {
            try
            {
                // Path to IGRF.Globe3D.exe
                // Assuming it's in the same output directory or sibling
                string currentDir = System.AppDomain.CurrentDomain.BaseDirectory;
                string globePath = System.IO.Path.Combine(currentDir, "IGRF.Globe3D.exe");
                
                // If not found in current dir, check if we are in dev environment (Avalonia/bin vs Globe3D/bin)
                if (!System.IO.File.Exists(globePath))
                {
                    // Fallback for dev: ..\..\..\..\IGRF.Globe3D\bin\Debug\net10.0-windows\IGRF.Globe3D.exe
                    string devPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(currentDir, @"..\..\..\..\IGRF.Globe3D\bin\Debug\net10.0-windows\IGRF.Globe3D.exe"));
                    if (System.IO.File.Exists(devPath))
                    {
                        globePath = devPath;
                    }
                }

                if (System.IO.File.Exists(globePath))
                {
                    var psi = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = globePath,
                        UseShellExecute = true,  // Important for .NET Core apps sometimes
                        WorkingDirectory = System.IO.Path.GetDirectoryName(globePath)
                    };
                    System.Diagnostics.Process.Start(psi);
                    LogStatus = "Launched 3D Globe";
                }
                else
                {
                    LogStatus = "Globe 3D Executable not found";
                }
            }
            catch (System.Exception ex)
            {
                LogStatus = $"Error launching 3D Globe: {ex.Message}";
            }
        }
    }
}
