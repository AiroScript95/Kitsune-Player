using Microsoft.Extensions.DependencyInjection;
using Project_Kitsune.Models;
using Project_Kitsune.Services;
using Project_Kitsune.ViewModels.ShellPages;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Project_Kitsune.Views.ShellPages
{
    /// <summary>
    /// Interaction logic for ConfigTemaPage.xaml
    /// </summary>
    public partial class ConfigTemaPage : UserControl
    {
        protected ConfiguracaoService configuracaoService;

        public ConfigTemaPage()
        {
            configuracaoService = App.ServiceProvider.GetRequiredService<ConfiguracaoService>();
            InitializeComponent();
            Configuracao configuracao = configuracaoService.CarregarConfiguracao();
            if (configuracao.FormatoLista == "List") RadioList.IsChecked = true;
            else RadioGrid.IsChecked = true;
        }

        private void TemaSelect_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton radio && radio.IsChecked == true)
            {
                string criterio = radio.Name switch
                {
                    "RadioAsa" => "Asa",
                    "RadioKuko" => "Kuko",
                    "RadioNogitsune" => "Nogitsune",
                    "RadioReiko" => "Reiko",
                    "RadioTenko" => "Tenko",
                    "RadioYako" => "Yako",
                    "RadioYoru" => "Yoru",
                    "RadioZenko" => "Zenko",
                    _ => "Asa"
                };

                if (DataContext is ConfigTemaPageViewModel viewModel)
                {
                    viewModel.TemaSelecionado = criterio;
                }
            }
        }

        private void BorderSelect_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton radio && radio.IsChecked == true)
            {
                string criterio = radio.Name switch
                {
                    "RadioNormal" => "Normal",
                    "RadioForte" => "Forte",
                    "RadioColor" => "Color",
                    "RadioColor2" => "Color2",
                    "RadioColor3" => "Color3",
                    _ => "Normal"
                };

                if (DataContext is ConfigTemaPageViewModel viewModel)
                {
                    viewModel.MudarBorder(criterio);
                }
            }
        }

        private void FormatoSelect_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton radio && radio.IsChecked == true)
            {
                string formato = radio.Name switch
                {
                    "RadioList" => "List",
                    "RadioGrid" => "Grid",
                    _ => "List"
                };
                if (DataContext is ConfigTemaPageViewModel viewModel)
                {
                    viewModel.MudarFormato(formato);
                }
            }
        }
    }
}