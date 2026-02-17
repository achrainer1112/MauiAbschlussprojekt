using Plugin.LocalNotification;

namespace MauiAbschlussprojekt.Services
{
    public interface IReminderService
    {
        Task<bool> RequestPermissionAsync();
        void StartPeriodicNotifications(int intervalMinutes, int startHour, int endHour);
        void StopPeriodicNotifications();
        void ScheduleDailySleepReminder(int bedTimeHour, int bedTimeMinute);
        void CancelSleepReminder();
    }

    public class ReminderService : IReminderService
    {
        private Timer? _waterNotificationTimer;
        private Timer? _sleepReminderTimer;

        // ── Berechtigungen ────────────────────────────────────────────────────
        public async Task<bool> RequestPermissionAsync()
        {
#if ANDROID || IOS
            try
            {
                if (!await LocalNotificationCenter.Current.AreNotificationsEnabled())
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

        // ── Wasser-Erinnerungen (periodisch) ─────────────────────────────────
        public void StartPeriodicNotifications(int intervalMinutes, int startHour, int endHour)
        {
#if ANDROID || IOS
            _waterNotificationTimer?.Dispose();

            _waterNotificationTimer = new Timer(_ =>
            {
                var now = DateTime.Now;
                bool inWindow;

                if (startHour == 0 && endHour == 0)
                    inWindow = true;
                else if (startHour <= endHour)
                    inWindow = now.Hour >= startHour && now.Hour <= endHour;
                else
                    inWindow = now.Hour >= startHour || now.Hour <= endHour;

                if (inWindow)
                    ShowWaterNotification();

            }, null, TimeSpan.Zero, TimeSpan.FromMinutes(intervalMinutes));
#endif
        }

        public void StopPeriodicNotifications()
        {
            _waterNotificationTimer?.Dispose();
            _waterNotificationTimer = null;

#if ANDROID || IOS
            LocalNotificationCenter.Current.Cancel(1000);
#endif
        }

        // ── Schlaf-Erinnerung (täglich 1h vor Schlafenszeit) ─────────────────
        /// <summary>
        /// Plant eine tägliche Schlaf-Erinnerung 1 Stunde vor der Ziel-Schlafenszeit.
        /// Der Timer prüft jede Minute ob die Erinnerungszeit erreicht ist und
        /// sendet dann genau einmal pro Tag eine Benachrichtigung.
        /// Beispiel: bedTimeHour=23, bedTimeMinute=30 → Erinnerung täglich um 22:30.
        /// </summary>
        public void ScheduleDailySleepReminder(int bedTimeHour, int bedTimeMinute)
        {
#if ANDROID || IOS
            CancelSleepReminder();

            // Erinnerungszeit = 1 Stunde vor Schlafenszeit
            // bedTimeHour kann 0–27 sein (Werte >23 = nach Mitternacht)
            int realBedHour = bedTimeHour % 24;
            int reminderHour = realBedHour == 0 ? 23 : realBedHour - 1;
            int reminderMinute = bedTimeMinute;

            // Merken wann zuletzt gefeuert, damit pro Tag nur 1x
            DateTime lastFired = DateTime.MinValue;

            _sleepReminderTimer = new Timer(_ =>
            {
                var now = DateTime.Now;

                // Feuere wenn Stunde+Minute übereinstimmt und heute noch nicht gefeuert
                if (now.Hour == reminderHour &&
                    now.Minute == reminderMinute &&
                    now.Date != lastFired.Date)
                {
                    lastFired = now;
                    ShowSleepReminderNotification(realBedHour, bedTimeMinute);
                }

            }, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
#endif
        }

        public void CancelSleepReminder()
        {
            _sleepReminderTimer?.Dispose();
            _sleepReminderTimer = null;

#if ANDROID || IOS
            LocalNotificationCenter.Current.Cancel(2000);
#endif
        }

        // ── Private: Notifications anzeigen ──────────────────────────────────
#if ANDROID || IOS
        private async void ShowWaterNotification()
        {
            try
            {
                await LocalNotificationCenter.Current.Show(new NotificationRequest
                {
                    NotificationId = 1000,
                    Title = "💧 Trink-Erinnerung",
                    Description = "Zeit, etwas Wasser zu trinken!",
                    BadgeNumber = 1,
                    CategoryType = NotificationCategoryType.Status
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Wasser-Notification Fehler: {ex.Message}");
            }
        }

        private async void ShowSleepReminderNotification(int bedHour, int bedMinute)
        {
            try
            {
                string bedTimeStr = $"{bedHour:D2}:{bedMinute:D2} Uhr";

                await LocalNotificationCenter.Current.Show(new NotificationRequest
                {
                    NotificationId = 2000,
                    Title = "😴 Zeit zum Schlafen!",
                    Description = $"In 1 Stunde ist deine Schlafenszeit ({bedTimeStr}). Bereite dich auf den Schlaf vor.",
                    BadgeNumber = 1,
                    CategoryType = NotificationCategoryType.Status
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Schlaf-Notification Fehler: {ex.Message}");
            }
        }
#endif
    }
}