using Microsoft.Maui.ApplicationModel;

namespace MauiAbschlussprojekt.Services
{
    public interface INotificationService
    {
        Task<bool> RequestPermissionAsync();
        void ScheduleNotification(string title, string message, DateTime scheduledTime);
        void CancelAllNotifications();
        void StartPeriodicNotifications(int intervalMinutes, int startHour, int endHour);
        void StopPeriodicNotifications();
    }

    // Basis-Implementierung (muss pro Plattform erweitert werden)
    public class NotificationService : INotificationService
    {
        private Timer? _notificationTimer;

        public async Task<bool> RequestPermissionAsync()
        {
            // In einer echten Implementierung würde hier die Berechtigung abgefragt
            // Für Android: AndroidX.Core.App.NotificationManagerCompat
            // Für iOS: UserNotifications Framework

            await Task.CompletedTask;
            return true;
        }

        public void ScheduleNotification(string title, string message, DateTime scheduledTime)
        {
            // Plattformspezifische Implementierung nötig
            // Android: NotificationManager mit AlarmManager
            // iOS: UNUserNotificationCenter

            System.Diagnostics.Debug.WriteLine($"[Notification] {title}: {message} at {scheduledTime}");
        }

        public void CancelAllNotifications()
        {
            StopPeriodicNotifications();
            System.Diagnostics.Debug.WriteLine("[Notification] All notifications cancelled");
        }

        public void StartPeriodicNotifications(int intervalMinutes, int startHour, int endHour)
        {
            StopPeriodicNotifications();

            _notificationTimer = new Timer(async _ =>
            {
                var now = DateTime.Now;

                // Prüfe ob wir im Zeitfenster sind
                if (now.Hour >= startHour && now.Hour < endHour)
                {
                    ScheduleNotification(
                        "Trink-Erinnerung 💧",
                        "Zeit, etwas Wasser zu trinken!",
                        DateTime.Now
                    );
                }
            }, null, TimeSpan.Zero, TimeSpan.FromMinutes(intervalMinutes));

            System.Diagnostics.Debug.WriteLine($"[Notification] Started periodic notifications every {intervalMinutes} minutes");
        }

        public void StopPeriodicNotifications()
        {
            _notificationTimer?.Dispose();
            _notificationTimer = null;
            System.Diagnostics.Debug.WriteLine("[Notification] Stopped periodic notifications");
        }
    }
}

