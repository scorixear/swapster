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
        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool SetForegroundWindow(IntPtr hWnd);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool SwitchToThisWindow(IntPtr hWnd, [MarshalAs(UnmanagedType.Bool)] bool bEnable);

        //[DllImport("user32.dll")]
        //private static extern IntPtr SetFocus(HandleRef hWnd);

        //[DllImport("user32.dll")]
        //private static extern IntPtr SetActiveWindow(HandleRef hWnd);

        // Used for Maximizing applications if they are minimized
        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool ShowWindow(IntPtr hWnd, ShowWindowEnum flags);

        // Gets the Window Thread Process ID of a given WindowHandle
        // Used to route Inputs from this application to other applications
        [LibraryImport("user32.dll", SetLastError = true)]
        private static partial uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
        // Returns the current ThreadID
        [LibraryImport("kernel32.dll")]
        private static partial uint GetCurrentThreadId();
        //Attaches or Detaches the input of a given thread to another thread
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

        public enum ProcessSwitchType
        {
            AppActivate,
            SetForegroundWindow,
            SwitchToThisWindow
        }

        // the current ThreadID
        private readonly uint _threadId;
        public ProcessSwitcher()
        {
            _threadId = GetCurrentThreadId();
        }

        /// <summary>
        /// Brings a given process to the front
        /// </summary>
        /// <param name="process"></param>
        /// <returns></returns>
        private static int BringWindowToFront(Process process, ProcessSwitchType processSwitchType)
        {
            // always maximize works best
            ShowWindow(process.MainWindowHandle, ShowWindowEnum.ShowMaximized);
            // AppActive seems most consistent between SwitchToThisWindow, SetForegroundWindow, SetActiveWindow and AppActivate
            // My guess is, the WindowHandle changes over time, while AppActive retrieves the WindowHandle everytime new from the given process id
            bool success;
            switch (processSwitchType)
            {

                case ProcessSwitchType.SetForegroundWindow:
                    success = SetForegroundWindow(process.MainWindowHandle);
                    return success ? 0 : 1;
                case ProcessSwitchType.SwitchToThisWindow:
                    success = SwitchToThisWindow(process.MainWindowHandle, true);
                    return success ? 0 : 2;
                case ProcessSwitchType.AppActivate:
                default:
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
        }

        // The id of the next process to bring to the front
        private int currentProcessCounter = 0;
        // A list of Processes we cycle through
        private List<Process> processList = new();
        // the length of the timer in seconds
        private int timerLength;
        // the current time counter, starting from timerLength to 0
        private int currentTimeCounter;
        // The timer ticking every second
        private PeriodicTimer? timer;
        // Event called every time the timer ticks
        public delegate void TimerTickEventHandler(int currentTime);
        public event TimerTickEventHandler? TimerTickEvent;
        // Event called when a process switch is not possible resulting in an error popup
        public delegate void ProcessSwitchErrorEventHandler(string processName);
        public event ProcessSwitchErrorEventHandler? ProcessSwitchErrorEvent;

        /// <summary>
        /// Starts the cycling of proccesses
        /// </summary>
        /// <param name="processes"></param>
        /// <param name="timerLength"></param>
        /// <returns></returns>
        public bool Start(List<Process> processes, int timerLength, ProcessSwitchType processSwitchType)
        {
            // if no process is provided or the timer is 0
            // don't start
            if (processes.Count == 0 || timerLength <= 0)
            {
                return false;
            }

            // setup local variables
            processList = processes;
            this.timerLength = timerLength;
            currentTimeCounter = timerLength;
            currentProcessCounter = 0;

            // Attach the ThreadInput of this thread to all given processes
            foreach (Process process in processes)
            {
                GetWindowThreadProcessId(process.MainWindowHandle, out var processThreadId);
                AttachThreadInput(_threadId, processThreadId, true);
            }

            // create a task that switches every time the timer ticks
            Task timerTask = new(async () =>
            {
                // initiate the timer
                timer = new(TimeSpan.FromSeconds(1));
                // asynchronously wait for the next tick
                while (await timer.WaitForNextTickAsync())
                {
                    // Invoke the event out of this thread
                    TimerTickEvent?.Invoke(currentTimeCounter);
                    // if the current time counter is bigger then 0, we don't switch yet
                    if (currentTimeCounter > 0)
                    {
                        currentTimeCounter -= 1;
                    }
                    else
                    {
                        // reset the counter
                        currentTimeCounter = timerLength;
                        // cycle counter if it reached the end
                        if (currentProcessCounter >= processList.Count)
                        {
                            currentProcessCounter = 0;
                        }
                        // try bringing the process to the front
                        try
                        {
                            int result = BringWindowToFront(processList[currentProcessCounter], processSwitchType);
                            // if the result is not 0, this is an error
                            // the error ID will be shown in the error popup
                            // and invoke the error event
                            if (result != 0)
                            {
                                ProcessSwitchErrorEvent?.Invoke(processList[currentProcessCounter].ProcessName + ": " + result);
                            }
                        }
                        catch
                        {
                            ProcessSwitchErrorEvent?.Invoke(processList[currentProcessCounter].ProcessName);
                        }
                        currentProcessCounter++;
                    }
                }
            });
            // before starting the task, show the first process
            // if that fails, don't start the timer
            try
            {
                int result = BringWindowToFront(processList[currentProcessCounter], processSwitchType);
                if (result != 0)
                {
                    ProcessSwitchErrorEvent?.Invoke(processList[currentProcessCounter].ProcessName + ": " + result);
                    return false;
                }
            }
            catch
            {
                ProcessSwitchErrorEvent?.Invoke(processList[currentProcessCounter].ProcessName);
                return false;
            }
            // increment counter, start the timer task
            currentProcessCounter++;
            timerTask.Start();
            return true;
        }

        /// <summary>
        /// Stops the current timer task and detaches thread inputs
        /// </summary>
        public void Stop()
        {

            // if the timer is present, dispose it
            timer?.Dispose();
            // detach thread inputs
            foreach (Process process in processList)
            {
                GetWindowThreadProcessId(process.MainWindowHandle, out var processThreadId);
                AttachThreadInput(_threadId, processThreadId, false);
            }
        }
    }
}
