using System;
using System.Globalization;
using System.Reflection;
using System.Windows.Data;

namespace Project_Kitsune.Converters
{
    public class StringToUriConverter : IValueConverter
    {
        // converte strings relativos "/Resources/..." para pack URI absoluto
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not string s || string.IsNullOrWhiteSpace(s))
                return string.Empty;

            // se já for um pack:// ou esquema absoluto, retorna diretamente
            if (s.StartsWith("pack://", StringComparison.OrdinalIgnoreCase) ||
                Uri.TryCreate(s, UriKind.Absolute, out _))
            {
                return new Uri(s, UriKind.RelativeOrAbsolute);
            }

            // converte "/Resources/..." ou "Resources/..." em pack://application:,,,/Assembly;component/...
            var assemblyName = Assembly.GetEntryAssembly()?.GetName().Name ?? Assembly.GetExecutingAssembly().GetName().Name;
            var normalized = s.StartsWith("/") ? s : "/" + s;
            var pack = $"pack://application:,,,/{assemblyName};component{normalized}";
            return new Uri(pack, UriKind.Absolute);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}