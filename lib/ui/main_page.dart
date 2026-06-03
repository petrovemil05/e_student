import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:shared_preferences/shared_preferences.dart';
import '../models/grade_item.dart';
import '../services/grades_parser.dart';
import '../services/tu_api_service.dart';
import '../services/notification_service.dart';
import '../viewmodels/grade_monitor_viewmodel.dart';

class MainPage extends StatefulWidget {
  const MainPage({super.key});

  @override
  State<MainPage> createState() => _MainPageState();
}

class _MainPageState extends State<MainPage> {
  final GradesParser _parser = GradesParser();
  final TuApiService _api = TuApiService();
  final TextEditingController _fnumController = TextEditingController();
  final TextEditingController _egnController = TextEditingController();

  List<GradeItem>? _grades;
  bool _isLoading = false;
  bool _isLoggedIn = false;

  @override
  void initState() {
    super.initState();
    _checkLogin();
    NotificationService.requestPermissions();
  }

  Future<void> _checkLogin() async {
    final prefs = await SharedPreferences.getInstance();
    String fnum = prefs.getString("fnum") ?? "";
    String egn = prefs.getString("egn") ?? "";

    if (fnum.isNotEmpty && egn.isNotEmpty) {
      setState(() {
        _isLoggedIn = true;
        _fnumController.text = fnum;
        _egnController.text = egn;
      });
      _loadGrades();
    }
  }

  Future<void> _onLoginClicked() async {
    String fnum = _fnumController.text;
    String egn = _egnController.text;

    if (fnum.isEmpty || egn.isEmpty) return;

    final prefs = await SharedPreferences.getInstance();
    await prefs.setString("fnum", fnum);
    await prefs.setString("egn", egn);

    setState(() {
      _isLoggedIn = true;
    });

    _loadGrades();
  }

  void _onLogoutClicked() async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.remove("fnum");
    await prefs.remove("egn");

