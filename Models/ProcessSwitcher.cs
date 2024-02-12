using Microsoft.VisualBasic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Swapster.Models
{
    // A Class that implements the switching of procceses
    public partial class ProcessSwitcher
    {
        // Alternative Option to set Focus
        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool SetForegroundWindow(IntPtr hWnd);

        // Used for Maximizing applications if they are minimized
        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool ShowWindow(IntPtr hWnd, ShowWindowEnum flags);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool IsIconic(IntPtr hWnd);

        [LibraryImport("user32.dll", SetLastError = true)]
        private static partial uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
        [LibraryImport("kernel32.dll")]
        private static partial uint GetCurrentThreadId();
        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool AttachThreadInput(uint idAttach, uint idAttachTo, [MarshalAs(UnmanagedType.Bool)] bool fAttach);

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

        private readonly uint _threadId;
        public ProcessSwitcher()
        {
            _threadId = GetCurrentThreadId();
        }

        public void BringWindowToFront(Process process)
        {
            // If the MainWindowHandle is 0, it is a Process that doesn't have a graphical interface (or is mimized to the tray)
            if (process.MainWindowHandle == IntPtr.Zero)
            {
                throw new ProcessFocusException("Process has not MainWindowHandle");
            }
            bool success;
            // If the Process is minimized
            if (IsIconic(process.MainWindowHandle))
            {
                // maximize the window
                success = ShowWindow(process.MainWindowHandle, ShowWindowEnum.ShowMaximized);
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
                throw new ProcessFocusException();
            }
            //success = SetForegroundWindow(process.MainWindowHandle);
            //if (success == false)
            //{
            //    throw new ProcessFocusException("Could not set focus to window");
            //}
        }

        private int currentProcess = 0;
        private List<Process> processList = new List<Process>();
        private int timerLength;
        private int currentTime;
        private PeriodicTimer? timer;
        public delegate void TimerTickEventHandler(int currentTime);
        public event TimerTickEventHandler? TimerTickEvent;
        public delegate void ProcessSwitchErrorEventHandler(string processName);
        public event ProcessSwitchErrorEventHandler? ProcessSwitchErrorEvent;
        public bool Start(List<Process> processes, int timerLength)
        {
            if (processes.Count == 0 || timerLength <= 0)
            {
                return false;
            }
            processList = processes;
            this.timerLength = timerLength;
            this.currentTime = timerLength;

            foreach (Process process in processes)
            {
                uint processThreadId = GetWindowThreadProcessId(process.MainWindowHandle, out var _);
                AttachThreadInput(_threadId, processThreadId, true);
            }

            Task timerTask = new(async () =>
            {
                timer = new(TimeSpan.FromSeconds(5));
                while (await timer.WaitForNextTickAsync())
                {
                    TimerTickEvent?.Invoke(this.currentTime);
                    if (this.currentTime > 4)
                    {
                        this.currentTime -= 5;
                    }
                    else
                    {
                        this.currentTime = this.timerLength;
                        if (this.currentProcess >= this.processList.Count)
                        {
                            this.currentProcess = 0;
                        }
                        try
                        {
                            BringWindowToFront(this.processList[currentProcess]);
                        }
                        catch
                        {
                            ProcessSwitchErrorEvent?.Invoke(this.processList[currentProcess].ProcessName);
                        }
                        this.currentProcess++;
                    }
                }
            });
            timerTask.Start();
            return true;
        }

        public void Stop()
        {
            try
            {
                timer?.Dispose();
                foreach (Process process in processList)
                {
                    uint processThreadId = GetWindowThreadProcessId(process.MainWindowHandle, out var _);
                    AttachThreadInput(_threadId, processThreadId, false);
                }
            }
            catch
            {
                throw;
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
