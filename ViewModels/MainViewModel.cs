using Swapster.Models;
using Swapster.Utils;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Media;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Input;

namespace Swapster.ViewModels
{
    public class MainViewModel : PropertyChangedBase
    {
        // Model class for collecting information about running processes
        private readonly ProcessCollector processCollector;
        private readonly ProcessSwitcher processSwitcher;

        private string _errorTitle = "Error";
        public string ErrorTitle
        {
            get => _errorTitle;
            set
            {
                _errorTitle = value;
                NotifyOfPopertyChanged(nameof(ErrorTitle));
            }
        }
        private string _errorMessage = "Fehlermeldung unkown";
        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                _errorMessage = value;
                NotifyOfPopertyChanged(nameof(ErrorMessage));
            }
        }
        private bool _showError = false;
        public bool ShowError
        {
            get => _showError;
            set
            {
                _showError = value;
                NotifyOfPopertyChanged(nameof(ShowError));
            }
        }

        public ICommand OkClickCommand { get; set; }

        public void OnErrorOkClick(object? parameter)
        {
            ShowError = false;
        }

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
                        List<Process> processes = new();
                        foreach (ProcessData p in SelectedProcesses)
                        {
                            Process? process = processCollector.GetProcessByName(p.Name);
                            if (process == null)
                            {
                                ErrorMessage = $"Konnte nicht zum Prozess {p.Name} wechseln. Bitte einmal auf \"Refresh\" klicken!";
                                ShowError = true;
                                return;
                            }
                            processes.Add(process);
                        }
                        processSwitcher.TimerTickEvent += Timer_Elapsed;
                        processSwitcher.ProcessSwitchErrorEvent += ProcessSwitcher_ProcessSwitchErrorEvent;
                        bool success = processSwitcher.Start(processes, intTimerInterval, _selectedSwitchType);
                        if (success == false)
                        {
                            return;
                        }
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
                    try
                    {
                        processSwitcher.Stop();
                    }
                    catch
                    {
                        ErrorMessage = $"Interner Error. Die Anwendung muss neugstartet werden!";
                        ShowError = true;
                    }
                    // then set the label
                    RunningState = "Start";
                }
                // update internal variable
                _isRunning = value;
                NotifyOfPopertyChanged(nameof(IsRunning));
            }
        }

        // Binding for the Timer Length
        public string TimerLength { get; set; } = "60";
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

        private ProcessSwitcher.ProcessSwitchType _selectedSwitchType = ProcessSwitcher.ProcessSwitchType.AppActivate;
        public string SelectedSwitchMethod
        {
            get
            {
                return _selectedSwitchType switch
                {
                    ProcessSwitcher.ProcessSwitchType.SetForegroundWindow => "SetForeground",
                    ProcessSwitcher.ProcessSwitchType.SwitchToThisWindow => "SwitchTo",
                    _ => "AppActivate",
                };
            }

            set
            {
                _selectedSwitchType = value switch
                {
                    "SetForeground" => ProcessSwitcher.ProcessSwitchType.SetForegroundWindow,
                    "SwitchTo" => ProcessSwitcher.ProcessSwitchType.SwitchToThisWindow,
                    _ => ProcessSwitcher.ProcessSwitchType.AppActivate,
                };
                NotifyOfPopertyChanged(nameof(SelectedSwitchMethod));
            }
        }

        public bool SoundChecked { get; set; }

        private readonly SoundPlayer? doneSound;
        private readonly SoundPlayer? preSound;
        public MainViewModel()
        {
            processCollector = new ProcessCollector();
            processSwitcher = new ProcessSwitcher();
            OkClickCommand = new DelegateCommand(OnErrorOkClick);
        }

        private void ProcessSwitcher_ProcessSwitchErrorEvent(string processName)
        {
            ErrorMessage = $"Konnte nicht zum Prozess {processName} wechseln. Bitte einmal auf \"Refresh\" klicken!";
            ShowError = true;
            IsRunning = false;
        }

        /// <summary>
        /// Called every second when the timer elapsed
        /// </summary>
        /// <param name="timerLeft"></param>
        private void Timer_Elapsed(int timerLeft)
        {
            TimerCountdown = $"Switch in {timerLeft} Sekunden";
            if (SoundChecked && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (timerLeft < 4 && timerLeft > 0)
                {
                    Assembly assembly = Assembly.GetExecutingAssembly();
                    string prePath = assembly.GetManifestResourceNames().Single(str => str.EndsWith("pre.wav"));
                    Stream? stream = assembly.GetManifestResourceStream(prePath);
                    SoundPlayer player = new(stream);
                    player.Play();

                }
                else if (timerLeft == 0)
                {
                    Assembly assembly = Assembly.GetExecutingAssembly();
                    string prePath = assembly.GetManifestResourceNames().Single(str => str.EndsWith("done.wav"));
                    Stream? stream = assembly.GetManifestResourceStream(prePath);
                    SoundPlayer player = new(stream);
                    player.Play();
                }
            }
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