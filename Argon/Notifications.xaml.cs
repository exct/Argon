using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

using LinqToDB;

using MahApps.Metro.IconPacks;

namespace Argon
{
    public partial class Notifications : UserControl
    {
        public CollectionViewSource NotificationsViewSource { get; set; } = new CollectionViewSource();
        private ObservableCollection<NotificationItem> NotificationList;
        public Notifications()
        {
            InitializeComponent();
            UpdateViewSource();
            DataContext = this;
        }

        public void UpdateViewSource()
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                NotificationList = Controller.NotificationList.ToObservableCollection();
                NotificationsViewSource.Source = NotificationList;
                NotificationsViewSource.SortDescriptions.Add(new SortDescription("Time", ListSortDirection.Descending));
                NotificationsDataGrid.Items.Refresh();
            }));
        }

        private void ActionButton_Click(object sender, RoutedEventArgs e)
        {
            var notif = (NotificationItem)((FrameworkElement)sender).DataContext;

            if (notif.Type == (int)CustomNotification.ActionType.BlockAllow) {
                Firewall.SetRule(notif.ApplicationName, notif.ApplicationPath, false);
            }
            else if (notif.Type == (int)CustomNotification.ActionType.UnblockAllow) {
                Firewall.RemoveRule(notif.ApplicationName, notif.ApplicationPath);
            }
            else if (notif.Type == (int)CustomNotification.ActionType.SuspendWhitelist ||
                     notif.Type == (int)CustomNotification.ActionType.TerminateWhitelist) {
                Controller.AddToWhitelist(notif.ApplicationPath);
            }

            Task.Run(() =>
            {
                Controller.NotificationList.Find(x => x == notif).NotActivated = false;

                using (var db = new ArgonDB())
                    db.NotificationsList
                      .Where(x => x.Type == notif.Type
                               && x.ApplicationPath == notif.ApplicationPath
                               && x.Time == notif.Time.Ticks)
                      .Set(x => x.NotActivated, 0)
                      .Update();

                UpdateViewSource();
            });

        }

        private void MarkAllAsReadButton_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(() =>
            {
                Controller.NotificationList.ForEach(x => x.NotActivated = false);

                UpdateViewSource();

                using (var db = new ArgonDB())
                    db.NotificationsList
                      .Set(x => x.NotActivated, 0)
                      .UpdateAsync();
            });
        }
    }

    public class NotificationItem
    {
        public PackIconFontAwesomeKind IconKind { get; set; }
        public SolidColorBrush IconColor { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string ApplicationName { get; set; }
        public string ApplicationPath { get; set; }
        public int Type { get; set; }
        public string ButtonLabel { get; set; }
        public bool NotActivated { get; set; }
        public DateTime Time { get; set; }
    }
}
