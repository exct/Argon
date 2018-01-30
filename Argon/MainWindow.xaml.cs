using System.Windows;

namespace Argon
{
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            Unloaded += OnUnload;

        }

        private void OnUnload(object sender, RoutedEventArgs e)
        {
            Controller.OnUnload();
        }

    }
}
