using Avalonia.Controls;
using ScottPlot;
using ScottPlot.Avalonia;
using IGRF.Avalonia.Common;
using IGRF.Avalonia.Helpers;

namespace IGRF.Avalonia.Views
{
    public partial class SatelliteView : UserControl
    {
        private AvaPlot? _mapPlot;
        private ScottPlot.Plottables.Scatter? _satPoint;
        private ScottPlot.Plottables.ContourLines? _contour;

        // Store data arrays to update position without recreating Scatter
        private double[] _satX = new double[] { 0 };
        private double[] _satY = new double[] { 0 };
        private bool _markerInitialized = false;

        public SatelliteView()
        {
            InitializeComponent();
            this.Loaded += SatelliteView_Loaded;
        }

        private void SatelliteView_Loaded(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
        {
            _mapPlot = this.FindControl<AvaPlot>("MapPlot");
            if (_mapPlot != null)
            {
                SetupMap();

                // Support both MainViewModel (legacy) and SatelliteViewModel (new MVVM)
                if (DataContext is ViewModels.SatelliteViewModel satVm)
                {
                    satVm.PropertyChanged += Vm_PropertyChanged;
                }
                else if (DataContext is ViewModels.MainViewModel vm)
                {
                    vm.PropertyChanged += Vm_PropertyChanged;
                }
            }
        }

        private void Vm_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (_mapPlot == null) return;

            // Only listen to SatLon (updated second after SatLat) to avoid double update
            if (e.PropertyName == "SatLon")
            {
                if (DataContext is ViewModels.SatelliteViewModel satVm)
                {
                    UpdateSatellitePosition(satVm.SatLat, satVm.SatLon);
                }
                else if (DataContext is ViewModels.MainViewModel vm)
                {
                    UpdateSatellitePosition(vm.SatLat, vm.SatLon);
                }
            }
            else if (e.PropertyName == "MapLoadStatus")
            {
                if (DataContext is ViewModels.SatelliteViewModel satVm)
                {
                    var mapData = satVm.GetMapData();
                    if (mapData != null)
                    {
                        RenderMapContours(mapData);
                    }
                }
                else if (DataContext is ViewModels.MainViewModel vm)
                {
                    var mapData = vm.GetMapData();
                    if (mapData != null)
                    {
                        RenderMapContours(mapData);
                    }
                }
            }
        }

        private void SetupMap()
        {
            if (_mapPlot == null) return;

            var plot = _mapPlot.Plot;

            // Apply dark theme using helper
            ScottPlotThemeHelper.ApplyDarkTheme(plot);

            // Setup Axis
            plot.Axes.SetLimits(AppConstants.MapSettings.LongitudeMin, AppConstants.MapSettings.LongitudeMax,
                               AppConstants.MapSettings.LatitudeMin, AppConstants.MapSettings.LatitudeMax);
            plot.Axes.Bottom.Label.Text = "Longitude";
            plot.Axes.Left.Label.Text = "Latitude";
            plot.Title("Geomagnetic Field Map");

            // Don't create marker yet - wait for first position update
            _satPoint = null;
            _markerInitialized = false;

            _mapPlot.Refresh();
        }

        private void UpdateSatellitePosition(double lat, double lon)
        {
            if (_mapPlot == null) return;

            // Update the data arrays
            _satX[0] = lon;
            _satY[0] = lat;

            if (!_markerInitialized || _satPoint == null)
            {
                // First time - create the scatter
                _satPoint = _mapPlot.Plot.Add.Scatter(_satX, _satY);
                _satPoint.Color = Color.FromHex(AppConstants.Colors.OrangeSatellite);
                _satPoint.MarkerSize = AppConstants.PlotSettings.SatelliteMarkerSize;
                _satPoint.MarkerShape = MarkerShape.FilledCircle;
                _markerInitialized = true;
            }
            // No else needed - Scatter references the same arrays, so updating arrays updates the plot

            _mapPlot.Refresh();
        }

        private void RenderMapContours(double[,] intensityData)
        {
            if (_mapPlot == null) return;

            int step = AppConstants.MapSettings.GridStep;
            int rows = AppConstants.MapSettings.GridRows;
            int cols = AppConstants.MapSettings.GridCols;

            // Remove old contour if exists
            if (_contour != null)
            {
                _mapPlot.Plot.Remove(_contour);
            }

            // Create coordinate arrays for ContourLines
            double[] xs = new double[cols];
            double[] ys = new double[rows];

            for (int i = 0; i < cols; i++)
                xs[i] = AppConstants.MapSettings.LongitudeMin + (i * step);

            for (int j = 0; j < rows; j++)
                ys[j] = AppConstants.MapSettings.LatitudeMin + (j * step);

            // Create ContourLines using ScottPlot 5 API with 3D coordinates
            var coords = new ScottPlot.Coordinates3d[cols, rows];
            for (int i = 0; i < cols; i++)
            {
                for (int j = 0; j < rows; j++)
                {
                    coords[i, j] = new ScottPlot.Coordinates3d(xs[i], ys[j], intensityData[i, j]);
                }
            }

            _contour = _mapPlot.Plot.Add.ContourLines(coords);
            _contour.LineColor = ScottPlot.Color.FromHex(AppConstants.Colors.CyanBlue);
            _contour.LineWidth = AppConstants.PlotSettings.DefaultLineWidth;
            _contour.Colormap = new ScottPlot.Colormaps.Turbo();

            // Re-add satellite marker on top if already initialized
            if (_markerInitialized && _satPoint != null)
            {
                _mapPlot.Plot.Remove(_satPoint);
                _satPoint = _mapPlot.Plot.Add.Scatter(_satX, _satY);
                _satPoint.Color = Color.FromHex(AppConstants.Colors.OrangeSatellite);
                _satPoint.MarkerSize = AppConstants.PlotSettings.SatelliteMarkerSize;
                _satPoint.MarkerShape = MarkerShape.FilledCircle;
            }

            _mapPlot.Refresh();
        }
    }
}
