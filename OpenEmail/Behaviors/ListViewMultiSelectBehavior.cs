using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Xaml.Interactivity;
using OpenEmail.ViewModels.Data;

namespace OpenEmail.Behaviors
{
    public class ListViewMultiSelectBehavior : DependencyObject, IBehavior
    {
        public static readonly DependencyProperty SelectedItemsProperty = DependencyProperty.Register(nameof(SelectedItems), typeof(ObservableCollection<MessageViewModel>), typeof(ListViewMultiSelectBehavior), new PropertyMetadata(null, new PropertyChangedCallback(OnSelectedItemsBindingChanged)));

        public ObservableCollection<MessageViewModel> SelectedItems
        {
            get { return (ObservableCollection<MessageViewModel>)GetValue(SelectedItemsProperty); }
            set { SetValue(SelectedItemsProperty, value); }
        }

        public DependencyObject AssociatedObject { get; set; }

        private void SyncSelectedItems(SelectionChangedEventArgs args)
        {
            if (AssociatedObject == null) return;

            if (AssociatedObject is not ListView listView) return;

            listView.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal, () =>
            {
                foreach (var item in args.RemovedItems)
                {
                    if (item is not MessageViewModel messageViewModel || !SelectedItems.Contains(messageViewModel)) continue;

                    SelectedItems.Remove((MessageViewModel)item);
                }

                foreach (var item in args.AddedItems)
                {
                    if (item is not MessageViewModel messageViewModel || SelectedItems.Contains(messageViewModel)) continue;
                    SelectedItems.Add(messageViewModel);
                }
            });
        }

        public static void OnSelectedItemsBindingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ListViewMultiSelectBehavior behavior)
            {
                if (e.OldValue is ObservableCollection<MessageViewModel> oldCollection)
                {
                    oldCollection.CollectionChanged -= behavior.OnItemsSourceCollectionChanged;
                }
                if (e.NewValue is ObservableCollection<MessageViewModel> newCollection)
                {
                    newCollection.CollectionChanged += behavior.OnItemsSourceCollectionChanged;
                }
            }
        }

        public void OnItemsSourceCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            // Here listen for VM manipulation of SelectedItems and update the ListView selection accordingly.

            if (AssociatedObject is not ListView listView) return;

            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (var item in e.NewItems)
                {
                    if (item is not MessageViewModel || listView.SelectedItems.Contains(item)) continue;
                    listView.SelectedItems.Add(item);
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (var item in e.OldItems)
                {
                    if (item is not MessageViewModel || !listView.SelectedItems.Contains(item)) continue;
                    listView.SelectedItems.Remove(item);
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                listView.SelectedItems.Clear();
            }
        }

        public void Attach(DependencyObject associatedObject)
        {
            AssociatedObject = associatedObject;

            if (AssociatedObject != null)
            {
                var listview = (ListView)this.AssociatedObject;
                listview.SelectionChanged += ItemSelectionChanged;

                // Initialize SelectedItems if it's null.  Important to avoid null reference exceptions.
                SelectedItems ??= new ObservableCollection<MessageViewModel>();

            }
        }

        private void ItemSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SyncSelectedItems(e);
        }

        public void Detach()
        {
            if (AssociatedObject != null)
            {
                var listview = AssociatedObject as ListView;

                if (AssociatedObject is ListView listView)
                {
                    listView.SelectionChanged -= ItemSelectionChanged;
                }
            }
        }
    }
}
