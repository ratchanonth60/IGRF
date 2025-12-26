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
        
        // --- TLE Management Properties ---
        [ObservableProperty] private string _satelliteNoradId = "";
        [ObservableProperty] private string _tleFetchStatus = "";
        [ObservableProperty] private string _manualTleName = "";
        [ObservableProperty] private string _manualTleLine1 = "";
        [ObservableProperty] private string _manualTleLine2 = "";
        
        // --- Space-Track.org Credentials ---
        [ObservableProperty] private string _spaceTrackUsername = "";
        [ObservableProperty] private string _spaceTrackPassword = "";
        [ObservableProperty] private bool _isSpaceTrackLoggedIn = false;
        
        private System.Net.Http.HttpClient? _spaceTrackClient;
        private System.Net.CookieContainer? _cookieContainer;
        
        /// <summary>
        /// Login to Space-Track.org and fetch TLE data
        /// </summary>
        [RelayCommand]
        private async System.Threading.Tasks.Task FetchTleFromSpaceTrack()
        {
            if (string.IsNullOrWhiteSpace(SatelliteNoradId))
            {
                TleFetchStatus = "Please enter a NORAD ID";
                return;
            }
            
            if (string.IsNullOrWhiteSpace(SpaceTrackUsername) || string.IsNullOrWhiteSpace(SpaceTrackPassword))
            {
                TleFetchStatus = "Please enter Space-Track credentials";
                return;
            }
            
            TleFetchStatus = "Logging in to Space-Track.org...";
            
            try
            {
                // Setup HttpClient with cookie support
                if (_cookieContainer == null)
                {
                    _cookieContainer = new System.Net.CookieContainer();
                }
                
                var handler = new System.Net.Http.HttpClientHandler
                {
                    CookieContainer = _cookieContainer,
                    UseCookies = true
                };
                
                _spaceTrackClient = new System.Net.Http.HttpClient(handler);
                _spaceTrackClient.Timeout = TimeSpan.FromSeconds(30);
                
                // Login to Space-Track.org
                var loginUrl = "https://www.space-track.org/ajaxauth/login";
                var loginContent = new System.Net.Http.FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("identity", SpaceTrackUsername),
                    new KeyValuePair<string, string>("password", SpaceTrackPassword)
                });
                
                var loginResponse = await _spaceTrackClient.PostAsync(loginUrl, loginContent);
                var loginResult = await loginResponse.Content.ReadAsStringAsync();
                
                if (!loginResponse.IsSuccessStatusCode || loginResult.Contains("Failed"))
                {
                    TleFetchStatus = "Login failed. Check credentials.";
                    IsSpaceTrackLoggedIn = false;
                    return;
                }
                
                IsSpaceTrackLoggedIn = true;
                TleFetchStatus = "Fetching TLE data...";
                
                // Fetch TLE using GP class
                string queryUrl = $"https://www.space-track.org/basicspacedata/query/class/gp/norad_cat_id/{SatelliteNoradId}/format/tle";
                
                var response = await _spaceTrackClient.GetStringAsync(queryUrl);
                var lines = response.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                
                if (lines.Length >= 3)
                {
                    string name = lines[0].Trim();
                    string line1 = lines[1].Trim();
                    string line2 = lines[2].Trim();
                    
                    var newSat = new SatelliteInfo 
                    { 
                        Name = name, 
                        Line1 = line1, 
                        Line2 = line2,
                        ID = int.TryParse(SatelliteNoradId, out int id) ? id : 0
                    };
                    
                    // Check if already exists and update or add
                    bool found = false;
                    for (int i = 0; i < SatelliteList.Count; i++)
                    {
                        if (SatelliteList[i].ID == newSat.ID || SatelliteList[i].Name == name)
                        {
                            SatelliteList[i] = newSat;
                            found = true;
                            break;
                        }
                    }
                    
                    if (!found)
                    {
                        SatelliteList.Add(newSat);
                    }
                    
                    SelectedSatellite = newSat;
                    TleFetchStatus = $"✓ {name} (ID: {SatelliteNoradId})";
                }
                else if (lines.Length >= 2)
                {
                    // No name, just TLE lines
                    string line1 = lines[0].Trim();
                    string line2 = lines[1].Trim();
                    
                    var newSat = new SatelliteInfo 
                    { 
                        Name = $"Satellite {SatelliteNoradId}", 
                        Line1 = line1, 
                        Line2 = line2,
                        ID = int.TryParse(SatelliteNoradId, out int id) ? id : 0
                    };
                    
                    SatelliteList.Add(newSat);
                    SelectedSatellite = newSat;
                    TleFetchStatus = $"✓ Added: {newSat.Name}";
                }
                else
                {
                    TleFetchStatus = "No TLE data found for this ID";
                }
            }
            catch (Exception ex)
            {
                TleFetchStatus = $"Error: {ex.Message}";
                IsSpaceTrackLoggedIn = false;
            }
        }
        
        /// <summary>
        /// Add a satellite manually with TLE data
        /// </summary>
        [RelayCommand]
        private void AddManualSatellite()
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
        }
    }
}
