using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Project_Kitsune.Converters
{
    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool ehNuloOuVazio = value == null || (value is byte[] bytes && bytes.Length == 0);
            bool inverter = parameter as string == "Invert";

            bool mostrar = inverter ? !ehNuloOuVazio : ehNuloOuVazio;
            return mostrar ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}