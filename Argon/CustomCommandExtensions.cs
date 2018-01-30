using CustomNotificationsExample.CustomCommand;
using System;
using ToastNotifications;
using ToastNotifications.Core;

namespace CustomNotificationsExample.CustomMessage
{
    public static class CustomCommandExtensions
    {
        public static void ShowCustomCommand(this Notifier notifier, string message, Action<CustomCommandNotification> confirmAction, Action<CustomCommandNotification> declineAction)
        {
            notifier.Notify<CustomCommandNotification>(() => new CustomCommandNotification(message, confirmAction, declineAction));
        }
    }
}
