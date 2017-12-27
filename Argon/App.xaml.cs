using LinqToDB;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Argon
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        void App_Startup(object sender, StartupEventArgs e)
        {
            EtwMonitor mon = EtwMonitor.Create();


            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
        }
        
    }
}
