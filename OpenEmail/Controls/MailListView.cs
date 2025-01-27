using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OpenEmail.ViewModels.Interfaces;

namespace OpenEmail.Controls
{
    public class MailListView : ItemsControl
    {
        public ISelectableItem SelectedItem
        {
            get { return (ISelectableItem)GetValue(SelectedItemProperty); }
            set { SetValue(SelectedItemProperty, value); }
        }

        public static readonly DependencyProperty SelectedItemProperty = DependencyProperty.Register(nameof(SelectedItem), typeof(ISelectableItem), typeof(MailListView), new PropertyMetadata(null, new PropertyChangedCallback(OnSelectedItemChanged)));

        private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as MailListView;
            control?.UpdateSelectedItem();
        }

        private void UpdateSelectedItem()
        {
            var itemsSource = ItemsSource as ObservableCollection<IMessageViewModel>;

            if (itemsSource == null) return;

            foreach (var item in itemsSource)
            {
                item.IsSelected = false;
            }

            if (SelectedItem == null) return;

            SelectedItem.IsSelected = true;
        }
    }
}
