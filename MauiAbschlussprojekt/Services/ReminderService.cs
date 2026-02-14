using Plugin.LocalNotification;

namespace MauiAbschlussprojekt.Services
{
    public interface IReminderService
    {
        Task<bool> RequestPermissionAsync();
        void StartPeriodicNotifications(int intervalMinutes, int startHour, int endHour);
        void StopPeriodicNotifications();
    }

    public class ReminderService : IReminderService
    {
        private Timer? _notificationTimer;

        public async Task<bool> RequestPermissionAsync()
        {
#if ANDROID || IOS
            try
            {
                if (await LocalNotificationCenter.Current.AreNotificationsEnabled() == false)
                {
                    var result = await LocalNotificationCenter.Current.RequestNotificationPermission();
                    return result != null;
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fehler bei Benachrichtigungs-Berechtigung: {ex.Message}");
                return false;
            }
#else
            return await Task.FromResult(true);
#endif
        }

        public void StartPeriodicNotifications(int intervalMinutes, int startHour, int endHour)
        {
#if ANDROID || IOS
            StopPeriodicNotifications();

            _notificationTimer = new Timer(_ =>
            {
                var now = DateTime.Now;
                bool isInTimeWindow;

                // Sonderfall: 0-0 bedeutet ganztägig (24 Stunden)
                if (startHour == 0 && endHour == 0)
                {
                    isInTimeWindow = true;
                }
                // Normalfall: Zeitfenster innerhalb eines Tages (z.B. 8-22)
                else if (startHour <= endHour)
                {
                    isInTimeWindow = now.Hour >= startHour && now.Hour <= endHour;
                }
                // Über Mitternacht: Zeitfenster geht über Mitternacht (z.B. 22-8)
                else
                {
                    isInTimeWindow = now.Hour >= startHour || now.Hour <= endHour;
                }

                if (isInTimeWindow)
                {
                    ShowNotification();
                }
            }, null, TimeSpan.Zero, TimeSpan.FromMinutes(intervalMinutes));
#endif
        }

        public void StopPeriodicNotifications()
        {
            _notificationTimer?.Dispose();
            _notificationTimer = null;

#if ANDROID || IOS
            LocalNotificationCenter.Current.CancelAll();
#endif
        }

#if ANDROID || IOS
        private async void ShowNotification()
        {
            try
            {
                var notification = new NotificationRequest
                {
                    NotificationId = 1000,
                    Title = "Trink-Erinnerung",
                    Description = "Zeit, etwas Wasser zu trinken!",
                    BadgeNumber = 1,
                    CategoryType = NotificationCategoryType.Status
                };

                await LocalNotificationCenter.Current.Show(notification);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fehler beim Anzeigen der Benachrichtigung: {ex.Message}");
            }
        }
#endif
    }
}