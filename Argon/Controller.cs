using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Linq;
using System.Management;
using System.Timers;

using LinqToDB;

namespace Argon
{
    public sealed class Controller
    {
        public static List<NetworkTraffic> NetworkTrafficList = new List<NetworkTraffic>();
        public static List<ProcessData> ProcessDataList = new List<ProcessData>();
        public static List<int> NewProcesses = new List<int>();
        public static ConcurrentDictionary<int, string> Services = new ConcurrentDictionary<int, string>();
        static ManagementClass mgmtClass = new ManagementClass("Win32_Service");
        static List<Process> ProcessList = new List<Process>();
        static Timer timer = new Timer(1000);
        static decimal TotalCpuLoadPct = 0;
        static long TotalCpuTime = 0;
        static long CurrentTime;
        static PerformanceCounter TotalCpuLoadCounter = new PerformanceCounter()
        {
            CategoryName = "Processor",
            CounterName = "% Processor Time",
            InstanceName = "_Total"
        };

        public static void Initialize()
        {
            Process.EnterDebugMode();
            TotalCpuLoadCounter.NextValue();
            GetServices();
            GetCurrentProcesses();
            InitProcessDataList();
            EtwMonitor.Initialize();
            timer.Elapsed += Timer_Elapsed;
            timer.Start();
        }


        static void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            GetProcessesUsage();
            WriteToDb();
        }

        static void GetCurrentProcesses()
        {
            lock (ProcessList)
                ProcessList = Process.GetProcesses().ToList();
        }

        static ConcurrentDictionary<int, string> GetServices()
        {
            lock (Services)
                Services.Clear();
            foreach (ManagementObject service in mgmtClass.GetInstances())
                Services.TryAdd(Convert.ToInt32(service["ProcessId"]), service["DisplayName"]?.ToString());
            return Services;
        }

        static string GetServiceName(int PID)
        {
            lock (Services)
                return Services.Where(x => x.Key == PID).Select(x => x.Value).FirstOrDefault() ?? //if null, call GetServices
                    GetServices().Where(x => x.Key == PID).Select(x => x.Value).FirstOrDefault();
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
            lock (NewProcesses) {
                if (NewProcesses.Count() > 1)
                    lock (ProcessDataList)
                        lock (ProcessList)
                            foreach (int i in NewProcesses) {
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
            try {
                if (p.HasExited) return;
                ProcessDataList.Add(new ProcessData
                {
                    ID = p.Id,
                    Name = p.ProcessName == "svchost" ?
                            (GetServiceName(p.Id) ??
                                (string.IsNullOrWhiteSpace(p.MainModule.FileVersionInfo.FileDescription) ?
                                    (string.IsNullOrWhiteSpace(p.MainModule.FileVersionInfo.ProductName) ?
                                        p.ProcessName : p.MainModule.FileVersionInfo.ProductName) : p.MainModule.FileVersionInfo.FileDescription)) :
                            string.IsNullOrWhiteSpace(p.MainModule.FileVersionInfo.FileDescription) ?
                                (string.IsNullOrWhiteSpace(p.MainModule.FileVersionInfo.ProductName) ?
                                    p.ProcessName : p.MainModule.FileVersionInfo.ProductName) : p.MainModule.FileVersionInfo.FileDescription,
                    Path = p.MainModule.FileName,
                    ProcessorTime = p.TotalProcessorTime.Ticks,
                    ProcessorTimeDiff = 0,
                    ProcessorLoadPercent = 0,
                    IsProtected = p.ProcessName == "svchost" ? true : false
                });
            }
            catch (System.ComponentModel.Win32Exception) {
                ProcessDataList.Add(new ProcessData
                {
                    ID = p.Id,
                    Name = p.Id == 4 ? "System" :
                           p.ProcessName == "smss" ? "Session Manager Subsystem" :
                           p.ProcessName == "services" ? "Services Control Manager" :
                           p.ProcessName == "NisSrv" ? "Microsoft Network Realtime Inspection Service" :
                           p.ProcessName == "MsMpEng" ? "Windows Defender" :
                           p.ProcessName == "csrss" ? "Client/Server Runtime Subsystem" :
                           p.ProcessName == "wininit" ? "Windows Initialization Process" :
                           p.ProcessName == "SecurityHealthService" ? "Windows Defender Security Center Service" :
                           p.ProcessName,
                    Path = p.ProcessName,
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

            lock (ProcessDataList) {
                lock (ProcessList)
                    foreach (Process p in ProcessList) {
                        var proc = ProcessDataList.Where(x => x.ID == p.Id).FirstOrDefault();
                        if (proc == null)
                            AddToProcessDataList(p);
                        else {
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
                        p.ProcessorLoadPercent = decimal.Round((p.ProcessorTimeDiff / (decimal)TotalCpuTime * TotalCpuLoadPct), 2);
            }
        }

        static void WriteToDb()
        {
            using (var db = new ArgonDB())
                try {
                    db.BeginTransaction();
                    lock (ProcessDataList)
                        foreach (ProcessData p in ProcessDataList) {
                            if (p.ProcessorLoadPercent != 0)
                                db.Insert(new ProcessCounter
                                {
                                    Time = p.Time,
                                    Name = p.Name,
                                    Path = p.Path,
                                    ProcessorLoadPercent = p.ProcessorLoadPercent
                                });
                        }
                    lock (NetworkTrafficList) {
                        foreach (NetworkTraffic n in NetworkTrafficList)
                            db.Insert(n);
                        NetworkTrafficList.Clear();
                    }
                    db.CommitTransaction();
                }
                catch { db.RollbackTransaction(); }
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
            public byte[] Icon { get; set; }
        }

    }
}
