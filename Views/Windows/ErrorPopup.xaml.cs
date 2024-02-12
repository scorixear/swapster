using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Swapster.Views
{
    /// <summary>
    /// Interaction logic for ErrorPopup.xaml
    /// </summary>
    public partial class ErrorPopup : UserControl
    {
        public ErrorPopup()
        {
            InitializeComponent();
        }

        public static DependencyProperty TitleProperty = DependencyProperty.Register(nameof(Title), typeof(string), typeof(ErrorPopup));
        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public static DependencyProperty ErrorProperty = DependencyProperty.Register(nameof(Error), typeof(string), typeof(ErrorPopup));
        public string Error
        {
            get => (string)GetValue(ErrorProperty);
            set => SetValue(ErrorProperty, value);
        }

        public static readonly DependencyProperty OkClickProperty = DependencyProperty.Register(nameof(OkClick), typeof(ICommand), typeof(ErrorPopup));
        public ICommand OkClick
        {
            get => (ICommand)GetValue(OkClickProperty);
            set => SetValue(OkClickProperty, value);
        }
    }
}
