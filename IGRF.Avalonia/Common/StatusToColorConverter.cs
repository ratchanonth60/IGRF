using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace IGRF.Avalonia.Common
{
    public class StatusToColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string status)
            {
                return status.ToUpper() switch
                {
                    "ON" => new SolidColorBrush(Color.Parse("#32CD32")), // Lime Green
                    "OFF" => new SolidColorBrush(Color.Parse("#808080")), // Gray
                    "ERROR" => new SolidColorBrush(Color.Parse("#FF4500")), // Red Orange
                    _ => new SolidColorBrush(Color.Parse("#808080"))
                };
            }
            return new SolidColorBrush(Color.Parse("#808080"));
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
