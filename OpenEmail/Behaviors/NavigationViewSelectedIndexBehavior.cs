using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Xaml.Interactivity;

namespace OpenEmail.Behaviors
{
    public class NavigationViewSelectedIndexBehavior : DependencyObject, IBehavior
    {
        public int SelectedIndex
        {
            get { return (int)GetValue(SelectedIndexProperty); }
            set { SetValue(SelectedIndexProperty, value); }
        }

        public static readonly DependencyProperty SelectedIndexProperty = DependencyProperty.Register(nameof(SelectedIndex), typeof(int), typeof(NavigationViewSelectedIndexBehavior), new PropertyMetadata(-1, new PropertyChangedCallback(OnSelectedIndexChanged)));

        public DependencyObject AssociatedObject { get; set; }

        public void Attach(DependencyObject associatedObject)
        {
            this.AssociatedObject = associatedObject;
            var navigationView = (NavigationView)this.AssociatedObject;
            navigationView.SelectionChanged += MenuSelectionChanged;
        }

        private static void OnSelectedIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var behavior = (NavigationViewSelectedIndexBehavior)d;
            var navigationView = (NavigationView)behavior.AssociatedObject;
            navigationView.SelectedItem = navigationView.MenuItems[behavior.SelectedIndex];

            if (navigationView.SelectedItem != null)
            {
                var container = navigationView.ContainerFromMenuItem(navigationView.SelectedItem);

                if (container is NavigationViewItem navigationViewItem)
                {
                    navigationViewItem.IsSelected = true;
                }
            }
        }

        private void MenuSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            this.SelectedIndex = sender.MenuItems.IndexOf(sender.SelectedItem);
        }

        public void Detach()
        {
            var navigationView = (NavigationView)this.AssociatedObject;
            navigationView.SelectionChanged -= MenuSelectionChanged;
        }
    }
}
