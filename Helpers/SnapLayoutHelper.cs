using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Project_Kitsune.Helpers
{
    public static class SnapLayoutHelper
    {
        private const int WM_NCHITTEST = 0x0084;
        private const int HTMAXBUTTON = 9;

        public static void Ativar(Window window, FrameworkElement botaoMaximizar)
        {
            var helper = new WindowInteropHelper(window);
            var hwndSource = HwndSource.FromHwnd(helper.EnsureHandle());
            hwndSource?.AddHook((IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) =>
                WndProc(hwnd, msg, wParam, lParam, ref handled, botaoMaximizar));
        }

        private static IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled, FrameworkElement botao)
        {
            if (msg == WM_NCHITTEST)
            {
                int x = (short)((int)lParam & 0xFFFF);
                int y = (short)(((int)lParam >> 16) & 0xFFFF);
                var pontoTela = new Point(x, y);
                var pontoJanela = botao.PointFromScreen(pontoTela);

                bool dentroDoBotao = pontoJanela.X >= 0 && pontoJanela.X <= botao.ActualWidth &&
                                      pontoJanela.Y >= 0 && pontoJanela.Y <= botao.ActualHeight;

                if (dentroDoBotao)
                {
                    handled = true;
                    return (IntPtr)HTMAXBUTTON;
                }
            }
            return IntPtr.Zero;
        }
    }
}