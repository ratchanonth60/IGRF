using System;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace IGRF.Avalonia.ViewModels
{
    /// <summary>
    /// MainViewModel - Map Loading Logic
    /// Contains geomagnetic map loading and visualization
    /// </summary>
    public partial class MainViewModel
    {
        // --- Map Loading (OxyPlot) ---
        [ObservableProperty] private OxyPlot.PlotModel? _mapPlotModel;
        private double[,]? _intensityGridData;
        [ObservableProperty] private string _mapLoadStatus = "No map loaded";
        
        [RelayCommand]
        public async Task LoadMapData()
        {
            try
            {
                var dialog = new global::Avalonia.Platform.Storage.FilePickerOpenOptions
                {
                    Title = "Select Geomagnetic Grid Data",
                    AllowMultiple = false,
                    FileTypeFilter = new[]
                    {
                        new global::Avalonia.Platform.Storage.FilePickerFileType("Text Files")
                        {
                            Patterns = new[] { "*.txt" },
                            MimeTypes = new[] { "text/plain" }
                        },
                        new global::Avalonia.Platform.Storage.FilePickerFileType("All Files")
                        {
                            Patterns = new[] { "*.*" }
                        }
                    }
                };
                
                var window = global::Avalonia.Application.Current?.ApplicationLifetime is global::Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                    ? desktop.MainWindow
                    : null;
                
                if (window == null)
                {
                    MapLoadStatus = "Error: Main window not found";
                    return;
                }
                
                var files = await window.StorageProvider.OpenFilePickerAsync(dialog);
                
                if (files == null || files.Count == 0)
                {
                    MapLoadStatus = "File selection cancelled";
                    return;
                }
                
                MapLoadStatus = "Loading map data...";
                
                var filePath = files[0].Path.LocalPath;
                
                await Task.Run(() =>
                {
                    string[] lines = System.IO.File.ReadAllLines(filePath);
                    int fullRows = lines.Length;
                    
                    int step = 2;
                    int newRows = 180 / step;
                    int newCols = 360 / step;
                    
                    var intensityData = new double[newCols, newRows];
                    
                    System.Threading.Tasks.Parallel.For(0, newRows, i =>
                    {
                        int originalLatIndex = i * step;
                        if (originalLatIndex >= fullRows) return;
                        
                        string line = lines[originalLatIndex];
                        var parts = line.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                        
                        for (int j = 0; j < newCols; j++)
                        {
                            int originalLonIndex = j * step;
                            if (originalLonIndex >= parts.Length) break;
                            
                            if (double.TryParse(parts[originalLonIndex], out double val))
                            {
                                intensityData[j, i] = val;
                            }
                        }
                    });
                    
                    _intensityGridData = intensityData;
                });
                
                Dispatcher.UIThread.Post(() =>
                {
                    MapLoadStatus = "Map loaded - showing contours";
                });
                
                LogStatus = "Map data loaded and contour rendered";
            }
            catch (Exception ex)
            {
                MapLoadStatus = $"Error: {ex.Message}";
                LogStatus = $"Map load failed: {ex.Message}";
            }
        }
        
        private void CreateMapPlot()
        {
            if (_intensityGridData == null) return;
            
            int step = 2;
            int rows = 180 / step;
            int cols = 360 / step;
            
            double[] lats = new double[rows];
            double[] lons = new double[cols];
            
            for (int i = 0; i < rows; i++) lats[i] = -90 + (i * step);
            for (int j = 0; j < cols; j++) lons[j] = -180 + (j * step);
            
            var model = new OxyPlot.PlotModel
            {
                Title = "Geomagnetic Field Map",
                TitleColor = OxyPlot.OxyColors.White,
                PlotAreaBorderColor = OxyPlot.OxyColors.Gray,
                Background = OxyPlot.OxyColors.Black,
                TextColor = OxyPlot.OxyColors.White
            };
            
            model.Axes.Add(new OxyPlot.Axes.LinearAxis
            {
                Position = OxyPlot.Axes.AxisPosition.Bottom,
                Title = "Longitude",
                TitleColor = OxyPlot.OxyColors.White,
                TicklineColor = OxyPlot.OxyColors.Gray,
                TextColor = OxyPlot.OxyColors.White
            });
            model.Axes.Add(new OxyPlot.Axes.LinearAxis
            {
                Position = OxyPlot.Axes.AxisPosition.Left,
                Title = "Latitude",
                TitleColor = OxyPlot.OxyColors.White,
                TicklineColor = OxyPlot.OxyColors.Gray,
                TextColor = OxyPlot.OxyColors.White
            });
            
            var contourSeries = new OxyPlot.Series.ContourSeries
            {
                ColumnCoordinates = lons,
                RowCoordinates = lats,
                Data = _intensityGridData,
                ContourLevelStep = 2000,
                LabelStep = 2,
                StrokeThickness = 1.5,
                LineStyle = OxyPlot.LineStyle.Solid,
                Color = OxyPlot.OxyColors.Cyan,
                LabelFormatString = "{0:0}"
            };
            
            model.Series.Add(contourSeries);
            MapPlotModel = model;
        }
        
        public double[,]? GetMapData() => _intensityGridData;
    }
}
