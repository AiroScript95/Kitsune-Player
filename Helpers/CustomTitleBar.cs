using System.Windows;
using System.Windows.Interop;
using System.Windows.Shell;

/// <summary>
/// Title bar custom com animações nativas e Snap Layouts (Windows 11).
/// Uso, no construtor da janela, depois do InitializeComponent():
///     CustomTitleBar.Apply(this, MinimizeButton, MaximizeButton, CloseButton);
/// A barra tem de ter 32 de altura (igual ao CaptionHeight abaixo).
/// </summary>
public static class CustomTitleBar
{
    private const int WM_NCLBUTTONDOWN = 0x00A1;
    private const int WM_NCLBUTTONUP = 0x00A2;
    private const int WM_NCHITTEST = 0x0084;
    private const int HTMINBUTTON = 8;
    private const int HTMAXBUTTON = 9;
    private const int HTCLOSE = 20;

    public static void Apply(Window window, FrameworkElement minimize, FrameworkElement maximize, FrameworkElement close)
    {
        // SingleBorderWindow + WindowChrome mantém animações, Aero Snap e Win+setas
        //window.WindowStyle = WindowStyle.SingleBorderWindow;
        WindowChrome.SetWindowChrome(window, new WindowChrome
        {
            CaptionHeight = 32,
            ResizeBorderThickness = new Thickness(6),
            UseAeroCaptionButtons = false,
            GlassFrameThickness = new Thickness(-1),
            CornerRadius = new CornerRadius(1)
        });

        WindowChrome.SetIsHitTestVisibleInChrome(minimize, true);
        WindowChrome.SetIsHitTestVisibleInChrome(maximize, true);
        WindowChrome.SetIsHitTestVisibleInChrome(close, true);

        // Maximizada, a janela ultrapassa o ecrã pela espessura da moldura
        window.StateChanged += (_, _) => AjustarMargem(window);
        window.Loaded += (_, _) => AjustarMargem(window);

        // Responde ao Windows com HTMAXBUTTON/HTMINBUTTON/HTCLOSE sobre os teus botões
        IntPtr Hook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_NCLBUTTONDOWN || msg == WM_NCLBUTTONUP)
            {
                int ht = (int)wParam.ToInt64();
                if (ht == HTMINBUTTON || ht == HTMAXBUTTON || ht == HTCLOSE)
                {
                    handled = true;                      // impede o desenho nativo
                    if (msg == WM_NCLBUTTONUP)
                    {
                        if (ht == HTMINBUTTON) SystemCommands.MinimizeWindow(window);
                        else if (ht == HTCLOSE) SystemCommands.CloseWindow(window);
                        else if (window.WindowState == WindowState.Maximized) SystemCommands.RestoreWindow(window);
                        else SystemCommands.MaximizeWindow(window);
                    }
                    return IntPtr.Zero;
                }
            }
            if (msg != WM_NCHITTEST) return IntPtr.Zero;

            // Coordenadas assinadas (sem OverflowException em 64 bits), em pixels físicos
            int lp = unchecked((int)(long)lParam);
            var client = window.PointFromScreen(new Point((short)(lp & 0xFFFF), (short)((lp >> 16) & 0xFFFF)));

            if (Hit(window, close, client)) { handled = true; return (IntPtr)HTCLOSE; }
            if (Hit(window, maximize, client)) { handled = true; return (IntPtr)HTMAXBUTTON; }
            if (Hit(window, minimize, client)) { handled = true; return (IntPtr)HTMINBUTTON; }

            return IntPtr.Zero;
        }

        void AddHook()
        {
            if (PresentationSource.FromVisual(window) is HwndSource source)
                source.AddHook(Hook);
        }

        if (new WindowInteropHelper(window).Handle != IntPtr.Zero) AddHook();
        else window.SourceInitialized += (_, _) => AddHook();
    }

    private static bool Hit(Window window, FrameworkElement element, Point clientPoint)
    {
        var pos = element.TranslatePoint(new Point(0, 0), window);
        var rect = new Rect(pos.X, pos.Y, element.ActualWidth, element.ActualHeight);
        rect.Inflate(2, 2);
        return rect.Contains(clientPoint);
    }

    private static void AjustarMargem(Window window)
    {
        if (window.Content is FrameworkElement root)
            root.Margin = window.WindowState == WindowState.Maximized
            ? SystemParameters.WindowResizeBorderThickness
            : new Thickness(0);
    }
}