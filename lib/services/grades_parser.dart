import 'package:html_unescape/html_unescape.dart';
import '../models/grade_item.dart';

class GradesParser {
  final unescape = HtmlUnescape();

  List<GradeItem> parse(String html) {
    List<GradeItem> grades = [];

    final pattern = RegExp(
      r'<td colspan=4><center><b>(?<semester>[^<]+)</b>|<span[^>]*><b>(?<subject>[^<]+)</b>',
      caseSensitive: false,
      multiLine: true,
    );

    final matches = pattern.allMatches(html);

    for (var m in matches) {
      String? semesterGroup = m.namedGroup('semester');
      String? subjectGroup = m.namedGroup('subject');

      // SEMESTER
      if (semesterGroup != null && semesterGroup.isNotEmpty) {
        grades.add(GradeItem(
          grade: "== ${semesterGroup.trim()} ==",
          subject: "",
          color: "White",
        ));
        continue;
      }

      // SUBJECT
      if (subjectGroup != null) {
        String subject = unescape.convert(subjectGroup).trim();

        int start = m.start;
        String remainingHtml = html.substring(m.end);

        final nextMatch = RegExp(
          r'<span[^>]*><b>|<td colspan=4><center><b>',
          caseSensitive: false,
        ).firstMatch(remainingHtml);

        int end = nextMatch != null ? m.end + nextMatch.start : html.length;

        String block = html.substring(start, end);

        final gradeMatch = RegExp(
          r'oценк[аи]:<br>\s*([^<]+?\(\d\))',
          caseSensitive: false,
        ).firstMatch(block);

        String grade = gradeMatch != null
            ? unescape.convert(gradeMatch.group(1)!).trim()
            : "Няма оценка";

        // COLOR RULES
        String color = "White";
        if (grade.contains("Няма оценка")) {
          color = "Blue";
        } else if (grade.contains("Зачита се") || grade.contains("(6)")) {
          color = "Green";
        } else if (grade.contains("(5)")) {
          color = "Cyan";
        } else if (grade.contains("(4)")) {
          color = "Yellow";
        } else if (grade.contains("(3)")) {
          color = "Orange";
        } else if (grade.contains("(2)")) {
          color = "Red";
        }

        grades.add(GradeItem(
          subject: subject,
          grade: grade,
          color: color,
        ));
      }
    }

    List<List<GradeItem>> groups = [];
    List<GradeItem>? currentGroup;

    for (var item in grades) {
      if (item.isSemester) {
        currentGroup = [item];
        groups.add(currentGroup);
      } else {
        currentGroup?.add(item);
      }
    }

    var reversedGroups = groups.reversed.toList();
    List<GradeItem> finalList = [];
    for (var g in reversedGroups) {
      finalList.addAll(g);
    }

    return finalList;
  }
}
