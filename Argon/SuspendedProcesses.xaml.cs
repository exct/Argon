using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Argon
{
    public partial class SuspendedProcesses : UserControl
    {
        public CollectionViewSource SuspendedProcessesViewSource { get; set; } = new CollectionViewSource();

        public SuspendedProcesses()
        {
            InitializeComponent();
            SuspendedProcessesViewSource.Source = Controller.SuspendedProcessList;
            DataContext = this;
        }

        public void UpdateViewSource()
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                SuspendedProcessesViewSource.Source = Controller.SuspendedProcessList;
                SuspendedProcessesDataGrid.Items.Refresh();
            }));
        }

        private void WhitelistButton_Click(object sender, RoutedEventArgs e)
        {
            var process = (ProcessData)((FrameworkElement)sender).DataContext;
            Controller.AddToWhitelist(process.ID, process.Path);
            Controller.ResumeProcess(process.ID);
        }

        private void TerminateButton_Click(object sender, RoutedEventArgs e)
        {
            var process = (ProcessData)((FrameworkElement)sender).DataContext;
            Controller.TerminateProcess(process.ID);
        }

    }
}
