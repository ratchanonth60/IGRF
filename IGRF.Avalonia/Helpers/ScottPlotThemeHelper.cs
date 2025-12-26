using ScottPlot;
using ScottPlot.Avalonia;

namespace IGRF.Avalonia.Helpers
{
    /// <summary>
    /// Helper class for configuring ScottPlot dark theme consistently across views
    /// </summary>
    public static class ScottPlotThemeHelper
    {
        /// <summary>
        /// Applies dark theme to a ScottPlot Plot
        /// </summary>
        public static void ApplyDarkTheme(Plot plot)
        {
            var darkColor = Color.FromHex(Common.AppConstants.Colors.DarkBackground);
            plot.FigureBackground.Color = darkColor;
            plot.DataBackground.Color = darkColor;
            plot.Axes.Color(Color.FromHex(Common.AppConstants.Colors.AxisColor));
            plot.Grid.MajorLineColor = Color.FromHex(Common.AppConstants.Colors.GridColor);
        }

        /// <summary>
        /// Creates and styles a satellite marker scatter plot
        /// </summary>
        public static ScottPlot.Plottables.Scatter CreateSatelliteMarker(Plot plot, double lon, double lat)
        {
            var marker = plot.Add.Scatter(new double[] { lon }, new double[] { lat });
            marker.Color = Color.FromHex(Common.AppConstants.Colors.OrangeSatellite);
            marker.MarkerSize = Common.AppConstants.PlotSettings.SatelliteMarkerSize;
            marker.MarkerShape = MarkerShape.FilledCircle;
            return marker;
        }

        /// <summary>
        /// Sets up a data streamer plot with dark theme and custom color
        /// </summary>
        /// <param name="avaPlot">The AvaPlot control</param>
        /// <param name="title">Title for the plot (only applied for primary streamer)</param>
        /// <param name="colorHex">Hex color for the line</param>
        /// <param name="addAsSecondary">If true, adds as secondary line without changing title/theme</param>
        public static ScottPlot.Plottables.DataStreamer SetupDataStreamer(
            AvaPlot avaPlot, 
            string title, 
            string colorHex,
            bool addAsSecondary = false)
        {
            var plot = avaPlot.Plot;
            
            if (!addAsSecondary)
            {
                // Apply dark theme and title only for primary streamer
                ApplyDarkTheme(plot);
                plot.Title(title);
                plot.Axes.Title.Label.ForeColor = Color.FromHex(colorHex);
            }
            
            // Create streamer with realtime scrolling
            var streamer = plot.Add.DataStreamer(Common.AppConstants.PlotSettings.DataStreamerCapacity);
            streamer.Color = Color.FromHex(colorHex);
            
            // Secondary streamer gets thinner, dashed-like appearance
            streamer.LineWidth = addAsSecondary 
                ? Common.AppConstants.PlotSettings.DefaultLineWidth 
                : Common.AppConstants.PlotSettings.DefaultLineWidthBold;
            
            // Realtime scrolling mode - data scrolls left when buffer fills
            streamer.ViewScrollLeft();
            
            // Auto-scale Y-axis to fit data including negative values
            streamer.ManageAxisLimits = true;
            
            if (!addAsSecondary)
            {
                avaPlot.Refresh();
            }
            
            return streamer;
        }
    }
}
