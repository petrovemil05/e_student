class GradeItem {
  final String subject;

  /// The display grade string, e.g.:
  ///   "Добър (4)"            — normal редовна grade
  ///   "Добър (4) (П)"        — final grade that came from a поправка/ликвидационна
  ///   "Няма оценка"
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