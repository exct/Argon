using System.Windows;

namespace Argon
{
    public partial class App : Application
    {
        void App_Startup(object sender, StartupEventArgs e)
        {
            Controller.Initialize();

            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
        }
    }
}
