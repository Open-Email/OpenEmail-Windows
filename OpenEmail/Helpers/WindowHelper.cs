using System;
using System.Collections.Generic;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WinRT.Interop;

namespace OpenEmail.Helpers
{
    public class WindowHelper
    {
        static public Window CreateWindow(Window createdWindow = null)
        {
            if (createdWindow == null)
            {
                createdWindow = new Window
                {
                    SystemBackdrop = new MicaBackdrop(),
                    Content = new Frame()
                };
            }

            TrackWindow(createdWindow);

            return createdWindow;
        }

        static public void TrackWindow(Window window)
        {
            window.Closed += (sender, args) =>
            {
                _activeWindows.Remove(window);
            };

            _activeWindows.Add(window);
        }

        static public AppWindow GetAppWindow(Window window)
        {
            IntPtr hWnd = WindowNative.GetWindowHandle(window);
            WindowId wndId = Win32Interop.GetWindowIdFromWindow(hWnd);
            return AppWindow.GetFromWindowId(wndId);
        }

        static public List<Window> ActiveWindows { get { return _activeWindows; } }

        static private List<Window> _activeWindows = new List<Window>();
    }
}
