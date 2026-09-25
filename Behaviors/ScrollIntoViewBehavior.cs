using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Project_Kitsune.Behaviors
{
    public class ScrollIntoViewBehavior : Behavior<ListBox>
    {
        public static readonly DependencyProperty ScrollToItemProperty =
            DependencyProperty.Register(nameof(ScrollToItem), typeof(object),
                typeof(ScrollIntoViewBehavior), new PropertyMetadata(null, OnScrollToItemChanged));

        public object ScrollToItem
        {
            get => GetValue(ScrollToItemProperty);
            set => SetValue(ScrollToItemProperty, value);
        }

        private static void OnScrollToItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ScrollIntoViewBehavior behavior && behavior.AssociatedObject != null && e.NewValue != null)
            {
                behavior.AssociatedObject.Dispatcher.BeginInvoke(() =>
                {
                    var index = behavior.AssociatedObject.Items.IndexOf(e.NewValue);
                    if (index >= 0)
                    {
                        behavior.AssociatedObject.ScrollIntoView(e.NewValue);
                        behavior.AssociatedObject.UpdateLayout();

                        if (behavior.AssociatedObject.ItemContainerGenerator.ContainerFromIndex(index) is ListBoxItem item)
                        {
                            var scrollViewer = GetScrollViewer(behavior.AssociatedObject);
                            if (scrollViewer != null)
                            {
                                item.UpdateLayout();
                                double itemHeight = item.TransformToAncestor(scrollViewer).Transform(new Point(0, 0)).Y;
                                double targetOffset = scrollViewer.VerticalOffset + itemHeight + (item.ActualHeight / 2) - (scrollViewer.ViewportHeight / 2);
                                scrollViewer.ScrollToVerticalOffset(targetOffset);
                            }
                        }
                    }
                }, System.Windows.Threading.DispatcherPriority.Loaded);
            }
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