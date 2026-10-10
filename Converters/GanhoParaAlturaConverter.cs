using System.Globalization;
using System.Windows.Data;

namespace Project_Kitsune.Converters
{
    public class GanhoParaAlturaConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[0] is not double valor || values[1] is not double maximo ||
                values[2] is not double altura || maximo <= 0)
                return 0.0;

            double parte = (string?)parameter == "cima" ? Math.Max(valor, 0) : Math.Max(-valor, 0);
            return (parte / maximo) * (altura / 2);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}