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
    }
}
