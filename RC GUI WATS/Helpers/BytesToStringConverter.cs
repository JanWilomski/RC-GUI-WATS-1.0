using System;
using System.Globalization;
using System.Text;
using System.Windows.Data;

namespace RiskCheckerGUI.Helpers
{
    public class BytesToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is byte[] bytes)
            {
                try
                {
                    return Encoding.ASCII.GetString(bytes);
                }
                catch
                {
                    return BitConverter.ToString(bytes);
                }
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}