    setState(() {
      _isLoggedIn = false;
      _grades = null;
      _fnumController.clear();
      _egnController.clear();
    });
  }

  Future<void> _loadGrades() async {
    setState(() {
      _isLoading = true;
    });

    try {
      String html = await _api.getHtmlAsync(
        _fnumController.text,
        _egnController.text,
      );
      var result = _parser.parse(html);
      setState(() {
        _grades = result;
      });
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text("Error: $e")),
        );
      }
    } finally {
      if (mounted) {
        setState(() {
          _isLoading = false;
        });
      }
    }
  }

  Color _getGradeColor(String colorName) {
    switch (colorName) {
      case "Green":
        return const Color(0xFF2ECC71);
      case "Red":
        return const Color(0xFFE74C3C);
      case "Blue":
        return const Color(0xFF3498DB);
      case "Yellow":
        return const Color(0xFFF1C40F);
      case "Cyan":
        return const Color(0xFF1ABC9C);
      case "Orange":
        return const Color(0xFFE67E22);
      default:
        return Colors.white;
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFF121212),
      appBar: AppBar(
        title: const Text("e-student TU-Sofia"),
        backgroundColor: Colors.transparent,
        elevation: 0,
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(15),
        child: Column(
          children: [
            const Text(
              "Оценки от Е-Студент",
              style: TextStyle(
                fontSize: 28,
                color: Colors.white,
                fontWeight: FontWeight.bold,
              ),
            ),
            const SizedBox(height: 15),
            if (!_isLoggedIn) _buildLoginCard(),
            if (_isLoggedIn) ...[
              _buildActions(),
              const SizedBox(height: 15),
              if (_isLoading)
                const CircularProgressIndicator(color: Colors.white),
              if (_grades != null) _buildGradesList(),
            ],
          ],
        ),
      ),
    );
  }

  Widget _buildLoginCard() {
    return Card(
      color: const Color(0xFF1E1E1E),
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(15)),
      child: Padding(
        padding: const EdgeInsets.all(15),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text(
              "Login",
              style: TextStyle(
                color: Colors.white,
                fontSize: 18,
                fontWeight: FontWeight.bold,
              ),
            ),
            const SizedBox(height: 10),
            TextField(
              controller: _fnumController,
              decoration: const InputDecoration(
                hintText: "Факултетен номер",
                hintStyle: TextStyle(color: Colors.grey),
                filled: true,
                fillColor: Colors.transparent,
              ),
              style: const TextStyle(color: Colors.white),
              keyboardType: TextInputType.number,
            ),
            const SizedBox(height: 10),
            TextField(
              controller: _egnController,
              decoration: const InputDecoration(
                hintText: "ЕГН",
                hintStyle: TextStyle(color: Colors.grey),
                filled: true,
                fillColor: Colors.transparent,
              ),
              style: const TextStyle(color: Colors.white),
              keyboardType: TextInputType.number,
              obscureText: true,
            ),
            const SizedBox(height: 15),
            SizedBox(
              width: double.infinity,
              child: ElevatedButton(
                onPressed: _onLoginClicked,
                style: ElevatedButton.styleFrom(
                  backgroundColor: const Color(0xFF2ECC71),
                  shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(10),
                  ),
                ),
                child: const Text("Login", style: TextStyle(color: Colors.white)),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildActions() {
    return Consumer<GradeMonitorViewModel>(
      builder: (context, vm, child) {
        return Column(
          children: [
            Row(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                Expanded(
                  child: ElevatedButton(
                    onPressed: _loadGrades,
                    style: ElevatedButton.styleFrom(
                      backgroundColor: const Color(0xFF3498DB),
                      shape: RoundedRectangleBorder(
                        borderRadius: BorderRadius.circular(10),
                      ),
                    ),
                    child: const Text("Refresh",
                        style: TextStyle(color: Colors.white)),
                  ),
                ),
                const SizedBox(width: 10),
                Expanded(
                  child: ElevatedButton(
                    onPressed: _onLogoutClicked,
                    style: ElevatedButton.styleFrom(
                      backgroundColor: const Color(0xFFE74C3C),
                      shape: RoundedRectangleBorder(
                        borderRadius: BorderRadius.circular(10),
                      ),
                    ),
                    child: const Text("Logout",
                        style: TextStyle(color: Colors.white)),
                  ),
                ),
              ],
            ),
            const SizedBox(height: 10),
            SizedBox(
              width: double.infinity,
              child: ElevatedButton(
                onPressed: () => vm.toggle(),
                child: Text(vm.toggleLabel),
              ),
            ),
          ],
        );
      },
    );
  }

  Widget _buildGradesList() {
    return ListView.builder(
      shrinkWrap: true,
      physics: const NeverScrollableScrollPhysics(),
      itemCount: _grades!.length,
      itemBuilder: (context, index) {
        final item = _grades![index];
        return Card(
          color: const Color(0xFF1E1E1E),
          margin: const EdgeInsets.symmetric(vertical: 5),
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
          child: Padding(
            padding: const EdgeInsets.all(12),
            child: Row(
              children: [
                if (item.isSemester)
                  Expanded(
                    child: Center(
                      child: Container(
                        padding: const EdgeInsets.symmetric(
                            horizontal: 20, vertical: 4),
                        decoration: BoxDecoration(
                          color: const Color(0xFF333333),
                          borderRadius: BorderRadius.circular(8),
                        ),
                        child: Text(
                          item.grade,
                          style: const TextStyle(
                            color: Colors.white,
                            fontSize: 16,
                          ),
                        ),
                      ),
                    ),
                  )
                else ...[
                  Expanded(
                    child: Text(
                      item.subject,
                      style: const TextStyle(color: Colors.white, fontSize: 16),
                    ),
                  ),
                  Container(
                    padding:
                        const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                    decoration: BoxDecoration(
                      color: const Color(0xFF333333),
                      borderRadius: BorderRadius.circular(8),
                    ),
                    child: Text(
                      item.grade,
                      style: TextStyle(
                        color: _getGradeColor(item.color),
                        fontSize: 14,
                        fontWeight: FontWeight.bold,
                      ),
                    ),
                  ),
                ],
              ],
            ),
          ),
        );
      },
    );
  }
}
