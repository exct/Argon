using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Timers;
using System.Management;
using LinqToDB;

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
        static long CurrentTime;

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
            lock (ProcessDataList)
                lock (ProcessList)
                    foreach (Process p in ProcessList)
                        AddToProcessDataList(p);
        }

        static void UpdateProcessDataList()
        {
            GetCurrentProcesses();
            lock (NewProcesses)
            {
                if (NewProcesses.Count() > 1)
                    lock (ProcessDataList)
                        lock (ProcessList)
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
                    ProcessorLoadPercent = 0,
                    IsProtected = p.ProcessName == "svchost" ? true : false
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
                    ProcessorLoadPercent = 0,
                    IsProtected = true
                });
            }
            catch { }
        }

        static void GetProcessesUsage()
        {
            UpdateProcessDataList();
            TotalCpuTime = 0;
            TotalCpuLoadPct = (decimal)TotalCpuLoadCounter.NextValue();
            CurrentTime = DateTime.Now.Ticks.NextSecond();

            lock (ProcessDataList)
            {
                lock (ProcessList)
                    foreach (Process p in ProcessList)
                    {
                        var proc = ProcessDataList.Where(x => x.ID == p.Id).FirstOrDefault();
                        if (proc == null)
                            AddToProcessDataList(p);
                        else
                        {
                            if (!proc.IsProtected)
                                if (p.HasExited) continue;
                            proc.Time = CurrentTime;
                            proc.ProcessorTimeDiff = p.TotalProcessorTime.Ticks - proc.ProcessorTime;
                            TotalCpuTime += proc.ProcessorTimeDiff;
                            proc.ProcessorTime = p.TotalProcessorTime.Ticks;
                        }
                    }

                foreach (ProcessData p in ProcessDataList)
                    if (TotalCpuTime == 0)
                        continue;
                    else
                        p.ProcessorLoadPercent = Decimal.Round((p.ProcessorTimeDiff / (decimal)TotalCpuTime * TotalCpuLoadPct), 2);
            }
        }

        static void WriteProcDataToDb()
        {
            lock (ProcessDataList)
            {
                using (var db = new ArgonDB())
                    try
                    {
                        db.BeginTransaction();
                        foreach (ProcessData p in ProcessDataList)
                            if (p.ProcessorLoadPercent != 0)
                                db.Insert(new ProcessCounters
                                {
                                    Time = p.Time,
                                    Name = p.Name,
                                    Path = p.Path,
                                    ProcessorLoadPercent = p.ProcessorLoadPercent
                                });
                        db.CommitTransaction();
                    }
                    catch
                    {
                        db.RollbackTransaction();
                    }
            }

        }

        public class ProcessData
        {
            public long Time { get; set; }
            public int ID { get; set; }
            public string Name { get; set; }
            public string Path { get; set; }
            public long ProcessorTime { get; set; }
            public long ProcessorTimeDiff { get; set; }
            public decimal ProcessorLoadPercent { get; set; }
            public bool IsProtected { get; set; }
        }

    }
}
