using Microsoft.UI.Xaml;
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

        public static readonly DependencyProperty SelectedIndexProperty = DependencyProperty.Register(nameof(SelectedIndex), typeof(int), typeof(NavigationViewSelectedIndexBehavior), new PropertyMetadata(0));

        public DependencyObject AssociatedObject { get; set; }

        public void Attach(DependencyObject associatedObject)
        {
            this.AssociatedObject = associatedObject;
            var navigationView = (Microsoft.UI.Xaml.Controls.NavigationView)this.AssociatedObject;
            navigationView.SelectionChanged += MenuSelectionChanged;
        }

        private void MenuSelectionChanged(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewSelectionChangedEventArgs args)
        {
            this.SelectedIndex = sender.MenuItems.IndexOf(sender.SelectedItem);
        }

        public void Detach()
        {
            var navigationView = (Microsoft.UI.Xaml.Controls.NavigationView)this.AssociatedObject;
            navigationView.SelectionChanged -= MenuSelectionChanged;
        }
    }
}
