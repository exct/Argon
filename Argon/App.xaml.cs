using System;
using System.Windows;


namespace Argon
{
    public partial class App : Application
    {
        void App_Startup(object sender, StartupEventArgs e)
        {
            Current.Properties["WindowsVersion"] = GetWindowsVersion();
            Controller.Initialize();

            var mainWindow = new MainWindow();
            mainWindow.Show();

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
