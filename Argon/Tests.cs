namespace Argon
{
    class Tests
    {
        public static void GenerateNotifications(int numberOfNotifications)
        {
            for (var i = 1; i < numberOfNotifications + 1; i++) {
                Controller.ShowNotification(0, "Test Application Example", "C:\\Program Files (x86)\\Microsoft Visual Studio\\2017\\Enterprise\\Common7\\IDE\\Remote Debugger\\x64\\msvsmon.exe", (CustomNotification.ActionType)(i % 4 + 1));
            }
        }

    }
}
