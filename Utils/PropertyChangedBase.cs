using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SwitchyReloaded.Utils
{
    // Base Class that provides an implementation of the INotifyPropertyChanged event
    public class PropertyChangedBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void NotifyOfPopertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}