using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Media;

using LinqToDB;

using MahApps.Metro.IconPacks;

using ToastNotifications;
using ToastNotifications.Lifetime;
using ToastNotifications.Position;

using static Argon.CustomNotification;

namespace Argon
{
    public sealed class Controller
    {
        public static bool NotifyNewApplication { get; set; }
        public static bool BlockNewConnections { get; set; }
        public static bool NotifyHighCpu { get; set; }
        public static bool SuspendHighCpu { get; set; }
        public static int ProcessorLoadThreshold { get; set; } = 50;
        public static List<ProcessData> SuspendedProcessList { get; set; } = new List<ProcessData>();
        public static List<NotificationItem> NotificationList { get; set; } = new List<NotificationItem>();
        public static List<WhitelistedApp> CpuSuspendWhitelist = new List<WhitelistedApp>();
        public static List<NetworkTraffic> NetworkTrafficList = new List<NetworkTraffic>();
        public static List<ProcessData> ProcessDataList = new List<ProcessData>();
        public static List<int> NewProcesses = new List<int>();
        public static List<string> NetworkProcessList = new List<string>();
        public static ConcurrentDictionary<int, string> Services = new ConcurrentDictionary<int, string>();

        private static Notifier _notifier = new Notifier(cfg =>
        {
            cfg.LifetimeSupervisor = new CountBasedLifetimeSupervisor(MaximumNotificationCount.UnlimitedNotifications());
            cfg.PositionProvider = new PrimaryScreenPositionProvider(Corner.BottomRight, 10, 40);
            cfg.DisplayOptions.TopMost = true; // set the option to show notifications over other windows
            cfg.DisplayOptions.Width = 350; // set the notifications width
            cfg.Dispatcher = Application.Current.Dispatcher;
        });
        private static ManagementClass mgmtClass = new ManagementClass("Win32_Service");
        private static ConcurrentBag<Process> ProcessList = new ConcurrentBag<Process>();
        private static System.Timers.Timer timer = new System.Timers.Timer(1000);
        private static float TotalCpuLoadPct = 0;
        private static long TotalCpuTime = 0;
        private static long CurrentTime;
        private static PerformanceCounter TotalCpuLoadCounter = new PerformanceCounter()
        {
            CategoryName = "Processor",
            CounterName = "% Processor Time",
            InstanceName = "_Total"
        };

        public static void Initialize()
        {
            ReadNotifications();
            Task.Run(() => { ReadConfig(); });
            Task.Run(() => { TotalCpuLoadCounter.NextValue(); });
            Firewall.Initialize();
            NetworkProcessList = GetNetworkProcessList();
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

        static void ReadConfig()
        {
            using (var db = new ArgonDB()) {
                NotifyNewApplication = db.Config.First(x => x.Name == "NotifyNewApplication").Value == 1;
                BlockNewConnections = db.Config.First(x => x.Name == "BlockNewConnections").Value == 1;
                SuspendHighCpu = db.Config.First(x => x.Name == "SuspendHighCpu").Value == 1;
                NotifyHighCpu = db.Config.First(x => x.Name == "NotifyHighCpu").Value == 1;
                ProcessorLoadThreshold = db.Config.First(x => x.Name == "HighCpuThreshold").Value;
                CpuSuspendWhitelist.Add(new WhitelistedApp { Name = "Argon", Path = Process.GetCurrentProcess().MainModule.FileName });
                db.CpuSuspendWhitelist.ForEachAsync(x => CpuSuspendWhitelist.Add(x));
            }
        }

        static void ReadNotifications()
        {
            using (var db = new ArgonDB())
                db.NotificationsList
                  .Where(x => x.Time > DateTime.Today.AddDays(-7).Ticks)
                  .ForEachAsync(x => AddToNotificationList(
                      x.ApplicationName, x.ApplicationPath, (ActionType)x.Type, new DateTime(x.Time), x.NotActivated == 1));

        }

        public static bool SuspendProcess(int PID)
        {
            bool success = false;
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                try {
                    success = PInvokes.DebugActiveProcess(Convert.ToUInt32(PID));
                    if (success) {
                        SuspendedProcessList.Add(ProcessDataList.First(x => x.ID == PID));
                        ((MainWindow)Application.Current.MainWindow).SuspendedProcesses.UpdateViewSource();
                    }
                }
                catch { }
            }));

