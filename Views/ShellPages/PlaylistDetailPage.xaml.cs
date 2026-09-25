using Project_Kitsune.Models;
using Project_Kitsune.ViewModels.ShellPages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Project_Kitsune.Views.ShellPages
{
    /// <summary>
    /// Interaction logic for PlaylistDetailPage.xaml
    /// </summary>
    public partial class PlaylistDetailPage : UserControl
    {
        public PlaylistDetailPage()
        {
            InitializeComponent();
        }

        private void StackPanel_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement elemento && elemento.DataContext is Music musica)
            {
                if (DataContext is PlaylistDetailPageViewModel viewModel)
                {
                    viewModel.TocarMusica(musica);
                }
            }
        }

        /* MUSICA EVENTS*/

        private void ConfirmarSelecao_Click(object sender, RoutedEventArgs e)
        {
            List<Music> selecionadas = new List<Music>();
            foreach (object item in ListaTodasMusicas.SelectedItems)
            {
                if (item is Music musica)
                {
                    selecionadas.Add(musica);
                }
            }

            if (DataContext is PlaylistDetailPageViewModel viewModel)
            {
                viewModel.AdicionarMusicaEscolhida(selecionadas);
            }
        }

        private void MenuMusica_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.ContextMenu != null)
            {
                btn.ContextMenu.PlacementTarget = btn;
                btn.ContextMenu.IsOpen = true;
            }
        }

        /* SLIDER EVENTS*/

        private void Slider_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount > 1) return;

            if (sender is Slider slider && DataContext is PlaylistDetailPageViewModel viewModel)
            {
                viewModel.Player.EstaArrastando = true;

                bool clicouNoThumb = FindParent<Thumb>((DependencyObject)e.OriginalSource) != null;
                if (clicouNoThumb) return; //

                Point posicaoClique = e.GetPosition(slider);
                double proporcao = posicaoClique.X / slider.ActualWidth;
                double novoValor = proporcao * slider.Maximum;
                slider.Value = Math.Max(slider.Minimum, Math.Min(slider.Maximum, novoValor));
                e.Handled = true;
            }
        }

        private static T? FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject? parentObject = VisualTreeHelper.GetParent(child);
            if (parentObject == null) return null;
            if (parentObject is T parent) return parent;
            return FindParent<T>(parentObject);
        }

        private void Slider_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is PlaylistDetailPageViewModel viewModel)
            {
                viewModel.Player.DefinirPosicaoManual((long)viewModel.Player.PosicaoAtualMs);
                viewModel.Player.EstaArrastando = false;
            }
        }

        private void ListaTodasMusicas_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is PlaylistDetailPageViewModel vm)
            {
                vm.NumeroSelecionadas = ListaTodasMusicas.SelectedItems.Count;
            }
        }
    }
}