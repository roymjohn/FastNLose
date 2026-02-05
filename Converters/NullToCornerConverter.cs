using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace FastNLose
{
    public class NullToCornerConverter : IValueConverter
    {
        // parameter format: "primary:default" e.g. "12:4"
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var param = parameter as string ?? "12:4";
            var parts = param.Split(':');
            int primary = 12, def = 4;
            if (parts.Length == 2)
            {
                int.TryParse(parts[0], out primary);
                int.TryParse(parts[1], out def);
            }
            return value == null ? def : primary;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => value;
    }
}
