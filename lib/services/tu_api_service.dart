import 'package:http/http.dart' as http;

class TuApiService {
  final http.Client client = http.Client();
  String? _cookie;

  final Map<String, String> _headers = {
    "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/147.0.0.0 Safari/537.36 OPR/131.0.0.0",
    "Accept": "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8",
    "Accept-Language": "en-US,en;q=0.9",
    "Upgrade-Insecure-Requests": "1",
    "Connection": "keep-alive",
    "Sec-Fetch-Site": "same-origin",
    "Sec-Fetch-Mode": "navigate",
    "Sec-Fetch-User": "?1",
    "Sec-Fetch-Dest": "document",
  };

  Future<String> getHtmlAsync(String fnum, String egn) async {
    const url = "https://e-university.tu-sofia.bg/ETUS/studenti/index.php";

    // 1. LOGIN
    var loginResponse = await client.post(
      Uri.parse(url),
      headers: _headers,
      body: {
        "fnum": fnum,
        "egn": egn,
      },
    );

    if (loginResponse.statusCode != 200) {
      throw Exception("Login failed with status: ${loginResponse.statusCode}");
    }

    // Extract cookie
    String? rawCookie = loginResponse.headers['set-cookie'];
    if (rawCookie != null) {
      _cookie = rawCookie.split(';').first;
    }

    // 2. FETCH DATA
    Map<String, String> dataHeaders = Map.from(_headers);
    if (_cookie != null) {
      dataHeaders['Cookie'] = _cookie!;
    }

    var dataResponse = await client.post(
      Uri.parse(url),
      headers: dataHeaders,
      body: {
        "deistvie": "1",
      },
    );

    if (dataResponse.statusCode != 200) {
      throw Exception("Fetch data failed with status: ${dataResponse.statusCode}");
    }

    return dataResponse.body;
  }
}
