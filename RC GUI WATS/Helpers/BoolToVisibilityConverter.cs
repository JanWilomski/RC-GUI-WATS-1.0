using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace RiskCheckerGUI.Helpers.Converters
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool visible)
            {
                if (parameter is string param && param == "Inverse")
                    visible = !visible;
                
                return visible ? Visibility.Visible : Visibility.Collapsed;
            }
            
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                bool result = visibility == Visibility.Visible;
                
                if (parameter is string param && param == "Inverse")
                    result = !result;
                
                return result;
            }
            
            return true;
        }
    }
}