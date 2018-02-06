using ToastNotifications.Core;

namespace Argon
{
    /// <summary>
    /// Interaction logic for CustomCommandDisplayPart.xaml
    /// </summary>
    public partial class CustomDisplayPart : NotificationDisplayPart
    {
        private CustomNotification _notification;

        public CustomDisplayPart(CustomNotification notification)
        {
            InitializeComponent();
            _notification = notification;
            DataContext = notification;
        }
    }
}
