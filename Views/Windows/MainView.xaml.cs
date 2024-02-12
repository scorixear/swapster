using SwitchyReloaded.ViewModels;
using System.Windows;

namespace Swapster.Views
{
    /// <summary>
    /// Code Behind of MainView.xaml
    /// Only Events are 1-1 Mapped to the MainViewModel.cs
    /// </summary>
    public partial class MainView : Window
    {
        public MainView()
        {
            InitializeComponent();
        }

        // called when the window is first loaded
        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            ((MainViewModel)DataContext).OnWindowLoaded();
        }

        // Called when the start/stop button is clicked
        private void StartStop_Click(object sender, RoutedEventArgs e)
        {
            ((MainViewModel)DataContext).OnStartStopClicked();
        }

        // Called when the select process button is clicked (arrow right)
        private void SelectProcess_Click(object sender, RoutedEventArgs e)
        {
            ((MainViewModel)DataContext).OnSelectProcess();
        }

        // Called when the deselect process button is clicked (arrow left)
        private void DeselectProcess_Click(object sender, RoutedEventArgs e)
        {
            ((MainViewModel)DataContext).OnDeselectProcess();
        }

        // Called when the refresh button is clicked
        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            ((MainViewModel)DataContext).OnRefreshClicked();
        }
    }
}
