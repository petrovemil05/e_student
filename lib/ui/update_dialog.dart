import 'package:flutter/material.dart';
import 'package:package_info_plus/package_info_plus.dart';
import '../models/update_info.dart';
import '../services/update_service.dart';

class UpdateDialog extends StatefulWidget {
  final UpdateInfo updateInfo;

  const UpdateDialog({super.key, required this.updateInfo});

  @override
  State<UpdateDialog> createState() => _UpdateDialogState();

  static void show(BuildContext context, UpdateInfo info) {
    showDialog(
      context: context,
      barrierDismissible: false,
      builder: (context) => UpdateDialog(updateInfo: info),
    );
  }
}

class _UpdateDialogState extends State<UpdateDialog> {
  double _progress = 0;
  bool _isDownloading = false;
  String _currentVersion = '';

  @override
  void initState() {
    super.initState();
    _loadCurrentVersion();
  }

  Future<void> _loadCurrentVersion() async {
    final info = await PackageInfo.fromPlatform();
    setState(() {
      _currentVersion = info.version;
    });
  }

  void _startUpdate() async {
    setState(() {
      _isDownloading = true;
    });

    await UpdateService.downloadAndInstall(
      url: widget.updateInfo.apkUrl,
      onProgress: (p) {
        setState(() {
          _progress = p;
        });
      },
      onError: (error) {
        setState(() {
          _isDownloading = false;
          _progress = 0;
        });
        if (mounted) {
          ScaffoldMessenger.of(context).showSnackBar(
            SnackBar(content: Text('Update failed: $error')),
          );
        }
      },
    );
  }

  @override
  Widget build(BuildContext context) {
    return AlertDialog(
      title: const Text('New Update Available'),
      content: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text('Current Version: $_currentVersion'),
          Text('Latest Version: ${widget.updateInfo.version}'),
          const SizedBox(height: 10),
          if (widget.updateInfo.description.isNotEmpty) ...[
            const Text(
              'What\'s New:',
              style: TextStyle(fontWeight: FontWeight.bold),
            ),
            Text(widget.updateInfo.description),
            const SizedBox(height: 10),
          ],
          if (_isDownloading) ...[
            const SizedBox(height: 10),
            LinearProgressIndicator(value: _progress),
            const SizedBox(height: 5),
            Text('${(_progress * 100).toStringAsFixed(0)}%'),
          ],
        ],
      ),
      actions: _isDownloading
          ? []
          : [
              TextButton(
                onPressed: () => Navigator.pop(context),
                child: const Text('Later'),
              ),
              ElevatedButton(
                onPressed: _startUpdate,
                child: const Text('Update'),
              ),
            ],
    );
  }
}
