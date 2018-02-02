using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Argon
{
    public partial class SuspendedProcesses : UserControl
    {
        public CollectionViewSource SuspendedProcessesViewSource { get; set; } = new CollectionViewSource();
        public CollectionViewSource WhitelistViewSource { get; set; } = new CollectionViewSource();

        public SuspendedProcesses()
        {
            InitializeComponent();
            SuspendedProcessesViewSource.Source = Controller.SuspendedProcessList;
            WhitelistViewSource.Source = Controller.CpuSuspendWhitelist;
            DataContext = this;
        }

        public void UpdateViewSource()
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                SuspendedProcessesDataGrid.Items.Refresh();
                if (SuspendedProcessesDataGrid.Items.Count == 0)
                    EmptyListMsg.Visibility = Visibility.Visible;
                else
                    EmptyListMsg.Visibility = Visibility.Collapsed;
                WhitelistDataGrid.Items.Refresh();
            }));
        }

        private void WhitelistButton_Click(object sender, RoutedEventArgs e)
        {
            var process = (ProcessData)((FrameworkElement)sender).DataContext;
            Controller.AddToWhitelist(process.ID, process.Name, process.Path);
            Controller.ResumeProcess(process.ID);
        }

        private void TerminateButton_Click(object sender, RoutedEventArgs e)
        {
            var process = (ProcessData)((FrameworkElement)sender).DataContext;
            Controller.TerminateProcess(process.ID);
        }

        private void SuspendedProcessesTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateViewSource();
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            var app = (WhitelistedApp)((FrameworkElement)sender).DataContext;
            Controller.RemoveFromWhitelist(app);
            UpdateViewSource();
        }
    }
}
