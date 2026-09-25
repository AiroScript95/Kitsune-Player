using Microsoft.Extensions.DependencyInjection;
using Project_Kitsune.Services;
using Project_Kitsune.ViewModels;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Windows.Media;

namespace Project_Kitsune
{
    public partial class App : Application
    {
        [LibraryImport("shell32.dll", StringMarshalling = StringMarshalling.Utf16)]
        private static partial int SetCurrentProcessExplicitAppUserModelID(string AppID);

        public event Action? TemaAlterado;

        public event Action? IdiomaAlterado;

        private static readonly string[] IdiomasSuportados = ["pt", "en", "jp", "es"];

        public static ServiceProvider ServiceProvider { get; private set; } = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            _ = SetCurrentProcessExplicitAppUserModelID("ProjectKitsune");
            RenderOptions.ProcessRenderMode = RenderMode.Default;
            var services = new ServiceCollection();
            services.AddSingleton<ConfiguracaoService>();
            services.AddSingleton<DatabaseService>();
            services.AddSingleton<BibliotecaService>();
            services.AddSingleton<AudioPlayerService>();
            services.AddSingleton<PlayerViewModel>();
            services.AddSingleton<ShellViewModel>();
            services.AddSingleton<OrdenacaoService>();
            services.AddSingleton<SmtcService>();
            services.AddSingleton<MainViewModel>();
            ServiceProvider = services.BuildServiceProvider();

            var mainWindow = new Views.MainWindow
            {
                DataContext = ServiceProvider.GetRequiredService<MainViewModel>()
            };
            mainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            var bibliotecaService = ServiceProvider.GetRequiredService<BibliotecaService>();
            bibliotecaService.PararMonitorizacao();

            base.OnExit(e);
        }

        public void AplicarTema(string nomeTema)
        {
            string caminho = $"/Resources/Themes/Colors.{nomeTema}.xaml";
            ResourceDictionary novoTema = new ResourceDictionary() { Source = new Uri(caminho, UriKind.Relative) };
            var temaAntigo = Resources.MergedDictionaries.
                FirstOrDefault(d => d.Source?.OriginalString.Contains("/Resources/Themes/Colors.") == true);
            if (temaAntigo != null) Resources.MergedDictionaries.Remove(temaAntigo);
            Resources.MergedDictionaries.Add(novoTema);

            TemaAlterado?.Invoke();
        }

        public static string DetectarIdiomaSistema()
        {
            string codigo = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            System.Diagnostics.Debug.WriteLine(codigo);
            return IdiomasSuportados.Contains(codigo) ? codigo : "en";
        }

        public void AplicarIdioma(string codigoIdioma)
        {
            string caminho = $"/Resources/Languages/Strings.{codigoIdioma}.xaml";
            ResourceDictionary novoIdioma = new ResourceDictionary() { Source = new Uri(caminho, UriKind.Relative) };

            var idiomaAntigo = Resources.MergedDictionaries.FirstOrDefault(d =>
                d.Source?.OriginalString.Contains("/Resources/Languages/Strings.") == true);

            if (idiomaAntigo != null) Resources.MergedDictionaries.Remove(idiomaAntigo);
            Resources.MergedDictionaries.Add(novoIdioma);

            IdiomaAlterado?.Invoke();
        }
    }
}