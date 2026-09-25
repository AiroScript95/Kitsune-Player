using Project_Kitsune.Models;
using System;
using System.Globalization;
using System.Windows.Data;

namespace Project_Kitsune.Converters
{
    public class MusicaAtualConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2)
                return false;

            if (values[0] is Music musicaDoItem && values[1] is Music musicaAtual)
            {
                return string.Equals(musicaDoItem.Caminho, musicaAtual.Caminho, StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}