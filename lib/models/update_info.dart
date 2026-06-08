class UpdateInfo {
  final String version;
  final int build;
  final String description;
  final String apkUrl;

  UpdateInfo({
    required this.version,
    required this.build,
    required this.description,
    required this.apkUrl,
  });

  factory UpdateInfo.fromJson(Map<String, dynamic> json) {
    return UpdateInfo(
      version: json['version'] ?? '',
      build: json['build'] ?? 0,
      description: json['description'] ?? '',
      apkUrl: json['apk_url'] ?? '',
    );
  }
}
