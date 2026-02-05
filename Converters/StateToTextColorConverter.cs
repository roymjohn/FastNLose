using System;
using System.Globalization;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Controls;

namespace FastNLose
{
    public class StateToTextColorConverter : IValueConverter
    {
        // state: 0 yellow, 1 red, 2 green
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int s)
            {
                return s == 1 ? Colors.White : Colors.Black;
            }
            if (int.TryParse(value?.ToString() ?? string.Empty, out var si))
                return si == 1 ? Colors.White : Colors.Black;
            return Colors.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => value;
    }
}
