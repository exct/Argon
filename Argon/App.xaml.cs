using System;
using System.Data.SQLite;
using System.Linq;
using System.Threading;
using System.Windows;

namespace Argon
{
    public partial class App : Application
    {
        private static Mutex _mutex = null;

        void App_Startup(object sender, StartupEventArgs e)
        {
            Current.Properties["WindowsVersion"] = GetWindowsVersion();
            CreateDbIfNotExist();
            Controller.Initialize();
            var mainWindow = new MainWindow();
            while (true)
                if (mainWindow.IsInitialized) {
                    mainWindow.Show();
                    break;
                }
        }


        protected override void OnStartup(StartupEventArgs e)
        {
            _mutex = new Mutex(true, "Argon", out bool createdNew);

            if (createdNew)
                base.OnStartup(e);
            else {
                PInvokes.PostMessage(
                    (IntPtr)PInvokes.HWND_BROADCAST,
                    PInvokes.WM_SHOWME,
                    IntPtr.Zero,
                    IntPtr.Zero);
                Current.Shutdown(); //app is already running! exit the application  
            }
        }

        private void CreateDbIfNotExist()
        {
            using (var db = new ArgonDB()) {
                try {
                    db.Config.Any();
                }
                catch {
                    string sql = "BEGIN TRANSACTION;" +
                                "DROP TABLE IF EXISTS `ProcessCounters`;" +
                                "CREATE TABLE IF NOT EXISTS `ProcessCounters` (" +
                                "	`Time`	INTEGER NOT NULL," +
                                "	`Name`	TEXT NOT NULL," +
                                "	`Path`	TEXT NOT NULL," +
                                "	`ProcessorLoadPercent`	REAL NOT NULL" +
                                ");" +
                                "DROP TABLE IF EXISTS `Notifications`;" +
                                "CREATE TABLE IF NOT EXISTS `Notifications` (" +
                                "	`Time`	INTEGER," +
                                "	`ApplicationName`	TEXT," +
                                "	`ApplicationPath`	TEXT," +
                                "	`Type`	INTEGER," +
                                "	`NotActivated`	INTEGER" +
                                ");" +
                                "DROP TABLE IF EXISTS `NetworkTraffic`;" +
                                "CREATE TABLE IF NOT EXISTS `NetworkTraffic` (" +
                                "	`Time`	INTEGER NOT NULL," +
                                "	`ApplicationName`	TEXT NOT NULL," +
                                "	`ProcessName`	TEXT NOT NULL," +
                                "	`FilePath`	TEXT NOT NULL," +
                                "	`Sent`	INTEGER NOT NULL," +
                                "	`Recv`	INTEGER NOT NULL," +
                                "	`SourceAddr`	TEXT NOT NULL," +
                                "	`SourcePort`	INTEGER NOT NULL," +
                                "	`DestAddr`	TEXT NOT NULL," +
                                "	`DestPort`	INTEGER NOT NULL," +
                                "	`Type`	INTEGER NOT NULL," +
                                "	`ProcessID`	INTEGER" +
                                ");" +
                                "DROP TABLE IF EXISTS `CpuSuspendWhitelist`;" +
                                "CREATE TABLE IF NOT EXISTS `CpuSuspendWhitelist` (" +
                                "	`Name`	TEXT NOT NULL," +
                                "	`Path`	TEXT NOT NULL UNIQUE" +
                                ");" +
                                "DROP TABLE IF EXISTS `Config`;" +
                                "CREATE TABLE IF NOT EXISTS `Config` (" +
                                "	`Name`	TEXT," +
                                "	`Value`	INTEGER" +
                                ");" +
                                "INSERT INTO `Config` VALUES ('NotifyNewApplication',1);" +
                                "INSERT INTO `Config` VALUES ('BlockNewConnections',0);" +
                                "INSERT INTO `Config` VALUES ('NotifyHighCpu',1);" +
                                "INSERT INTO `Config` VALUES ('SuspendHighCpu',0);" +
                                "INSERT INTO `Config` VALUES ('HighCpuThreshold',30);" +
                                "DROP INDEX IF EXISTS `processor_time_index`;" +
                                "CREATE INDEX IF NOT EXISTS `processor_time_index` ON `ProcessCounters` (" +
                                "	`time`	DESC" +
                                ");" +
                                "DROP INDEX IF EXISTS `network_time_path_index`;" +
                                "CREATE INDEX IF NOT EXISTS `network_time_path_index` ON `NetworkTraffic` (" +
                                "	`time`	DESC," +
                                "	`filepath`," +
                                "	`ApplicationName`" +
                                ");" +
                                "DROP INDEX IF EXISTS `network_time_index`;" +
                                "CREATE INDEX IF NOT EXISTS `network_time_index` ON `NetworkTraffic` (" +
                                "	`time`	DESC" +
                                ");" +
                                "DROP INDEX IF EXISTS `network_applicationname_index`;" +
                                "CREATE INDEX IF NOT EXISTS `network_applicationname_index` ON `NetworkTraffic` (" +
                                "	`ApplicationName`" +
                                ");" +
                                "COMMIT;";

                    SQLiteCommand cmd = new SQLiteCommand(sql, new SQLiteConnection(db.ConnectionString).OpenAndReturn());
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private string GetWindowsVersion()
        {
            OperatingSystem oSVersion = Environment.OSVersion;
            switch (oSVersion.Version.Major) {
                case 5:
                    UnsupportedWindowsVer("XP");
                    break;
                case 6:
                    switch (oSVersion.Version.Minor) {
                        case 0:
                            UnsupportedWindowsVer("Vista");
                            break;
                        case 1:
                            return "7";
                        case 2:
                            return "8";
                    }
                    break;
                case 10:
                    return "10";
            }
            return "10";
        }

        private void UnsupportedWindowsVer(string ver)
        {
            MessageBox.Show("Windows " + ver + " not supported.", "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error,
                            MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
            Current.Shutdown();
        }


    }
}
