using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Argon
{
    public static class Processes
    {
        public static List<Process> ProcessList = new List<Process>();

        public static void GetCurrentProcesses()
        {
            lock (ProcessList)
                ProcessList = Process.GetProcesses().ToList();
        }

    }
}
