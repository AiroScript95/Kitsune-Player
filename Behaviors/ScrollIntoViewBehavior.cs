using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Project_Kitsune.Behaviors
{
    public class ScrollGrupoViewBehavior : Behavior<ItemsControl>
    {
        public static readonly DependencyProperty ScrollToItemProperty =
            DependencyProperty.Register(nameof(ScrollToItem), typeof(object),
                typeof(ScrollGrupoViewBehavior), new PropertyMetadata(null, OnScrollToItemChanged));

        public object ScrollToItem
        {
            get => GetValue(ScrollToItemProperty);
            set => SetValue(ScrollToItemProperty, value);
        }

        private static void OnScrollToItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ScrollGrupoViewBehavior behavior && behavior.AssociatedObject != null && e.NewValue != null)
            {
                behavior.AssociatedObject.Dispatcher.BeginInvoke(() =>
                {
                    var index = behavior.AssociatedObject.Items.IndexOf(e.NewValue);
                    if (index >= 0)
                    {
                        var scrollViewer = GetScrollViewer(behavior.AssociatedObject);
                        if (scrollViewer != null)
                        {
                            // Tenta pegar o container visual se já estiver gerado
                            if (behavior.AssociatedObject.ItemContainerGenerator.ContainerFromIndex(index) is FrameworkElement item)
                            {
                                item.UpdateLayout();
                                double itemHeight = item.TransformToAncestor(scrollViewer).Transform(new Point(0, 0)).Y;
                                double targetOffset = scrollViewer.VerticalOffset + itemHeight + (item.ActualHeight / 2) - (scrollViewer.ViewportHeight / 2);
                                scrollViewer.ScrollToVerticalOffset(targetOffset);
                            }
                            else
                            {
                                // Se estiver virtualizado, força o painel a trazer para a vista pelo índice
                                if (ItemContainerGeneratorHelper(behavior.AssociatedObject, index) is FrameworkElement generatedItem)
                                {
                                    generatedItem.BringIntoView();
                                }
                                else
                                {
                                    // Fallback por estimativa de linha (cada linha tem aprox 132px com margens)
                                    int itensPorLinha = Math.Max(1, (int)(scrollViewer.ViewportWidth / 332));
                                    int linha = index / itensPorLinha;
                                    scrollViewer.ScrollToVerticalOffset(linha * 132);
                                }
                            }
                        }
                    }
                }, System.Windows.Threading.DispatcherPriority.Loaded);
            }
        }

        private static FrameworkElement? ItemContainerGeneratorHelper(ItemsControl itemsControl, int index)
        {
            itemsControl.UpdateLayout();
            return itemsControl.ItemContainerGenerator.ContainerFromIndex(index) as FrameworkElement;
        }

        private static ScrollViewer GetScrollViewer(DependencyObject obj)
        {
            if (obj is ScrollViewer sv) return sv;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                var child = VisualTreeHelper.GetChild(obj, i);
                var result = GetScrollViewer(child);
                if (result != null) return result;
            }
            return null!;
        }
    }
}