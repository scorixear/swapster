using Microsoft.VisualBasic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Swapster.Models
{
    // A Class that implements the switching of procceses
    public partial class ProcessSwitcher
    {
        // Alternative Option to set Focus
        //[LibraryImport("user32.dll")]
        //[return: MarshalAs(UnmanagedType.Bool)]
        //private static partial bool SetForegroundWindow(IntPtr hWnd);

        // Used for Maximizing applications if they are minimized
        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool ShowWindow(IntPtr hWnd, ShowWindowEnum flags);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool IsIconic(IntPtr hWnd);

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
                throw new ProcessFocusException("Process has not MainWindowHandle");
            }

            // If the Process is minimized
            if (IsIconic(process.MainWindowHandle))
            {
                // maximize the window
                bool success = ShowWindow(process.MainWindowHandle, ShowWindowEnum.ShowMaximized);
                if (success == false)
                {
                    throw new ProcessFocusException("Could not maximize window");
                }
            }
            try
            {
                Interaction.AppActivate(process.Id);
            }
            catch
            {
                throw new ProcessFocusException("Could not set focus to app");
            }
        }

        public class ProcessFocusException : Exception
        {
            public ProcessFocusException() { }
            public ProcessFocusException(string message) : base(message) { }
            public ProcessFocusException(string message, Exception innerException) : base(message, innerException) { }
        }
    }
}
