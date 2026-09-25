using Microsoft.Extensions.DependencyInjection;
using Project_Kitsune.Helpers;
using Project_Kitsune.Models;
using Project_Kitsune.Services;
using Project_Kitsune.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Project_Kitsune.Views
{
    /// <summary>
    /// Lógica interna para EditarMusicaWindow.xaml
    /// </summary>
    public partial class EditarMusicaWindow : Window
    {
        private readonly ShellViewModel _shellViewModel;

        public EditarMusicaWindow(Music musica, BibliotecaService biblioteca)
        {
            _shellViewModel = App.ServiceProvider.GetRequiredService<ShellViewModel>();
            _shellViewModel.PropertyChanged += ShellViewModel_PropertyChanged;
            ((App)Application.Current).TemaAlterado += AtualizarCorBordaNativa;
            ((App)Application.Current).TemaAlterado += AtualizarCorTitle;

            InitializeComponent();
            var vm = new EditarMusicaViewModel(musica, biblioteca);
            vm.FechadoComSucesso += () => { DialogResult = true; Close(); };
            DataContext = vm;

            this.Closing += EditarMusicaWindow_Closing;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            DwmHelper.AtivarCantosArredondados(this);
            AtualizarCorBordaNativa();
            AtualizarCorTitle();
        }

        private void AtualizarCorTitle()
        {
            if (Application.Current.Resources["SurfaceBrush"] is SolidColorBrush brush)
            {
                DwmHelper.DefinirCorTitleBar(this, brush.Color);
            }
        }

        private void AtualizarCorBordaNativa()
        {
            string chaveRecurso = _shellViewModel.Border switch
            {
                "Normal" => "BorderBrush2",
                "Color" => "AccentBrush",
                "Color2" => "AccentAltBrush",
                "Color3" => "AccentTertiaryBrush",
                _ => "BorderBrush2"
            };

            if (TryFindResource(chaveRecurso) is SolidColorBrush brush)
            {
                DwmHelper.DefinirCorBorda(this, brush.Color);
            }
        }

        private void ShellViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ShellViewModel.Border))
                AtualizarCorBordaNativa();
        }

        private void EditarMusicaWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            _shellViewModel.PropertyChanged -= ShellViewModel_PropertyChanged;
            ((App)Application.Current).TemaAlterado -= AtualizarCorBordaNativa;
            ((App)Application.Current).TemaAlterado -= AtualizarCorTitle;
        }

        private void Cancelar_Click(object sender, RoutedEventArgs e) => Close();
    }
}