            return success;
        }

        public static bool ResumeProcess(int PID)
        {
            bool success = false;
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                try {
                    success = PInvokes.DebugActiveProcessStop(Convert.ToUInt32(PID));
                    if (success) {
                        SuspendedProcessList.RemoveAll(x => x.ID == PID);
                        ((MainWindow)Application.Current.MainWindow).SuspendedProcesses.UpdateViewSource();
                    }
                }
                catch { }
            }));

            return success;
        }

        public static void TerminateProcess(int PID)
        {
            try {
                Process.GetProcessById(PID).Kill();
                ResumeProcess(PID);
            }
            catch { }
        }

        public static void AddToWhitelist(string name, string path)
        {
            AddToWhitelist(0, name, path);
        }
        public static void AddToWhitelist(int PID, string name, string path)
        {
            var app = new WhitelistedApp
            {
                Name = name,
                Path = path
            };

            using (var db = new ArgonDB())
                try {
                    db.InsertAsync(app);
                }
                catch { }

            CpuSuspendWhitelist.Add(app);
            SuspendedProcessList.RemoveAll(x => x.ID == PID);
        }

        public static void RemoveFromWhitelist(WhitelistedApp app)
        {
            CpuSuspendWhitelist.RemoveAll(x => x == app);
            using (var db = new ArgonDB())
                db.DeleteAsync(app);
        }

        public static void ShowNotification(int PID, string applicationName, string applicationPath, CustomNotification.ActionType actionType)
        {
            DateTime time = DateTime.Now;

            Application.Current.Dispatcher.BeginInvoke(
                new Action(() => _notifier.ShowNotification(PID, applicationName, applicationPath, actionType, time)));

            AddToNotificationList(applicationName, applicationPath, actionType, time);

            Task.Run(() =>
            {
                using (var db = new ArgonDB())
                    db.InsertAsync(new Notification
                    {
                        Time = time.Ticks,
                        ApplicationName = applicationName,
                        ApplicationPath = applicationPath,
                        Type = (int)actionType,
                        NotActivated = 1
                    });
            });

            try {
                Application.Current.Dispatcher.BeginInvoke(
                    new Action(() =>
                    {
                        ((MainWindow)Application.Current.MainWindow).Notifications.UpdateViewSource();
                    }));
            }
            catch { }

        }

        public static void AddToNotificationList(string applicationName, string applicationPath, CustomNotification.ActionType actionType, DateTime time)
        {
            AddToNotificationList(applicationName, applicationPath, actionType, time, true);
        }
        public static void AddToNotificationList(string applicationName, string applicationPath, CustomNotification.ActionType actionType, DateTime time, bool notActivated)
        {
            string title = actionType == ActionType.BlockAllow ? "First connection: " :
                           actionType == ActionType.UnblockAllow ? "Blocked connection: " :
                           actionType == ActionType.SuspendWhitelist ? "High CPU load: " :
                           actionType == ActionType.TerminateWhitelist ? "Suspended: " :
                           "";

            string content = actionType == ActionType.BlockAllow ?
                                "Application at the following path initiated a network connection:\n" :
                             actionType == ActionType.UnblockAllow ?
                                "Application at the following path was blocked from connecting to the network:\n" :
                             actionType == ActionType.SuspendWhitelist ?
                                "Application at the following path is using a high percentage of processor time:\n" :
                             actionType == ActionType.TerminateWhitelist ?
                                "Application at the following path was suspended for using a high percentage of processor time:\n" :
                             null;

            string buttonLabel = actionType == ActionType.BlockAllow ? "Block" :
                                 actionType == ActionType.UnblockAllow ? "Unblock" :
                                 actionType == ActionType.SuspendWhitelist ? "Whitelist" :
                                 actionType == ActionType.TerminateWhitelist ? "Whitelist" :
                                 null;

            SolidColorBrush iconColor = actionType == ActionType.BlockAllow ?
                                            new SolidColorBrush(Color.FromArgb(255, 0, 182, 0)) :
                                        actionType == ActionType.UnblockAllow ?
                                            new SolidColorBrush(Color.FromArgb(255, 182, 0, 0)) :
                                        actionType == ActionType.SuspendWhitelist ?
                                            new SolidColorBrush(Color.FromArgb(255, 0, 150, 182)) :
                                        actionType == ActionType.TerminateWhitelist ?
                                            new SolidColorBrush(Color.FromArgb(255, 204, 80, 0)) :
                                        new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
            iconColor.Freeze();

            PackIconFontAwesomeKind iconKind = actionType == ActionType.BlockAllow ?
                                                   PackIconFontAwesomeKind.InfoCircleSolid :
                                               actionType == ActionType.UnblockAllow ?
                                                   PackIconFontAwesomeKind.TimesCircleSolid :
                                               actionType == ActionType.SuspendWhitelist ?
                                                   PackIconFontAwesomeKind.InfoCircleSolid :
                                               actionType == ActionType.TerminateWhitelist ?
                                                   PackIconFontAwesomeKind.MoonSolid :
                                               PackIconFontAwesomeKind.CircleSolid;

            var n = new NotificationItem
            {
                ApplicationName = applicationName,
                ApplicationPath = applicationPath,
                Title = title + applicationName,
                Content = content + applicationPath,
                IconKind = iconKind,
                IconColor = iconColor,
                ButtonLabel = buttonLabel,
                Type = (int)actionType,
                NotActivated = notActivated,
                Time = time
            };

            NotificationList.Add(n);
        }

        static void GetCurrentProcesses()
        {
            lock (ProcessList)
                ProcessList = new ConcurrentBag<Process>(Process.GetProcesses());
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

        public static void InitProcessDataList()
        {
            lock (ProcessDataList) {
                ProcessDataList = new List<ProcessData>();
                lock (ProcessList)
                    Parallel.ForEach(ProcessList, (p) =>
                    {
                        AddToProcessDataList(p);
                    });
            }
        }

        public static void UpdateProcessDataList()
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
            TotalCpuLoadPct = TotalCpuLoadCounter.NextValue();
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
                    if (p.ProcessorTimeDiff <= 0)
                        p.ProcessorLoadPercent = 0;
                    else {
                        p.ProcessorLoadPercent = Math.Round((p.ProcessorTimeDiff / (double)TotalCpuTime * TotalCpuLoadPct), 2);
                        if ((NotifyHighCpu || SuspendHighCpu)
                            && p.ProcessorLoadPercent > ProcessorLoadThreshold
                            && !CpuSuspendWhitelist.Any(x => x.Path == p.Path)) {
                            if (SuspendHighCpu)
                                try {
                                    SuspendProcess(p.ID);
                                    ShowNotification(p.ID, p.Name, p.Path, CustomNotification.ActionType.TerminateWhitelist);
                                }
                                catch { }
                            else if (NotifyHighCpu) {
                                if (!NotificationList.Any(x => x.NotActivated && x.Type == (int)ActionType.SuspendWhitelist
                                                          && x.ApplicationPath == p.Path))
                                    ShowNotification(p.ID, p.Name, p.Path, CustomNotification.ActionType.SuspendWhitelist);
                            }
                        }
                    }
            }
        }

        static void WriteToDb()
        {
            using (var db = new ArgonDB())
                try {
                    db.BeginTransaction();
                    lock (ProcessDataList)
                        foreach (ProcessData p in ProcessDataList)
                            if (p.ProcessorLoadPercent != 0)
                                db.InsertAsync(new ProcessCounter
                                {
                                    Time = p.Time,
                                    Name = p.Name,
                                    Path = p.Path,
                                    ProcessorLoadPercent = p.ProcessorLoadPercent
                                });

                    lock (NetworkTrafficList) {
                        foreach (NetworkTraffic n in NetworkTrafficList)
                            db.InsertAsync(n);
                        NetworkTrafficList.Clear();
                    }
                    db.CommitTransaction();
                }
                catch { db.RollbackTransaction(); }
        }

        static List<string> GetNetworkProcessList()
        {
            using (var db = new ArgonDB())
                return db.NetworkTraffic
                         .Select(x => x.FilePath)
                         .Distinct()
                         .ToList();
        }


        public static void OnUnload()
        {
            EtwMonitor.EtwSession.Dispose();
            _notifier.Dispose();
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
        public double ProcessorLoadPercent { get; set; }
        public bool IsProtected { get; set; }
        public byte[] Icon { get; set; }
    }
}
