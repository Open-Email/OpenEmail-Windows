using System.Collections.Generic;
using Microsoft.UI.Xaml.Controls;
using WinUIEx;

namespace OpenEmail.Helpers
{
    public class WindowHelper
    {
        static public WindowEx CreateWindow(WindowEx createdWindow = null)
        {
            if (createdWindow == null)
            {
                createdWindow = new WindowEx
                {
                    Content = new Frame(),
                    Title = "Open Email"
                };
            }

            // TrackWindow(createdWindow);

            return createdWindow;
        }

        static public void TrackWindow(WindowEx window)
        {
            window.Closed += (sender, args) =>
            {
                _activeWindows.Remove(window);
            };

            _activeWindows.Add(window);
        }

        static public List<WindowEx> ActiveWindows { get { return _activeWindows; } }

        static private List<WindowEx> _activeWindows = new List<WindowEx>();
    }
}
