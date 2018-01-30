using ToastNotifications.Core;

namespace CustomNotificationsExample.CustomCommand
{
    /// <summary>
    /// Interaction logic for CustomCommandDisplayPart.xaml
    /// </summary>
    public partial class CustomCommandDisplayPart : NotificationDisplayPart
    {
        private CustomCommandNotification _notification;

        public CustomCommandDisplayPart(CustomCommandNotification notification)
        {
            InitializeComponent();
            _notification = notification;
            DataContext = notification;
        }
    }
}
