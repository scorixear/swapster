using System.Diagnostics;

namespace SwitchyReloaded.Models
{
    // Class for collecting all processes that do have a graphical user interface
    public class ProcessCollector
    {
        public List<Process> Processes { get; private set; } = new List<Process>();

        // Updates processes
        public void UpdateProcesses()
        {
            // A process is considered, if does have a MainWindowHandle (without, we couldn't set the focus)
            // Processes that are miminized to the tray are not possible to maximize / setting focus
            Processes = Process.GetProcesses().Where(p => (long)p.MainWindowHandle != 0).ToList();
        }

        // Maps process names to given process objects retrieved by the UpdateProcesses() method
        public Process? GetProcessByName(string name)
        {
            return Processes.FirstOrDefault(p => p.ProcessName.Contains(name));
        }
    }
}