using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Interop;

namespace Project_Kitsune.Helpers
{
    public static class DwmHelper
    {
        public static readonly bool SuportaDwmModerno = Environment.OSVersion.Version.Build >= 22000;

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref uint attrValue, int attrSize);

        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        private const int DWMWA_BORDER_COLOR = 34;
        private const int DWMWA_CAPTION_COLOR = 35;
        private const int DWMWCP_ROUND = 2;

        public static void AtivarCantosArredondados(Window window)
        {
            if (!SuportaDwmModerno) return;
            var hwnd = new System.Windows.Interop.WindowInteropHelper(window).Handle;
            int preference = DWMWCP_ROUND;
            _ = DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref preference, sizeof(int));
        }

        public static void DefinirCorBorda(Window window, Color cor)
        {
            if (!SuportaDwmModerno) return;
            var hwnd = new WindowInteropHelper(window).Handle;
            uint colorRef = (uint)(cor.R | (cor.G << 8) | (cor.B << 16));
            _ = DwmSetWindowAttribute(hwnd, DWMWA_BORDER_COLOR, ref colorRef, sizeof(uint));
        }

        public static void DefinirCorTitleBar(Window window, Color cor)
        {
            if (!SuportaDwmModerno) return;
            var hwnd = new WindowInteropHelper(window).Handle;
            int colorRef = cor.R | (cor.G << 8) | (cor.B << 16); // formato COLORREF (BGR)
            _ = DwmSetWindowAttribute(hwnd, DWMWA_CAPTION_COLOR, ref colorRef, sizeof(int));
        }
    }
}