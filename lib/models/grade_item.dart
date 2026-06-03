class GradeItem {
  final String subject;
  final String grade;
  final String color;

  GradeItem({
    required this.subject,
    required this.grade,
    required this.color,
  });

  bool get isSemester => subject.trim().isEmpty;
  bool get notSemester => !isSemester;
}
