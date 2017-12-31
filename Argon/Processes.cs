using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Timers;
using System.Management;

namespace Argon
{
    public static class Processes
    {
        public static List<ProcessData> ProcessDataList = new List<ProcessData>();
        public static List<int> NewProcesses = new List<int>();
        static List<Process> ProcessList = new List<Process>();
        static Dictionary<int, string> Services = new Dictionary<int, string>();
        static Timer timer = new Timer(1000);
        static decimal TotalCpuLoadPct = 0;
        static long TotalCpuTime = 0;
        static ManagementClass mgmtClass = new ManagementClass("Win32_Process");
        public static void Initialize()
        {
            TotalCpuLoadCounter.NextValue();
            GetServices();
            GetCurrentProcesses();
            InitProcessDataList();
            timer.Elapsed += Timer_Elapsed;
            timer.Start();
        }

        static PerformanceCounter TotalCpuLoadCounter = new PerformanceCounter()
        {
            CategoryName = "Processor",
            CounterName = "% Processor Time",
            InstanceName = "_Total"
        };

        static void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            GetProcessesUsage();
            WriteProcDataToDb();
        }

        static void GetCurrentProcesses()
        {
            lock (ProcessList)
                ProcessList = Process.GetProcesses().ToList();
        }

        static void GetServices()
        {
            lock (Services)
                Services.Clear();
            foreach (ManagementObject process in mgmtClass.GetInstances())
                if (process["Name"].ToString() == "svchost.exe")
                    Services.Add(Convert.ToInt32(process["ProcessId"]),
                                 process["CommandLine"] == null ? "" : process["CommandLine"].ToString());
        }

        static string GetServiceName(int PID)
        {
            try
            {
                lock (Services)
                    return Services.Where(x => x.Key == PID).Select(x => x.Value).First();
            }
            catch //refresh Services list if fail on first attempt
            {
                GetServices();
                try
                {
                    lock (Services)
                        return Services.Where(x => x.Key == PID).Select(x => x.Value).First();
                }
                catch //fail to obtain file path
                {
                    return "";
                }
            };
        }

        static void InitProcessDataList()
        {
            lock (ProcessList)
                lock (ProcessDataList)
                    foreach (Process p in ProcessList)
                        AddToProcessDataList(p);
        }

        static void UpdateProcessDataList()
        {
            GetCurrentProcesses();
            lock (NewProcesses)
            {
                if (NewProcesses.Count() > 1)
                    lock (ProcessList)
                        lock (ProcessDataList)
                            foreach (int i in NewProcesses)
                            {
                                var p = ProcessList.Where(x => x.Id == i).FirstOrDefault();
                                if (p != null)
                                    AddToProcessDataList(p);
                            }
                NewProcesses.Clear();
            }
        }

        static void AddToProcessDataList(Process p)
        {
            if (p.Id == 0) return;
            try
            {
                ProcessDataList.Add(new ProcessData
                {
                    ID = p.Id,
                    Name = p.ProcessName,
                    Path = p.ProcessName == "svchost" ? GetServiceName(p.Id) : ProcessList.Where(x => x.Id == p.Id).Select(x => x.MainModule.FileName).First(),
                    ProcessorTime = p.TotalProcessorTime.Ticks,
                    ProcessorTimeDiff = 0,
                    ProcessorUsagePercent = 0,
                    IsSystem = p.ProcessName == "svchost" ? true : false
                });
            }
            catch (System.ComponentModel.Win32Exception)
            {
                ProcessDataList.Add(new ProcessData
                {
                    ID = p.Id,
                    Name = p.ProcessName,
                    Path = p.Id == 4 ? "System" :
                           p.ProcessName == "smss" ? "Session Manager Subsystem" :
                           p.ProcessName == "services" ? "Services Control Manager" :
                           p.ProcessName == "NisSrv" ? "Microsoft Network Realtime Inspection Service" :
                           p.ProcessName == "MsMpEng" ? "Windows Defender" :
                           p.ProcessName == "csrss" ? "Client/Server Runtime Subsystem" :
                           p.ProcessName == "wininit" ? "Windows Initialization Process" :
                           p.ProcessName == "SecurityHealthService" ? "Windows Defender Security Center Service" :
                           p.ProcessName,
                    ProcessorTime = p.TotalProcessorTime.Ticks,
                    ProcessorTimeDiff = 0,
                    ProcessorUsagePercent = 0,
                    IsSystem = true
                });
            }
            catch { }
        }

        static void GetProcessesUsage()
        {
            UpdateProcessDataList();
            TotalCpuTime = 0;
            TotalCpuLoadPct = (decimal)TotalCpuLoadCounter.NextValue();

            lock (ProcessList)
                lock (ProcessDataList)
                    foreach (Process p in ProcessList)
                    {
                        if (p.Id == 0) continue;
                        var proc = ProcessDataList.Where(x => x.ID == p.Id).FirstOrDefault();
                        if (proc == null)
                            AddToProcessDataList(p);
                        else
                        {
                            if (!proc.IsSystem)
                                if (p.HasExited) continue;
                            proc.ProcessorTimeDiff = p.TotalProcessorTime.Ticks - proc.ProcessorTime;
                            TotalCpuTime += proc.ProcessorTimeDiff;
                            proc.ProcessorTime = p.TotalProcessorTime.Ticks;
                        }
                    }

            lock (ProcessDataList)
                foreach (ProcessData p in ProcessDataList)
                {
                    if (TotalCpuTime == 0) continue;
                    p.ProcessorUsagePercent = p.ProcessorTimeDiff / (decimal)TotalCpuTime * TotalCpuLoadPct;
                }
        }

        static void WriteProcDataToDb()
        {

        }

        public class ProcessData
        {
            public int ID { get; set; }
            public string Name { get; set; }
            public string Path { get; set; }
            public long ProcessorTime { get; set; }
            public long ProcessorTimeDiff { get; set; }
            public decimal ProcessorUsagePercent { get; set; }
            public bool IsSystem { get; set; }
        }

    }
}
