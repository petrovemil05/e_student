import 'dart:async';
import 'dart:math';
import 'package:intl/intl.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'tu_api_service.dart';
import 'notification_service.dart';

class GradeMonitorService {
  final String fnum;
  final String egn;
  final TuApiService _api = TuApiService();
  final _rng = Random();

  // Callback to update status without relying on NotificationService
  final Function(String title, String body)? onStatusUpdate;

  static const int persistentNotifId = 1001;
  static const int gradeAlertNotifId = 1002;
  static const String _prefLastCountKey = "last_grade_count";

  GradeMonitorService({
    required this.fnum,
    required this.egn,
    this.onStatusUpdate,
  });

  Future<void> checkOnce() async {
    try {
      await _checkGrades();
    } catch (e) {
      _updateStatus("❌ Грешка при проверка", e.toString());
      try {
        NotificationService.showAlert(gradeAlertNotifId, "Грешка при проверка", e.toString());
      } catch (_) {}
    }
  }

  void _updateStatus(String title, String body) {
    if (onStatusUpdate != null) {
      onStatusUpdate!(title, body);
    } else {
      NotificationService.showPersistent(persistentNotifId, title, body);
    }
  }

  Future<void> _checkGrades() async {
    try {
      String time = DateFormat('HH:mm:ss').format(DateTime.now());
      _updateStatus("⏳ Проверявам…", time);

      final prefs = await SharedPreferences.getInstance();
      int lastGradeCount = prefs.getInt(_prefLastCountKey) ?? -1;

      String html = await _api.getHtmlAsync(fnum, egn);
      int currentCount = _countOtsenka(html);

      if (lastGradeCount == -1) {
        await prefs.setInt(_prefLastCountKey, currentCount);
        _updateStatus(
          "✅ Активно следене",
          "Първа проверка: $time | Оценки: $currentCount",
        );
      } else if (currentCount > lastGradeCount) {
        int newGrades = currentCount - lastGradeCount;
        int previousCount = lastGradeCount;
        await prefs.setInt(_prefLastCountKey, currentCount);

        String alertBody = newGrades == 1
            ? "Получихте нова оценка в e-university!"
            : "Получихте $newGrades нови оценки в e-university!";

        _updateStatus(
          "🎓 Нова оценка засечена!",
          "$time | Беше: $previousCount → Сега: $currentCount",
        );

        try {
          NotificationService.showAlert(gradeAlertNotifId, "🎓 Нова оценка!", alertBody);
        } catch (_) {}
      } else {
        await prefs.setInt(_prefLastCountKey, currentCount);
        Duration next = timeUntilNextHalfHour();
        String nextTime = DateFormat('HH:mm').format(DateTime.now().add(next));

        _updateStatus(
          "✅ Няма промяна",
          "Проверено: $time | Оценки: $currentCount | Следваща: $nextTime",
        );
      }
    } catch (e) {
      String time = DateFormat('HH:mm:ss').format(DateTime.now());
      _updateStatus("❌ Грешка при проверка", "$time | $e");
    }
  }

  /// Returns the delay until the next :00 or :30 mark, plus a random jitter
  /// of ±2 minutes (±120 seconds) so the exact hit time varies each cycle.
  Duration timeUntilNextHalfHour() {
    final now = DateTime.now();
    final int nextMinute = now.minute < 30 ? 30 : 60;
    DateTime next = DateTime(now.year, now.month, now.day, now.hour, 0)
        .add(Duration(minutes: nextMinute));
    if (!next.isAfter(now)) {
      next = next.add(const Duration(hours: 1));
    }

    // Jitter: random value in [-120, +120] seconds
    final int jitterSeconds = _rng.nextInt(241) - 120; // 0..240 → -120..+120
    final Duration jitter = Duration(seconds: jitterSeconds);
    final Duration base = next.difference(now);

    // Guard: never return a negative delay (e.g. jitter lands us in the past)
    final Duration result = base + jitter;
    return result.isNegative ? Duration.zero : result;
  }

  int _countOtsenka(String html) {
    if (html.isEmpty) return 0;
    final pattern = RegExp(r'oценк[аи]', caseSensitive: false);
    return pattern.allMatches(html).length;
  }
}