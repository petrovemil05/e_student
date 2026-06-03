using Android.App;
using Android.OS;
using AndroidX.Core.App;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.Content;
using Microsoft.Maui.Controls.PlatformConfiguration;

namespace e_student
{
    [Service(ForegroundServiceType = Android.Content.PM.ForegroundService.TypeDataSync)]
    public class GradeMonitorForegroundService : Service
    {
        public const string ActionStart = "START";
        public const string ActionStop = "STOP";
        public const string ActionCheck = "CHECK";
        public const string ExtraFnum = "fnum";
        public const string ExtraEgn = "egn";

        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(30);

        private DateTime GetNextIntervalTime()
        {
            var now = DateTime.Now;
            var next = now.AddTicks(-(now.Ticks % _checkInterval.Ticks));
            if (next <= now) next = next.Add(_checkInterval);
            return next;
        }

        private static void SavePref(string key, string value) =>
            Android.App.Application.Context
            .GetSharedPreferences("e_student", Android.Content.FileCreationMode.Private)!
            .Edit()!.PutString(key, value)!.Apply();

        private static string GetPref(string key) =>
            Android.App.Application.Context
            .GetSharedPreferences("e_student", Android.Content.FileCreationMode.Private)!
            .GetString(key, "") ?? "";

        private GradeMonitorService? _monitor;
        private CancellationTokenSource? _cts;
        private Task? _monitoringTask;

        public override IBinder? OnBind(Intent? intent) => null;

        public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
        {
            if (intent?.Action == ActionStop)
            {
                // Stop the monitoring loop
                _cts?.Cancel();
                _cts = null;
                _monitoringTask = null;

                _monitor?.Stop();
                StopForeground(StopForegroundFlags.Remove);
                StopSelf();
                return StartCommandResult.NotSticky;
            }

            // Show the persistent notification (required before anything else)
            StartForeground(1001, BuildNotification("⏰ Следя оценките…", "Инициализиране…"),
                Android.Content.PM.ForegroundService.TypeDataSync);

            if (intent?.Action == ActionStart)
            {
                string fnum = intent.GetStringExtra(ExtraFnum) ?? "";
                string egn  = intent.GetStringExtra(ExtraEgn)  ?? "";
                SavePref("fnum", fnum);
                SavePref("egn", egn);

                _monitor = new GradeMonitorService(fnum, egn);

                // Do an immediate check for testing feedback
                _cts = new CancellationTokenSource();
                _ = DoInitialCheckThenStartLoopAsync(_cts.Token);

                UpdateNotification("⏰ Следя оценките…", $"Първа проверка: изпълнява се...");
                return StartCommandResult.Sticky;
            }

            // If action is Check (from old alarm), we ignore it now because we use the loop.
            // But we still return not sticky to avoid keeping the service alive unnecessarily.
            return StartCommandResult.NotSticky;
        }

        private async Task DoInitialCheckThenStartLoopAsync(CancellationToken token)
        {
            try
            {
                // Do immediate check
                string fnum = GetPref("fnum");
                string egn  = GetPref("egn");
                if (!string.IsNullOrEmpty(fnum) && !string.IsNullOrEmpty(egn))
                {
                    _monitor ??= new GradeMonitorService(fnum, egn);
                    await _monitor.CheckOnceAsync();

                    // Brief pause to let user see the check happened
                    await Task.Delay(TimeSpan.FromSeconds(2), token);
                }
            }
            catch (Android.OS.OperationCanceledException)
            {
                // Expected when stopping
            }
            catch (Exception ex)
            {
                // Show error but continue to start loop
                System.Diagnostics.Debug.WriteLine($"DoInitialCheckThenStartLoopAsync error: {ex}");
                UpdateNotification("❌ Грешка", $"{ex.GetType().Name}: {ex.Message}");
                await Task.Delay(TimeSpan.FromSeconds(2), token);
            }
            finally
            {
                // Now start the regular monitoring loop
                if (!token.IsCancellationRequested)
                {
                    _cts = new CancellationTokenSource();
                    _monitoringTask = RunMonitoringLoopAsync(_cts.Token);
                }
            }
        }

        private Notification BuildNotification(string title, string body)
        {
            var ctx = Android.App.Application.Context;

            var intent = ctx.PackageManager!.GetLaunchIntentForPackage(ctx.PackageName!)!;
            intent.SetFlags(ActivityFlags.SingleTop);
            var pi = PendingIntent.GetActivity(ctx, 0, intent,
                PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

            var stopIntent = new Intent(this, typeof(GradeMonitorForegroundService));
            stopIntent.SetAction(ActionStop);
            var stopPi = PendingIntent.GetService(this, 0, stopIntent,
                PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

            return new NotificationCompat.Builder(this, "grade_monitor_channel")
                .SetSmallIcon(Android.Resource.Drawable.IcDialogInfo)
                .SetContentTitle(title)
                .SetContentText(body)
                .SetOngoing(true)
                .SetPriority(NotificationCompat.PriorityLow)
                .SetContentIntent(pi)
                .AddAction(Android.Resource.Drawable.IcMenuCloseClearCancel, "Спри", stopPi)
                .Build();
        }

        private void UpdateNotification(string title, string body)
        {
            var manager = NotificationManagerCompat.From(this);
            manager.Notify(1001, BuildNotification(title, body));
        }

        private async Task RunMonitoringLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    // Calculate next interval time
                    var next = GetNextIntervalTime();

                    // Wait until then
                    var delayMs = (next - DateTime.Now).TotalMilliseconds;
                    if (delayMs > 0)
                    {
                        await Task.Delay((int)delayMs, token);
                    }

                    // If cancelled during delay, exit
                    if (token.IsCancellationRequested)
                        break;

                    // Perform the check
                    string fnum = GetPref("fnum");
                    string egn  = GetPref("egn");
                    if (!string.IsNullOrEmpty(fnum) && !string.IsNullOrEmpty(egn))
                    {
                        _monitor ??= new GradeMonitorService(fnum, egn);
                        await _monitor.CheckOnceAsync();
                    }
                }
                catch (System.OperationCanceledException)
                {
                    break;
                }
                catch (Exception)
                {
                    await Task.Delay(TimeSpan.FromMinutes(1), token);
                }
            }
        }

        public override void OnDestroy()
        {
            _cts?.Cancel();
            _monitor?.Stop();
            base.OnDestroy();
        }
    }
}