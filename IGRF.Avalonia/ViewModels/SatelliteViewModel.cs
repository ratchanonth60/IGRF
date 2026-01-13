using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IGRF_Interface.Core.Services;
using IGRF_Interface.Core.Interfaces;

namespace IGRF.Avalonia.ViewModels
{
    /// <summary>
    /// ViewModel for Satellite View
    /// Contains satellite tracking and IGRF calculation properties
    /// </summary>
    public partial class SatelliteViewModel : ViewModelBase
    {
        private readonly ISatelliteCacheService _cacheService;

        // Satellite list
        [ObservableProperty] private ObservableCollection<SatelliteInfo> _satellites = new();
        [ObservableProperty] private SatelliteInfo? _selectedSatellite;

        // Satellite position
        [ObservableProperty] private double _satLat;
        [ObservableProperty] private double _satLon;
        [ObservableProperty] private double _satAlt;

        // IGRF calculated values
        [ObservableProperty] private double _igrfX;
        [ObservableProperty] private double _igrfY;
        [ObservableProperty] private double _igrfZ;
        [ObservableProperty] private double _igrfTotal;
        [ObservableProperty] private double _igrfDeclination;
        [ObservableProperty] private double _igrfInclination;

        // Map data status
        [ObservableProperty] private string _mapLoadStatus = "Loading...";

        // Map data storage
        private double[,]? _mapData;

        public SatelliteViewModel(ISatelliteCacheService cacheService)
        {
            _cacheService = cacheService;
        }

        /// <summary>
        /// Initialize satellite list from cache or defaults
        /// </summary>
        public async Task InitializeSatellitesAsync()
        {
            var cached = await _cacheService.LoadSatellitesAsync();
            if (cached.Count > 0)
            {
                foreach (var sat in cached)
                    Satellites.Add(sat);
            }
            else
            {
                foreach (var sat in _cacheService.GetDefaultSatellites())
                    Satellites.Add(sat);
            }

            if (Satellites.Count > 0)
                SelectedSatellite = Satellites[0];
        }

        /// <summary>
        /// Update satellite position
        /// </summary>
        public void UpdatePosition(double lat, double lon, double alt)
        {
            SatLat = lat;
            SatLon = lon;
            SatAlt = alt;
        }

        /// <summary>
        /// Update IGRF calculated values
        /// </summary>
        public void UpdateIgrfValues(double x, double y, double z, double total, double dec, double inc)
        {
            IgrfX = x;
            IgrfY = y;
            IgrfZ = z;
            IgrfTotal = total;
            IgrfDeclination = dec;
            IgrfInclination = inc;
        }

        /// <summary>
        /// Set map data for contour rendering
        /// </summary>
        public void SetMapData(double[,] data)
        {
            _mapData = data;
            MapLoadStatus = "Loaded";
        }

        /// <summary>
        /// Get map data for rendering
        /// </summary>
        public double[,]? GetMapData() => _mapData;

        [RelayCommand]
        private async Task RefreshSatellitesAsync()
        {
            // Reload from cache
            Satellites.Clear();
            var cached = await _cacheService.LoadSatellitesAsync();
            foreach (var sat in cached.Count > 0 ? cached : _cacheService.GetDefaultSatellites())
                Satellites.Add(sat);
        }
    }
}
