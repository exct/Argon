using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

using LinqToDB;

using MahApps.Metro.Controls;

namespace Argon
{
    public partial class MainWindow
    {
        public bool NotifyFirstConn { get { return Controller.NotifyNewApplication; } set { Controller.NotifyNewApplication = value; } }
        public bool BlockFirstConn { get { return Controller.BlockNewConnections; } set { Controller.BlockNewConnections = value; } }
        public bool NotifyHighCPU { get { return Controller.NotifyHighCpu; } set { Controller.NotifyHighCpu = value; } }
        public bool SuspendHighCPU { get { return Controller.SuspendHighCpu; } set { Controller.SuspendHighCpu = value; } }
        private bool SliderInit = false;
        private bool TrayClose = false;
        System.Windows.Forms.NotifyIcon trayIcon = new System.Windows.Forms.NotifyIcon
        {
            Icon = System.Drawing.Icon.ExtractAssociatedIcon(
                        System.Reflection.Assembly.GetEntryAssembly().ManifestModule.Name),
            Visible = true,
            Text = "Argon"
        };

        public MainWindow()
        {
            InitializeComponent();

            LeftWindowCommandsOverlayBehavior = WindowCommandsOverlayBehavior.Always;
            RightWindowCommandsOverlayBehavior = WindowCommandsOverlayBehavior.Always;
            WindowButtonCommandsOverlayBehavior = WindowCommandsOverlayBehavior.Always;
            IconOverlayBehavior = WindowCommandsOverlayBehavior.Always;

            var contextMenu = new System.Windows.Forms.ContextMenu();
            var menuItem = new System.Windows.Forms.MenuItem
            {
                Index = 0,
                Text = "Exit"
            };
            menuItem.Click += delegate (object sender, EventArgs e)
            {
                TrayClose = true;
                Close();
            };
            trayIcon.DoubleClick += delegate (object sender, EventArgs args)
            {
                Show();
                WindowState = WindowState.Normal;
            };
            contextMenu.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] { menuItem });
            trayIcon.ContextMenu = contextMenu;

            Unloaded += OnUnload;
            DataContext = this;

            ThresholdSlider.Value = Controller.ProcessorLoadThreshold;
        }

        private void OnUnload(object sender, RoutedEventArgs e)
        {
            trayIcon.Dispose();
            Controller.OnUnload();
        }

        private void MainTabControl_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (!(e.OriginalSource is MetroAnimatedSingleRowTabControl)) return;
            int index = ((sender as MetroAnimatedSingleRowTabControl)).SelectedIndex;

            if (index == 2)
                FirewallUI.RefreshRuleList();
            else if (index == 3)
                SuspendedProcesses.UpdateViewSource();
            else if (index == 4)
                Notifications.UpdateViewSource();

            e.Handled = true;
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsFlyout.IsOpen = !SettingsFlyout.IsOpen;
            e.Handled = true;
        }

        private void ChkNotifyFirstConn_Click(object sender, RoutedEventArgs e)
        {
            bool val = ((CheckBox)sender).IsChecked ?? false;

            Controller.NotifyNewApplication = val;

            using (var db = new ArgonDB())
                db.Config
                  .Where(x => x.Name == "NotifyNewApplication")
                  .Set(x => x.Value, val ? 1 : 0)
                  .Update();

            e.Handled = true;
        }

        private void ChkBlockFirstConn_Click(object sender, RoutedEventArgs e)
        {
            bool val = ((CheckBox)sender).IsChecked ?? false;

            Controller.BlockNewConnections = val;

            using (var db = new ArgonDB())
                db.Config
                  .Where(x => x.Name == "BlockNewConnections")
                  .Set(x => x.Value, val ? 1 : 0)
                  .Update();

            e.Handled = true;
        }

        private void ChkNotifyHighCPU_Click(object sender, RoutedEventArgs e)
        {
            bool val = ((CheckBox)sender).IsChecked ?? false;

            Controller.NotifyHighCpu = val;

            using (var db = new ArgonDB())
                db.Config
                  .Where(x => x.Name == "NotifyHighCpu")
                  .Set(x => x.Value, val ? 1 : 0)
                  .Update();

            e.Handled = true;
        }

        private void ChkSuspendHighCPU_Click(object sender, RoutedEventArgs e)
        {
            bool val = ((CheckBox)sender).IsChecked ?? false;

            Controller.SuspendHighCpu = val;

            using (var db = new ArgonDB())
                db.Config
                  .Where(x => x.Name == "SuspendHighCpu")
                  .Set(x => x.Value, val ? 1 : 0)
                  .Update();

            e.Handled = true;
        }

        private void ThresholdSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (((Slider)sender).IsMouseCaptureWithin) return;
            UpdateThresholdValue((int)e.NewValue);
        }

        private void ThresholdSlider_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            UpdateThresholdValue((int)((Slider)sender).Value);
        }

        private void UpdateThresholdValue(int threshold)
        {
            if (!SliderInit) {
                SliderInit = true;
                return;
            }

            Controller.ProcessorLoadThreshold = threshold;

            using (var db = new ArgonDB())
                db.Config
                  .Where(x => x.Name == "HighCpuThreshold")
                  .Set(x => x.Value, threshold)
                  .Update();
        }

        private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!TrayClose) {
                e.Cancel = true;
                Hide();
            }
        }
    }
}
