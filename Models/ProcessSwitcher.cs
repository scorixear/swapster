using Microsoft.VisualBasic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Swapster.Models
{
    // A Class that implements the switching of procceses
    public partial class ProcessSwitcher
    {
        // Alternative Options to set Focus
        // all not that reliable
        //[LibraryImport("user32.dll")]
        //[return: MarshalAs(UnmanagedType.Bool)]
        //private static partial bool SetForegroundWindow(IntPtr hWnd);

        //[LibraryImport("user32.dll")]
        //[return: MarshalAs(UnmanagedType.Bool)]
        //private static partial bool SwitchToThisWindow(IntPtr hWnd, [MarshalAs(UnmanagedType.Bool)] bool bEnable);

        //[DllImport("user32.dll")]
        //private static extern IntPtr SetFocus(HandleRef hWnd);

        //[DllImport("user32.dll")]
        //private static extern IntPtr SetActiveWindow(HandleRef hWnd);

        // Used for Maximizing applications if they are minimized
        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool ShowWindow(IntPtr hWnd, ShowWindowEnum flags);


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

        public int BringWindowToFront(Process process)
        {
            ShowWindow(process.MainWindowHandle, ShowWindowEnum.ShowMaximized);
            try
            {
                Interaction.AppActivate(process.Id);
            }
            catch
            {
                return 3;
            }
            return 0;
        }

        private int currentProcess = 0;
        private List<Process> processList = new();
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
            this.currentProcess = 0;

            foreach (Process process in processes)
            {
                GetWindowThreadProcessId(process.MainWindowHandle, out var processThreadId);
                AttachThreadInput(_threadId, processThreadId, true);
            }

            Task timerTask = new(async () =>
            {
                timer = new(TimeSpan.FromSeconds(1));
                while (await timer.WaitForNextTickAsync())
                {
                    TimerTickEvent?.Invoke(this.currentTime);
                    if (this.currentTime > 0)
                    {
                        this.currentTime -= 1;
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
                            int result = BringWindowToFront(this.processList[currentProcess]);
                            if (result != 0)
                            {
                                ProcessSwitchErrorEvent?.Invoke(this.processList[currentProcess].ProcessName + ": " + result);
                            }
                        }
                        catch
                        {
                            ProcessSwitchErrorEvent?.Invoke(this.processList[currentProcess].ProcessName);
                        }
                        this.currentProcess++;
                    }
                }
            });
            try
            {
                BringWindowToFront(this.processList[currentProcess]);
            }
            catch
            {
                ProcessSwitchErrorEvent?.Invoke(this.processList[currentProcess].ProcessName);
                return false;
            }
            this.currentProcess++;
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
                    GetWindowThreadProcessId(process.MainWindowHandle, out var processThreadId);
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
