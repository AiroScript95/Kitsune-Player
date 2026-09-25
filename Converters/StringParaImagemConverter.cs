using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace Project_Kitsune.Converters
{
    public class StringParaImagemConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string caminho && !string.IsNullOrWhiteSpace(caminho) && File.Exists(caminho))
            {
                BitmapImage imagem = new BitmapImage();
                imagem.BeginInit();
                imagem.CacheOption = BitmapCacheOption.OnLoad;
                imagem.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                imagem.UriSource = new Uri(caminho);
                imagem.EndInit();
                imagem.Freeze();
                return imagem;
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}