using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace FastNLose
{
    public class NullToSizeConverter : IValueConverter
    {
        // parameter format: "primary:default" e.g. "80:24"
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var param = parameter as string ?? "80:24";
            var parts = param.Split(':');
            double primary = 80.0, def = 24.0;
            if (parts.Length == 2)
            {
                double.TryParse(parts[0], out primary);
                double.TryParse(parts[1], out def);
            }
            return value == null ? def : primary;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => value;
    }
}
