using System;
using System.Collections.Generic;
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
                // Fix: Trim strings to avoid parsing errors
                _satService.SetTLE(value.Name.Trim(), value.Line1.Trim(), value.Line2.Trim());
                _timerSat?.Start();
            }
            else
            {
                _satService.SetTLE("","","");
                _timerSat?.Stop();
            }
        }

        // Cache Service
        private readonly SatelliteCacheService _satCacheService = new SatelliteCacheService();

        private async void InitializeSatellites()
        {
             SatelliteList.Clear();
             
             // Load from cache
             var cachedSats = await _satCacheService.LoadSatellitesAsync();
             
             if (cachedSats.Count == 0)
             {
                 // No cache -> Load defaults
                 cachedSats = _satCacheService.GetDefaultSatellites();
                 await _satCacheService.SaveSatellitesAsync(cachedSats);
             }
             else
             {
                 // Patch: Enforce fresh TLEs for defaults (in case cache is old/broken)
                 var defaults = _satCacheService.GetDefaultSatellites();
                 foreach (var def in defaults)
                 {
                     if (def.ID == "0") continue; // Skip Manual
                     
                     var existing = cachedSats.Find(s => s.ID == def.ID);
                     if (existing != null)
                     {
                         existing.Line1 = def.Line1;
                         existing.Line2 = def.Line2;
                         existing.Name = def.Name;
                     }
                     else
                     {
                         cachedSats.Add(def);
                     }
                 }
                 await _satCacheService.SaveSatellitesAsync(cachedSats);
             }
             
             foreach (var sat in cachedSats)
             {
                 SatelliteList.Add(sat);
             }
             
             _timerSat = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
             _timerSat.Tick += (s, e) => UpdateSatellitePosition();
             
             if (SatelliteList.Count > 0)
                 SelectedSatellite = SatelliteList[0];
        }

        // Simulation State
        private DateTime _simulationTime = DateTime.UtcNow;
        private DateTime _lastUpdate = DateTime.UtcNow;

        private void UpdateSatellitePosition()
        {
             var now = DateTime.UtcNow;
             double dt = (now - _lastUpdate).TotalSeconds;
             _lastUpdate = now;

             // Logic: If TimeSpeed is 0 or 1, sync with real-time. If > 1, simulate faster.
             if (TimeSpeed <= 1)
             {
                 _simulationTime = DateTime.UtcNow;
             }
             else
             {
                 _simulationTime = _simulationTime.AddSeconds(dt * TimeSpeed);
             }

             var result = _satService.CalculatePosition(_simulationTime);
             SatLat = result.Lat;
             SatLon = result.Lon;
             SatAlt = result.Alt;
             
             // Send to 3D Globe
             _ = _globePipeService.SendSatellitePositionAsync(result.Lat, result.Lon, result.Alt);
             
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
        
        // --- TLE Management Properties ---
        [ObservableProperty] private string _satelliteNoradId = "";
        [ObservableProperty] private string _tleFetchStatus = "";
        [ObservableProperty] private string _manualTleName = "";
        [ObservableProperty] private string _manualTleLine1 = "";
        [ObservableProperty] private string _manualTleLine2 = "";
        
        // --- Space-Track.org Credentials ---
        [ObservableProperty] private string _spaceTrackUsername = "ratchanonth60@gmail.com";
        [ObservableProperty] private string _spaceTrackPassword = "g7tHUcc74JbCZDH";
        [ObservableProperty] private bool _isSpaceTrackLoggedIn = false;
        
        private readonly SpaceTrackService _stService = new SpaceTrackService();
        
        /// <summary>
        /// Login to Space-Track.org and fetch TLE data
        /// </summary>
        [RelayCommand]
        private async System.Threading.Tasks.Task FetchTleFromSpaceTrack()
        {
            if (string.IsNullOrWhiteSpace(SatelliteNoradId))
            {
                TleFetchStatus = "Please enter a NORAD ID (Numeric or Alpha-5)";
                return;
            }
            
            // Login if needed
            if (!_stService.IsLoggedIn)
            {
                if (string.IsNullOrWhiteSpace(SpaceTrackUsername) || string.IsNullOrWhiteSpace(SpaceTrackPassword))
                {
                    TleFetchStatus = "Please enter Space-Track credentials";
                    return;
                }
                
                TleFetchStatus = "Logging in to Space-Track.org...";
                bool success = await _stService.LoginAsync(SpaceTrackUsername, SpaceTrackPassword);
                
                IsSpaceTrackLoggedIn = success;
                if (!success)
                {
                    TleFetchStatus = _stService.LastStatus;
                    return;
                }
            }
            
            TleFetchStatus = $"Fetching TLE for {SatelliteNoradId}...";
            
            var newSat = await _stService.FetchTleAsync(SatelliteNoradId);
            
            if (newSat != null)
            {
                // Update or Add
                bool found = false;
                for (int i = 0; i < SatelliteList.Count; i++)
                {
                    if (SatelliteList[i].ID == newSat.ID || SatelliteList[i].Name == newSat.Name)
                    {
                        SatelliteList[i] = newSat;
                        found = true;
                        break;
                    }
                }
                
                if (!found) SatelliteList.Add(newSat);
                
                SelectedSatellite = newSat;
                TleFetchStatus = $"✓ Found: {newSat.Name}";
                
                await _satCacheService.SaveSatellitesAsync(SatelliteList);
            }
            else
            {
                TleFetchStatus = _stService.LastStatus;
            }
        }
        
        /// <summary>
        /// Add a satellite manually with TLE data
        /// </summary>
        [RelayCommand]
        private async System.Threading.Tasks.Task AddManualSatellite()
        {
            if (string.IsNullOrWhiteSpace(ManualTleName) || 
                string.IsNullOrWhiteSpace(ManualTleLine1) || 
                string.IsNullOrWhiteSpace(ManualTleLine2))
            {
                TleFetchStatus = "Please fill all TLE fields";
                return;
            }
            
            // Validate TLE format
            if (!ManualTleLine1.StartsWith("1 ") || !ManualTleLine2.StartsWith("2 "))
            {
                TleFetchStatus = "Invalid TLE format (must start with 1/2)";
                return;
            }
            
            var newSat = new SatelliteInfo 
            { 
                Name = ManualTleName.Trim(), 
                ID = "MANUAL" + new Random().Next(1000,9999),
                Line1 = ManualTleLine1.Trim(), 
                Line2 = ManualTleLine2.Trim()
            };
            
            SatelliteList.Add(newSat);
            SelectedSatellite = newSat;
            
            // Clear inputs
            ManualTleName = "";
            ManualTleLine1 = "";
            ManualTleLine2 = "";
            
            TleFetchStatus = $"✓ Added: {newSat.Name}";

            await _satCacheService.SaveSatellitesAsync(SatelliteList);
        }
    }
}
