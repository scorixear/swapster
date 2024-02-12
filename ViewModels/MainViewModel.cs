using SwitchyReloaded.Models;
using SwitchyReloaded.Utils;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace SwitchyReloaded.ViewModels
{
    public class MainViewModel : PropertyChangedBase
    {
        // Model class for collecting information about running processes
        private readonly ProcessCollector processCollector;

        // Currently Shown Process
        private int processId = 0;
        // Timer object for timing when to switch
        private System.Timers.Timer? timer;

        // Variable for the Start / Stop Button
        private string _runningState = "Start";
        public string RunningState
        {
            get
            {
                return _runningState;
            }

            set
            {
                _runningState = value;
                NotifyOfPopertyChanged(nameof(RunningState));
            }
        }

        // Flag denoting if the timer is currently running or not
        private bool _isRunning = false;
        public bool IsRunning
        {
            get
            {
                return _isRunning;
            }

            set
            {
                // If the timer is started
                if (value)
                {
                    // If the timer length is a number
                    if (int.TryParse(TimerLength, out int intTimerInterval))
                    {
                        timerLength = intTimerInterval;
                        timerCounter = timerLength;
                        // If a current timer is running
                        if (timer != null)
                        {
                            // stop and dispose it
                            timer?.Stop();
                            timer?.Dispose();
                        }
                        // Create a new Timer that elapses every second
                        timer = new System.Timers.Timer(1000);
                        timer.Elapsed += Timer_Elapsed;
                        // And autoresets
                        timer.AutoReset = true;
                        // And start the timer
                        timer.Enabled = true;
                        // then set the label
                        RunningState = "Stop";
                    }
                    // else if the given timer length is not a number
                    // don't do anything
                    else
                    {
                        return;
                    }
                }
                // if the timer is stopped
                else
                {
                    // Stop and dispose the timer
                    timer?.Stop();
                    timer?.Dispose();
                    timer = null;
                    // then set the label
                    RunningState = "Start";
                }
                // update internal variable
                _isRunning = value;
                NotifyOfPopertyChanged(nameof(IsRunning));
            }
        }

        // Called every second when the timer is elapsed
        private void Timer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            // if no processes are selected, skip
            if (SelectedProcesses.Count == 0)
            {
                return;
            }

            // Update Timer Countdown
            TimerCountdown = $"Switch in {timerCounter} Sekunden";
            // if we haven't hit the switch timer
            if (timerCounter > 0)
            {
                // just decremnt
                timerCounter--;
                return;
            }
            // otherwise reset to set timer length
            timerCounter = timerLength;
            // increment processId
            processId++;
            // wrap processId to the start
            if (processId >= SelectedProcesses.Count)
            {
                processId = 0;
            }
            // Get the process object from the name
            Process? process = processCollector.GetProcessByName(SelectedProcesses[processId].Name);
            // if the process could not be found, skip
            if (process == null)
            {
                return;
            }
            // Call model class to bring process to front
            ProcessSwitcher.BringWindowToFront(process);
        }

        // Binding for the Timer Length
        public string TimerLength { get; set; } = "60";
        // Integer Representation of TimerLength
        private int timerLength = 60;
        // Counter that counts down from TimerLength to 0
        private int timerCounter = 60;

        // Label for Timer Countdown
        private string _timerCountdown = "Switch in 0 Sekunden";
        public string TimerCountdown
        {
            get
            {
                return _timerCountdown;
            }

            set
            {
                _timerCountdown = value;
                NotifyOfPopertyChanged(nameof(TimerCountdown));
            }
        }

        // List of Processes that are available but not selected
        private ObservableCollection<ProcessData> _processes = new();
        public ObservableCollection<ProcessData> Processes
        {
            get
            {
                return _processes;
            }
            set
            {
                _processes = value;
                NotifyOfPopertyChanged(nameof(Processes));
            }
        }

        // List of Process that are selected
        private ObservableCollection<ProcessData> _selectedProcesses = new();
        public ObservableCollection<ProcessData> SelectedProcesses
        {
            get
            {
                return _selectedProcesses;
            }

            set
            {
                _selectedProcesses = value;
                NotifyOfPopertyChanged(nameof(SelectedProcesses));
            }
        }

        // Currently Process selected from the processes that are not selected yet
        public ProcessData? SelectedNewProcess { get; set; }
        // Currently Process selected from the selected processes
        public ProcessData? SelectedActiveProcess { get; set; }


        public MainViewModel()
        {
            processCollector = new ProcessCollector();
        }

        // Called when the Window is first loaded up
        public void OnWindowLoaded()
        {
            // Collect all processes
            processCollector.UpdateProcesses();
            // Map their names and sort the list
            List<string> processNames = processCollector.Processes.Select(p => p.ProcessName).ToList();
            processNames.Sort();
            // Update Binding Variable to show processes
            Processes = new ObservableCollection<ProcessData>(processNames.Select(p => new ProcessData(p)));
        }

        // Called when the Refresh Button is clicked
        public void OnRefreshClicked()
        {
            // if we are currently running a timer, stop it
            if (IsRunning)
            {
                IsRunning = false;
            }
            // update the procesc list
            processCollector.UpdateProcesses();
            // get all new process names and sort them
            List<string> processNames = processCollector.Processes.Select(p => p.ProcessName).ToList();
            processNames.Sort();

            List<ProcessData> newProcessList = new();
            List<ProcessData> selectedProcessList = new();
            // for each process name
            foreach (string processName in processNames)
            {
                // if the process is selected, keep it selected
                if (SelectedProcesses.FirstOrDefault(p => p.Name == processName) == null)
                {
                    newProcessList.Add(new ProcessData(processName));
                }
                // otherwise add it to the not-selected list
                else
                {
                    selectedProcessList.Add(new ProcessData(processName));
                }
            }

            // and update binding variables
            Processes = new ObservableCollection<ProcessData>(newProcessList);
            SelectedProcesses = new ObservableCollection<ProcessData>(selectedProcessList);
        }

        // sets the IsRunning Binding Variable and therefor starts/stops the timer
        public void OnStartStopClicked()
        {
            IsRunning = !IsRunning;
        }

        // Called when a process is selected
        public void OnSelectProcess()
        {
            // if no selection is set, skip
            if (SelectedNewProcess == null)
            {
                return;
            }
            // get the Object that is selected
            ProcessData selectedProcess = SelectedNewProcess;

            // And remove it from the List of unselected processes
            Processes = new ObservableCollection<ProcessData>(Processes.Where(p => p != selectedProcess));

            // create a new list of Selected Processes
            List<ProcessData> newSelectedProcesses = new(SelectedProcesses)
            {
                selectedProcess
            };
            // and sort it
            newSelectedProcesses.Sort((a, b) => a.Name.CompareTo(b.Name));
            // Update binding variable
            SelectedProcesses = new ObservableCollection<ProcessData>(newSelectedProcesses);
        }

        // Called when a process is deselected
        public void OnDeselectProcess()
        {
            // if no process is selected, skip
            if (SelectedActiveProcess == null)
            {
                return;
            }
            // get the selected object
            ProcessData selectedProcess = SelectedActiveProcess;
            // remove it from the selection list
            SelectedProcesses = new ObservableCollection<ProcessData>(SelectedProcesses.Where(p => p != selectedProcess));

            // create new list of deselected processes
            List<ProcessData> newProcesses = new(Processes) { selectedProcess };
            // and sort it
            newProcesses.Sort((a, b) => a.Name.CompareTo((b.Name)));
            // update binding variable
            Processes = new ObservableCollection<ProcessData>(newProcesses);
        }
    }

    // Binding Variable to show Process Names in the GUI
    public class ProcessData
    {
        public ProcessData(string name)
        {
            Name = name;
        }
        public string Name { get; set; } = string.Empty;
    }
}