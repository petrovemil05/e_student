using Microsoft.Maui.Controls;

#if ANDROID
using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;
using Microsoft.Maui.ApplicationModel;
#endif

#if IOS
using UserNotifications;
#endif

namespace e_student
{
    /// <summary>
    /// Thin cross-platform wrapper around native notification APIs.
    /// On Android it uses NotificationCompat for a persistent foreground-style
    /// notification; on iOS it schedules a local UNUserNotification.
    /// </summary>
    public static class NotificationHelper
    {
        private const string ChannelId = "grade_monitor_channel";
        private const string ChannelName = "Проверка на оценки";

#if ANDROID
        private static NotificationManagerCompat? _manager;

        private static NotificationManagerCompat Manager
        {
            get
            {
                if (_manager == null)
                {
                    var ctx = Android.App.Application.Context;
                    _manager = NotificationManagerCompat.From(ctx);

                    // Create the channel once (API 26+)
                    if (OperatingSystem.IsAndroidVersionAtLeast(26))
                    {
                        var channel = new NotificationChannel(
                            ChannelId,
                            ChannelName,
                            NotificationImportance.Low)   // Low = no sound for persistent
                        {
                            Description = "Известия за нови оценки от e-university"
                        };
                        var nm = (NotificationManager?)ctx.GetSystemService(Context.NotificationService);
                        nm?.CreateNotificationChannel(channel);
                    }
                }
                return _manager;
            }
        }

        /// <summary>Show or update a persistent (ongoing) notification.</summary>
        public static void ShowPersistent(int id, string title, string body)
        {
            var ctx = Android.App.Application.Context;
            var builder = new NotificationCompat.Builder(ctx, ChannelId)
                .SetSmallIcon(Android.Resource.Drawable.IcDialogInfo)
                .SetContentTitle(title)
                .SetContentText(body)
                .SetOngoing(true)                          // makes it persistent / non-dismissible
                .SetPriority(NotificationCompat.PriorityLow)
                .SetVisibility(NotificationCompat.VisibilityPublic);

            // Tap → open the app
            var intent = ctx.PackageManager!.GetLaunchIntentForPackage(ctx.PackageName!)!;
            intent.SetFlags(ActivityFlags.SingleTop);
            var pi = PendingIntent.GetActivity(ctx, 0, intent,
                PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);
            builder.SetContentIntent(pi);

            Manager.Notify(id, builder.Build());
        }

        /// <summary>Show a heads-up / alert notification (dismissible).</summary>
        public static void ShowAlert(int id, string title, string body)
        {
            var ctx = Android.App.Application.Context;

            // Use a separate high-importance channel for alerts
            const string alertChannelId = "grade_alert_channel";
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var alertChannel = new NotificationChannel(
                    alertChannelId,
                    "Нови оценки",
                    NotificationImportance.High);
                var nm = (NotificationManager?)ctx.GetSystemService(Context.NotificationService);
                nm?.CreateNotificationChannel(alertChannel);
            }

            var builder = new NotificationCompat.Builder(ctx, alertChannelId)
                .SetSmallIcon(Android.Resource.Drawable.IcDialogInfo)
                .SetContentTitle(title)
                .SetContentText(body)
                .SetAutoCancel(true)
                .SetPriority(NotificationCompat.PriorityHigh)
                .SetDefaults(NotificationCompat.DefaultAll);

            Manager.Notify(id, builder.Build());
        }

        /// <summary>Cancel / remove a notification by ID.</summary>
        public static void Cancel(int id) => Manager.Cancel(id);

#elif IOS

        public static void ShowPersistent(int id, string title, string body)
        {
            // iOS does not support true "ongoing" notifications — we deliver a
            // regular local notification and update it on every check.
            DeliverLocalNotification(id.ToString(), title, body, repeating: false);
        }

        public static void ShowAlert(int id, string title, string body) =>
            DeliverLocalNotification(id.ToString(), title, body, repeating: false);

        public static void Cancel(int id)
        {
            UNUserNotificationCenter.Current.RemovePendingNotificationRequests(
                new[] { id.ToString() });
            UNUserNotificationCenter.Current.RemoveDeliveredNotifications(
                new[] { id.ToString() });
        }

        private static void DeliverLocalNotification(
            string identifier, string title, string body, bool repeating)
        {
            UNUserNotificationCenter.Current.RequestAuthorization(
                UNAuthorizationOptions.Alert | UNAuthorizationOptions.Sound,
                (granted, _) =>
                {
                    if (!granted) return;

                    var content = new UNMutableNotificationContent
                    {
                        Title = title,
                        Body = body,
                        Sound = repeating ? null : UNNotificationSound.Default
                    };

                    // Fire immediately (1-second trigger is the iOS minimum)
                    var trigger = UNTimeIntervalNotificationTrigger.CreateTrigger(1, false);
                    var request = UNNotificationRequest.FromIdentifier(identifier, content, trigger);

                    UNUserNotificationCenter.Current.AddNotificationRequest(request, _ => { });
                });
        }

#else
        // Fallback for Windows / MacCatalyst — write to Debug output
        public static void ShowPersistent(int id, string title, string body) =>
            System.Diagnostics.Debug.WriteLine($"[NOTIF:{id}] {title} — {body}");

        public static void ShowAlert(int id, string title, string body) =>
            System.Diagnostics.Debug.WriteLine($"[ALERT:{id}] {title} — {body}");

        public static void Cancel(int id) =>
            System.Diagnostics.Debug.WriteLine($"[CANCEL:{id}]");
#endif
    }
}