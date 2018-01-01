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
            Controller.Initialize();

            //MainWindow mainWindow = new MainWindow();
            //mainWindow.Show();
        }
        
    }
}
