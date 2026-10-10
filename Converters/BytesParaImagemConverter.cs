using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace Project_Kitsune.Converters
{
    public class BytesParaImagemConverter : IValueConverter
    {
        private const int LarguraPadrao = 600;

        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not byte[] bytes || bytes.Length == 0) return null;

            int largura = int.TryParse(parameter?.ToString(), out int l) ? l : LarguraPadrao;

            var image = new BitmapImage();
            using var stream = new MemoryStream(bytes);
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.DecodePixelWidth = largura;
            image.StreamSource = stream;
            image.EndInit();
            image.Freeze();
            return image;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}