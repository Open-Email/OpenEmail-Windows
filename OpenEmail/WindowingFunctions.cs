using System;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;

namespace OpenEmail
{
    public static class WindowingFunctions
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr LoadImage(IntPtr hInst, string lpszName, uint uType, int cxDesired, int cyDesired, uint fuLoad);

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        private const int IMAGE_ICON = 1;
        private const int LR_LOADFROMFILE = 0x00000010;
        private const int LR_DEFAULTSIZE = 0x00000040;

        public static void CloseWindow(Window window)
        {
            const uint WM_CLOSE = 0x0010;

            // Find the window handle by its title
            IntPtr hWnd = global::WinRT.Interop.WindowNative.GetWindowHandle(window);

            if (hWnd == IntPtr.Zero)
            {
                Console.WriteLine("Window not found. Ensure the title is correct.");
            }
            else
            {
                PostMessage(hWnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
            }
        }

        public static void SetWindowIcon(string iconPath, Window window)
        {
            IntPtr hwnd = global::WinRT.Interop.WindowNative.GetWindowHandle(window);

            const int WM_SETICON = 0x0080;
            const int ICON_SMALL = 0;
            const int ICON_BIG = 1;

            IntPtr hIcon = LoadImage(IntPtr.Zero, iconPath, IMAGE_ICON, 0, 0, LR_LOADFROMFILE | LR_DEFAULTSIZE);

            SendMessage(hwnd, WM_SETICON, ICON_SMALL, hIcon);
            SendMessage(hwnd, WM_SETICON, ICON_BIG, hIcon);
        }

        public static void DisableMinimizeMaximizeButtons(Window window)
        {
            IntPtr hwnd = global::WinRT.Interop.WindowNative.GetWindowHandle(window);

            const int GWL_STYLE = -16;
            const int WS_MINIMIZEBOX = 0x00020000;
            const int WS_MAXIMIZEBOX = 0x00010000;
            const int WS_SIZEBOX = 0x00040000;

            // Get the current window style
            int style = GetWindowLong(hwnd, GWL_STYLE);

            style &= ~WS_MINIMIZEBOX;
            style &= ~WS_MAXIMIZEBOX;
            style &= ~WS_SIZEBOX;

            SetWindowLong(hwnd, GWL_STYLE, style);
        }

        public static void BringToFront(this Window window)
        {
            var hwnd = global::WinRT.Interop.WindowNative.GetWindowHandle(window);
            SetForegroundWindow(hwnd);
        }

        public static void CenterWindowOnScreen(Window window)
        {
            IntPtr hwnd = global::WinRT.Interop.WindowNative.GetWindowHandle(window);

            // Constants for GetSystemMetrics
            const int SM_CXSCREEN = 0; // Screen width
            const int SM_CYSCREEN = 1; // Screen height

            // Get screen dimensions
            int screenWidth = GetSystemMetrics(SM_CXSCREEN);
            int screenHeight = GetSystemMetrics(SM_CYSCREEN);

            // Get window dimensions
            GetWindowRect(hwnd, out RECT rect);
            int windowWidth = rect.Right - rect.Left;
            int windowHeight = rect.Bottom - rect.Top;

            // Calculate position for centering
            int x = (screenWidth - windowWidth) / 2;
            int y = (screenHeight - windowHeight) / 2;

            // Set window position
            const uint SWP_NOSIZE = 0x0001;
            const uint SWP_NOZORDER = 0x0004;
            SetWindowPos(hwnd, IntPtr.Zero, x, y, 0, 0, SWP_NOSIZE | SWP_NOZORDER);
        }
    }
}
