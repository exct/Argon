
using ToastNotifications;

namespace Argon
{
    public static class NotificationExtensions
    {
        public static void ShowNotification(this Notifier notifier, int PID, string applicationName, string applicationPath, CustomNotification.ActionType actionType)
        {
            notifier.Notify<CustomNotification>(() => new CustomNotification(PID, applicationName, applicationPath, actionType));
        }
    }
}
