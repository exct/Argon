using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

using LinqToDB;

using ToastNotifications.Core;

namespace Argon
{
    public class CustomNotification : NotificationBase, INotifyPropertyChanged
    {
        public enum ActionType { BlockAllow = 1, UnblockAllow = 2, SuspendWhitelist = 3, TerminateWhitelist = 4 }

        private CustomDisplayPart _displayPart;

        public ICommand Button1Command { get; set; }
        public ICommand Button2Command { get; set; }
        public ICommand CloseCommand { get; set; }

        public CustomNotification(int PID, string applicationName, string applicationPath, ActionType actionType, DateTime time)
        {
            Time = time.ToLongTimeString();
            string title = actionType == ActionType.BlockAllow ? "First connection: " :
                           actionType == ActionType.UnblockAllow ? "Blocked connection: " :
                           actionType == ActionType.SuspendWhitelist ? "High CPU load: " :
                           actionType == ActionType.TerminateWhitelist ? "Suspended: " :
                           "";

            Title = title + applicationName;
            Message = applicationPath;

            Button1 = actionType == ActionType.BlockAllow ? "BLOCK" :
                      actionType == ActionType.UnblockAllow ? "UNBLOCK" :
                      actionType == ActionType.SuspendWhitelist ? "SUSPEND" :
                      actionType == ActionType.TerminateWhitelist ? "TERMINATE" :
                      null;

            Button2 = actionType == ActionType.BlockAllow ? "ALLOW" :
                      actionType == ActionType.UnblockAllow ? "ALLOW" :
                      actionType == ActionType.SuspendWhitelist ? "WHITELIST" :
                      actionType == ActionType.TerminateWhitelist ? "WHITELIST" :
                      null;

            BackgroundColor = actionType == ActionType.BlockAllow ?
                                  new SolidColorBrush(Color.FromArgb(255, 0, 182, 0)) :
                              actionType == ActionType.UnblockAllow ?
                                  new SolidColorBrush(Color.FromArgb(255, 182, 0, 0)) :
                              actionType == ActionType.SuspendWhitelist ?
                                  new SolidColorBrush(Color.FromArgb(255, 0, 112, 128)) :
                              actionType == ActionType.TerminateWhitelist ?
                                  new SolidColorBrush(Color.FromArgb(255, 204, 80, 0)) :
                              new SolidColorBrush(Color.FromArgb(255, 0, 0, 0));

            Action _button1Action = actionType == ActionType.BlockAllow ?
                                        new Action(() => Firewall.SetRule(applicationName, applicationPath, false)) :
                                    actionType == ActionType.UnblockAllow ?
                                        new Action(() => Firewall.RemoveRule(applicationName + "__" + applicationPath)) :
                                    actionType == ActionType.SuspendWhitelist ?
                                        new Action(() => Controller.SuspendProcess(PID)) :
                                    actionType == ActionType.TerminateWhitelist ?
                                        new Action(() => Controller.TerminateProcess(PID)) :
                                    new Action(() => { });

            Action _button2Action = actionType == ActionType.BlockAllow ?
                                        new Action(() => Firewall.SetRule(applicationName, applicationPath, true)) :
                                    actionType == ActionType.UnblockAllow ?
                                        new Action(() => Firewall.SetRule(applicationName, applicationPath, true)) :
                                    actionType == ActionType.SuspendWhitelist ?
                                        new Action(() =>
                                        {
                                            Controller.AddToWhitelist(PID, applicationName, applicationPath);
                                            ((MainWindow)Application.Current.MainWindow).SuspendedProcesses.UpdateViewSource();
                                        }) :
                                    actionType == ActionType.TerminateWhitelist ?
                                        new Action(() =>
                                        {
                                            Controller.ResumeProcess(PID);
                                            Controller.AddToWhitelist(PID, applicationName, applicationPath);
                                            ((MainWindow)Application.Current.MainWindow).SuspendedProcesses.UpdateViewSource();
                                        }) :
                                    new Action(() => { });

            var _closeAction = new Action<CustomNotification>(n => n.Close());

            Action setNotificationActivated = new Action(() =>
            {
                Controller.NotificationList.Find(x => x.Type == (int)actionType && x.ApplicationPath == applicationPath && x.Time == time).NotActivated = false;
                ((MainWindow)Application.Current.MainWindow).Notifications.UpdateViewSource();

                using (var db = new ArgonDB())
                    db.NotificationsList
                      .Where(x => x.Type == (int)actionType
                               && x.ApplicationPath == applicationPath
                               && x.Time == time.Ticks)
                      .Set(x => x.NotActivated, 0)
                      .Update();
            });

            Button1Command = new RelayCommand(x => { _button1Action(); setNotificationActivated(); _closeAction(this); });
            Button2Command = new RelayCommand(x => { _button2Action(); setNotificationActivated(); _closeAction(this); });
            CloseCommand = new RelayCommand(x => _closeAction(this));
        }

        public override NotificationDisplayPart DisplayPart => _displayPart ?? (_displayPart = new CustomDisplayPart(this));

        #region binding properties

        private string _title;
        public string Title
        {
            get {
                return _title;
            }
            set {
                _title = value;
                OnPropertyChanged();
            }
        }

        private string _message;

        public string Message
        {
            get {
                return _message;
            }
            set {
                _message = value;
                OnPropertyChanged();
            }
        }
        public string Time { get; set; }
        public string Button1 { get; private set; }
        public string Button2 { get; private set; }
        public SolidColorBrush BackgroundColor { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName]string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null)
                handler.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
