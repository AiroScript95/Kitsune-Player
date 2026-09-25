using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Project_Kitsune.Converters
{
    public class ProgressoParaLarguraConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 3) return 0.0;

            // proteger UnsetValue
            if (values[0] == DependencyProperty.UnsetValue ||
                values[1] == DependencyProperty.UnsetValue ||
                values[2] == DependencyProperty.UnsetValue)
                return 0.0;

            double valor = SafeToDouble(values[0]);
            double maximo = SafeToDouble(values[1]);
            double larguraTotal = SafeToDouble(values[2]);

            if (maximo <= 0 || larguraTotal <= 0) return 0.0;

            double proporcao = valor / maximo;
            if (double.IsNaN(proporcao) || double.IsInfinity(proporcao)) return 0.0;
            proporcao = Math.Max(0.0, Math.Min(1.0, proporcao));

            return proporcao * larguraTotal;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();

        // Mínimo e seguro: trata UnsetValue, null e tipos numéricos comuns
        private static double SafeToDouble(object o)
        {
            if (o == null) return 0.0;
            if (o == DependencyProperty.UnsetValue) return 0.0;

            try
            {
                if (o is double d) return d;
                if (o is float f) return (double)f;
                if (o is int i) return i;
                if (o is long l) return l;
                if (o is decimal m) return (double)m;
                if (o is short s) return s;
                if (o is byte b) return b;

                if (o is IConvertible conv)
                    return conv.ToDouble(CultureInfo.InvariantCulture);

                // evitar ambiguidade com o método Convert da classe; qualificar System.Convert
                var str = System.Convert.ToString(o, CultureInfo.InvariantCulture);
                if (double.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
                    return parsed;
            }
            catch
            {
            }

            return 0.0;
        }
    }
}