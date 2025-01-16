using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace OpenEmail.Controls
{
    public class CustomNavigationView : NavigationView
    {
        public int SelectedIndex
        {
            get { return (int)GetValue(SelectedIndexProperty); }
            set { SetValue(SelectedIndexProperty, value); }
        }

        public static readonly DependencyProperty SelectedIndexProperty = DependencyProperty.Register("SelectedIndex", typeof(int), typeof(CustomNavigationView), new PropertyMetadata(-1, new PropertyChangedCallback(OnSelectedIndexChanged)));

        private static void OnSelectedIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as CustomNavigationView;
            if (control != null)
            {
                if (control.SelectedIndex < 0)
                {
                    control.SelectedItem = null;
                }
                else
                {
                    control.SelectedItem = control.MenuItems[control.SelectedIndex];
                }

            }
        }

        public CustomNavigationView()
        {
            RegisterPropertyChangedCallback(SelectedItemProperty, OnSelectedItemChanged);
        }

        private void OnSelectedItemChanged(DependencyObject sender, DependencyProperty dp)
        {
            if (SelectedItem == null)
            {
                SelectedIndex = -1;
            }
            else
            {
                SelectedIndex = MenuItems.IndexOf(SelectedItem);
            }
        }
    }
}
