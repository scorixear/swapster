using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SwitchyReloaded.Models
{
    // A Class that implements the switching of procceses
    public partial class ProcessSwitcher
    {
        // Used for actually setting the Focus
        [LibraryImport("user32.dll")] private static partial int SetForegroundWindow(IntPtr hWnd);
        // Used for Maximizing applications if they are minimized
        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool ShowWindow(IntPtr hWnd, ShowWindowEnum flags);

        // Enum IntPtr for what ShowWindow can do
        private enum ShowWindowEnum : int
        {
            Hide = 0,
            ShowNormal = 1,
            ShowMinimized = 2,
            ShowMaximized = 3,
            Maximize = 3,
            ShowNormalNoActive = 4,
            Show = 5,
            Minimize = 6,
            ShowMinNoActivate = 7,
            ShowNoActivate = 8,
            Restore = 9,
            ShowDefault = 10,
            ForceMinimized = 11
        }

        public static void BringWindowToFront(Process process)
        {
            // If the MainWindowHandle is 0, it is a Process that doesn't have a graphical interface (or is mimized to the tray)
            if (process.MainWindowHandle == IntPtr.Zero)
            {
                throw new NoGraphicalProcessException();
            }
            // maximize the window
            ShowWindow(process.MainWindowHandle, ShowWindowEnum.ShowMaximized);
            // and set the focus
            SetForegroundWindow(process.MainWindowHandle);
        }

        public class NoGraphicalProcessException : Exception
        {
            public NoGraphicalProcessException() { }
            public NoGraphicalProcessException(string message) : base(message) { }
            public NoGraphicalProcessException(string message, Exception innerException) : base(message, innerException) { }
        }
    }
}
