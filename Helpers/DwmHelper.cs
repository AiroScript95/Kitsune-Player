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

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hwnd, IntPtr hwndInsertAfter,
            int x, int y, int width, int height, uint flags);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);

        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        private const int DWMWA_BORDER_COLOR = 34;
        private const int DWMWA_CAPTION_COLOR = 35;
        private const int DWMWCP_ROUND = 2;
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_DLGMODALFRAME = 0x0001;
        private const int SWP_NOSIZE = 0x0001;
        private const int SWP_NOMOVE = 0x0002;
        private const int SWP_NOZORDER = 0x0004;
        private const int SWP_FRAMECHANGED = 0x0020;
        private const uint WM_SETICON = 0x0080;

        public static void AtivarCantosArredondados(Window window)
        {
            if (!SuportaDwmModerno) return;
            var hwnd = new WindowInteropHelper(window).Handle;
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

        public static void OcultarIcon(Window window)
        {
            var hwnd = new WindowInteropHelper(window).Handle;

            // Remove o ícone da barra de título
            int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, exStyle | WS_EX_DLGMODALFRAME);

            SendMessage(hwnd, WM_SETICON, new IntPtr(1), IntPtr.Zero); // ICON_BIG
            SendMessage(hwnd, WM_SETICON, IntPtr.Zero, IntPtr.Zero);   // ICON_SMALL

            SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0,
                SWP_NOSIZE | SWP_NOMOVE | SWP_NOZORDER | SWP_FRAMECHANGED);
        }
    }
}