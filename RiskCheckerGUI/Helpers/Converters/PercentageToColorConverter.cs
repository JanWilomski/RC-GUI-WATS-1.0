using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace RiskCheckerGUI.Helpers.Converters
{
    public class PercentageToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double percentage)
            {
                if (percentage < 75)
                    return new SolidColorBrush(Colors.LightGreen);
                else if (percentage < 90)
                    return new SolidColorBrush(Colors.Yellow);
                else
                    return new SolidColorBrush(Colors.Red);
            }
            
            return new SolidColorBrush(Colors.Transparent);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}