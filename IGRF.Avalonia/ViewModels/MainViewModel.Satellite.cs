using System;
using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IGRF_Interface.Core.Services;
using IGRF_Interface.Core.Models;

namespace IGRF.Avalonia.ViewModels
{
    /// <summary>
    /// MainViewModel - Satellite & IGRF Logic
    /// Contains satellite tracking, TLE management, and geomagnetic calculations
    /// </summary>
    public partial class MainViewModel
    {
        // Satellite Service
        private readonly SatelliteService _satService = new SatelliteService();
        private DispatcherTimer? _timerSat;
        
        // --- Satellite Properties ---
        [ObservableProperty] private double _satLat;
        [ObservableProperty] private double _satLon;
        [ObservableProperty] private double _satAlt;
        [ObservableProperty] private string _satelliteCalculationResult = "";
        [ObservableProperty] private double _timeSpeed = 0;
        
        // --- Manual IGRF Calculation ---
        [ObservableProperty] private double _manualLat;
        [ObservableProperty] private double _manualLon;
        [ObservableProperty] private string _calculationResult = "Enter Lat/Lon and Click Calculate";
        
        // --- Satellite List ---
        [ObservableProperty] private ObservableCollection<SatelliteInfo> _satelliteList = new();
        [ObservableProperty] private SatelliteInfo? _selectedSatellite;

        partial void OnSelectedSatelliteChanged(SatelliteInfo? value)
        {
            if (value != null && !string.IsNullOrEmpty(value.Line1))
            {
                _satService.SetTLE(value.Name, value.Line1, value.Line2);
                _timerSat?.Start();
            }
            else
            {
                _satService.SetTLE("","","");
                _timerSat?.Stop();
            }
        }

        private void InitializeSatellites()
        {
             SatelliteList.Clear();
             SatelliteList.Add(new SatelliteInfo { Name = "-- Manual --", ID = 0 });
             SatelliteList.Add(new SatelliteInfo { Name = "ISS (ZARYA)", Line1 = "1 25544U 98067A   24030.10147156  .00014904  00000-0  27473-3 0  9998", Line2 = "2 25544  51.6414 284.5574 0002475 176.3471 287.7672 15.49357173436989" });
             
             _timerSat = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
             _timerSat.Tick += (s, e) => UpdateSatellitePosition();
             
             SelectedSatellite = SatelliteList[0];
        }

        private void UpdateSatellitePosition()
        {
             var result = _satService.CalculatePosition(DateTime.UtcNow);
             SatLat = result.Lat;
             SatLon = result.Lon;
             SatAlt = result.Alt;
             
             if (IsAutoSetpoint)
             {
                 try
                 {
                     var mag = _calcService.CalculateIGRF(result.Lat, result.Lon, result.Alt, DateTime.UtcNow);
                     SetpointX = mag.X;
                     SetpointY = mag.Y;
                     SetpointZ = mag.Z;
                     
                     var sb = new System.Text.StringBuilder();
                     sb.AppendLine($"--- Real-time IGRF ---");
                     sb.AppendLine($"Lat: {result.Lat:F4}  Lon: {result.Lon:F4}");
                     sb.AppendLine($"Alt: {result.Alt:F2} km");
                     sb.AppendLine($"X: {mag.X:F4} nT");
                     sb.AppendLine($"Y: {mag.Y:F4} nT");
                     sb.AppendLine($"Z: {mag.Z:F4} nT");
                     double total = Math.Sqrt(mag.X*mag.X + mag.Y*mag.Y + mag.Z*mag.Z);
                     sb.AppendLine($"Total: {total:F4} nT");
                     sb.AppendLine($"Time: {DateTime.UtcNow:HH:mm:ss}");
                     
                     SatelliteCalculationResult = sb.ToString();
                 }
                 catch (Exception ex)
                 {
                     SatelliteCalculationResult = $"IGRF Error:\n{ex.Message}\n{ex.StackTrace}";
                     System.Diagnostics.Debug.WriteLine($"IGRF Calc Error: {ex}");
                 }
             }
        }

        [RelayCommand]
        public void CalculateGeoMagnetic()
        {
            try
            {
                var mag = _calcService.CalculateIGRF(ManualLat, ManualLon, 0, DateTime.UtcNow);
                
                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"--- IGRF Calculation ---");
                sb.AppendLine($"Location: {ManualLat:F4}, {ManualLon:F4}");
                sb.AppendLine($"X (North): {mag.X:F2} nT");
                sb.AppendLine($"Y (East): {mag.Y:F2} nT");
                sb.AppendLine($"Z (Down): {mag.Z:F2} nT");
                double total = Math.Sqrt(mag.X*mag.X + mag.Y*mag.Y + mag.Z*mag.Z);
                sb.AppendLine($"Total: {total:F2} nT");
                
                CalculationResult = sb.ToString();
            }
            catch (Exception ex)
            {
                CalculationResult = $"Error: {ex.Message}";
            }
        }
    }
}
