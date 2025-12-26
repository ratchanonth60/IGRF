using System;
using Avalonia.Controls;
using ScottPlot;
using IGRF.Avalonia.Common;
using IGRF.Avalonia.Helpers;

namespace IGRF.Avalonia.Views
{
    public partial class TuningView : UserControl
    {
        // Sensor 1 streamers (primary - solid line)
        private ScottPlot.Plottables.DataStreamer _streamerX1;
        private ScottPlot.Plottables.DataStreamer _streamerY1;
        private ScottPlot.Plottables.DataStreamer _streamerZ1;
        
        // Sensor 2 streamers (dual mode - dashed line)
        private ScottPlot.Plottables.DataStreamer _streamerX2;
        private ScottPlot.Plottables.DataStreamer _streamerY2;
        private ScottPlot.Plottables.DataStreamer _streamerZ2;
        
        // Throttle refresh rate for smooth graph
        private DateTime _lastRefresh = DateTime.MinValue;
        private const int REFRESH_INTERVAL_MS = 50; // 20 Hz max refresh rate
        
        // Flag to pre-fill buffer on first data
        private bool _bufferInitialized = false;

        public TuningView()
        {
            InitializeComponent();
            
            // Setup Plots using helper - Sensor 1 (solid line)
            _streamerX1 = ScottPlotThemeHelper.SetupDataStreamer(PlotX, "Sensor 1", AppConstants.Colors.CyanBlue);
            _streamerY1 = ScottPlotThemeHelper.SetupDataStreamer(PlotY, "Sensor 1", AppConstants.Colors.SkyBlue);
            _streamerZ1 = ScottPlotThemeHelper.SetupDataStreamer(PlotZ, "Sensor 1", AppConstants.Colors.SlateBlue);
            
            // Setup Sensor 2 streamers (different color for dual mode)
            _streamerX2 = ScottPlotThemeHelper.SetupDataStreamer(PlotX, "Sensor 2", AppConstants.Colors.Orange, addAsSecondary: true);
            _streamerY2 = ScottPlotThemeHelper.SetupDataStreamer(PlotY, "Sensor 2", AppConstants.Colors.Orange, addAsSecondary: true);
            _streamerZ2 = ScottPlotThemeHelper.SetupDataStreamer(PlotZ, "Sensor 2", AppConstants.Colors.Orange, addAsSecondary: true);

            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object? sender, EventArgs e)
        {
            if (DataContext is ViewModels.MainViewModel vm)
            {
                vm.DataUpdated += OnDataUpdated;
                vm.DualDataUpdated += OnDualDataUpdated;
            }
        }

        private void OnDataUpdated(double x, double y, double z)
        {
            // Pre-fill buffer with first value so graph starts from position 0
            if (!_bufferInitialized)
            {
                PreFillBuffer(x, y, z, x, y, z);
                _bufferInitialized = true;
            }
            
            // Add new data (sensor 1 only)
            _streamerX1.Add(x);
            _streamerY1.Add(y);
            _streamerZ1.Add(z);
            
            // Add zeros to sensor 2 to keep them in sync
            _streamerX2.Add(0);
            _streamerY2.Add(0);
            _streamerZ2.Add(0);
            
            RefreshPlots();
        }
        
        private void OnDualDataUpdated(double x1, double y1, double z1, double x2, double y2, double z2)
        {
            // Pre-fill buffer with first value so graph starts from position 0
            if (!_bufferInitialized)
            {
                PreFillBuffer(x1, y1, z1, x2, y2, z2);
                _bufferInitialized = true;
            }
            
            // Add data from both sensors
            _streamerX1.Add(x1);
            _streamerY1.Add(y1);
            _streamerZ1.Add(z1);
            
            _streamerX2.Add(x2);
            _streamerY2.Add(y2);
            _streamerZ2.Add(z2);
            
            RefreshPlots();
        }
        
        private void PreFillBuffer(double x1, double y1, double z1, double x2, double y2, double z2)
        {
            int capacity = AppConstants.PlotSettings.DataStreamerCapacity;
            for (int i = 0; i < capacity - 1; i++)
            {
                _streamerX1.Add(x1);
                _streamerY1.Add(y1);
                _streamerZ1.Add(z1);
                _streamerX2.Add(x2);
                _streamerY2.Add(y2);
                _streamerZ2.Add(z2);
            }
        }
        
        private void RefreshPlots()
        {
            var now = DateTime.Now;
            if ((now - _lastRefresh).TotalMilliseconds >= REFRESH_INTERVAL_MS)
            {
                _lastRefresh = now;
                PlotX.Refresh();
                PlotY.Refresh();
                PlotZ.Refresh();
            }
        }
    }
}
