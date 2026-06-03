using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace e_student
{
    /// <summary>
    /// Provides grade checking capability for the foreground service.
    /// The foreground service manages the background loop and calls CheckOnceAsync() at intervals.
    /// </summary>
    public class GradeMonitorService
    {
        // ── state ────────────────────────────────────────────────────────────
        private int _lastGradeCount = -1;   // -1 = not yet measured

        private readonly string _fnum;
        private readonly string _egn;
        private readonly TuApiService _api;

        // Notification IDs
        private const int PersistentNotifId = 1001;
        private const int GradeAlertNotifId = 1002;

        // ── public API ───────────────────────────────────────────────────────
        public GradeMonitorService(string fnum, string egn)
        {
            _fnum = fnum;
            _egn = egn;
            _api = new TuApiService();
        }

        /// <summary>Performs a single grade check and shows appropriate notifications.</summary>
        public async Task CheckOnceAsync()
        {
            try
            {
                await CheckGradesAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CheckOnceAsync error: {ex}");
                ShowErrorNotification($"CheckOnceAsync: {ex.Message}");
            }
        }

        /// <summary>Stops monitoring (for cleanup purposes).</summary>
        public void Stop()
        {
            _lastGradeCount = -1;
        }

        // ── background loop ──────────────────────────────────────────────────
        private async Task CheckGradesAsync(CancellationToken ct)
        {
            string? html = null;
            try
            {
                UpdatePersistentNotification("⏳ Проверявам…", DateTime.Now.ToString("HH:mm:ss"));

                html = await _api.GetHtmlAsync(_fnum, _egn);
                int currentCount = CountOtsenka(html);
                int previousCount = _lastGradeCount;

                if (_lastGradeCount == -1)
                {
                    // First check — just record baseline
                    _lastGradeCount = currentCount;
                    UpdatePersistentNotification(
                        "✅ Активно следене",
                        $"Първа проверка: {DateTime.Now:HH:mm:ss} | Оценки: {currentCount}");
                }
                else if (currentCount > _lastGradeCount)
                {
                    // More grades now than before — new grades received!
                    int newGrades = currentCount - _lastGradeCount;
                    _lastGradeCount = currentCount;
                    ShowGradeAlert(newGrades);
                    UpdatePersistentNotification(
                        "🎓 Нова оценка засечена!",
                        $"{DateTime.Now:HH:mm:ss} | Беше: {previousCount} → Сега: {currentCount}");
                }
                else
                {
                    // No change
                    _lastGradeCount = currentCount;
                    TimeSpan next = TimeUntilNextHalfHour();
                    UpdatePersistentNotification(
                        "✅ Няма промяна",
                        $"Проверено: {DateTime.Now:HH:mm:ss} | Оценки: {currentCount} | Следваща: {DateTime.Now.Add(next):HH:mm}");
                }
            }
            catch (Exception ex)
            {
                // Show full error in notification so you can see what went wrong
                UpdatePersistentNotification(
                    "❌ Грешка при проверка",
                    $"{DateTime.Now:HH:mm:ss} | {ex.GetType().Name}: {ex.Message}");
                ShowErrorNotification(ex.Message);
            }
            finally
            {
                html = null;
                GC.Collect(0, GCCollectionMode.Optimized);
            }
        }

        // ── helpers ──────────────────────────────────────────────────────────

        /// <summary>Returns how long to wait until the next :00 or :30 mark.</summary>
        private static TimeSpan TimeUntilNextHalfHour()
        {
            DateTime now = DateTime.Now;
            int nextMinute = now.Minute < 30 ? 30 : 60;
            DateTime next = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0)
                                .AddMinutes(nextMinute);
            if (next <= now) next = next.AddHours(1);
            return next - now;
        }

        /// <summary>Counts occurrences of "оценка" in the HTML (handles both Cyrillic о and Latin o).</summary>
        private static int CountOtsenka(string html)
        {
            if (string.IsNullOrEmpty(html))
                return 0;

            int count = Regex.Matches(html, @"oценк[аи]", RegexOptions.IgnoreCase).Count;
            return count;
        }

        // ── notification wrappers ─────────────────────────────────────────────
        // These delegate to the platform-specific helper so the service itself
        // stays free of #if directives and is easy to unit-test.

        private static void UpdatePersistentNotification(string title, string body) =>
            NotificationHelper.ShowPersistent(PersistentNotifId, title, body);

        private static void ShowGradeAlert(int count) =>
            NotificationHelper.ShowAlert(
                GradeAlertNotifId,
                "🎓 Нова оценка!",
                count == 1
                    ? "Получихте нова оценка в e-university!"
                    : $"Получихте {count} нови оценки в e-university!");

        private static void ShowErrorNotification(string message) =>
            NotificationHelper.ShowAlert(
                GradeAlertNotifId,
                "Грешка при проверка",
                message);
    }
}
