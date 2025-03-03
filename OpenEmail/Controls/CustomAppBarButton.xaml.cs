using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace OpenEmail.Controls
{
    public sealed partial class CustomAppBarButton : UserControl
    {
        public static readonly DependencyProperty IconProperty = DependencyProperty.Register(nameof(Icon), typeof(object), typeof(CustomAppBarButton), new PropertyMetadata(null));
        public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(nameof(Label), typeof(string), typeof(CustomAppBarButton), new PropertyMetadata(string.Empty));
        public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(CustomAppBarButton), new PropertyMetadata(null));
        public static readonly DependencyProperty IsCompactProperty = DependencyProperty.Register(nameof(IsCompact), typeof(bool), typeof(CustomAppBarButton), new PropertyMetadata(true, new PropertyChangedCallback(OnIsCompactChanged)));

        public bool IsCompact
        {
            get { return (bool)GetValue(IsCompactProperty); }
            set { SetValue(IsCompactProperty, value); }
        }

        public ICommand Command
        {
            get { return (ICommand)GetValue(CommandProperty); }
            set { SetValue(CommandProperty, value); }
        }

        public object Icon
        {
            get => GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        public string Label
        {
            get => (string)GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        public static void OnIsCompactChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CustomAppBarButton button)
            {
                button.OnIsCompactChanged();
            }
        }

        private void OnIsCompactChanged()
        {
            LabelTextblock.Visibility = IsCompact ? Visibility.Collapsed : Visibility.Visible;
        }

        public CustomAppBarButton()
        {
            InitializeComponent();
        }
    }
}
