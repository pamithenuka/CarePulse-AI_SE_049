import 'dart:io' show Platform;

import 'package:flutter/foundation.dart' show kIsWeb;

/// backend-api's `http` launch profile listens on port 5014 (see
/// backend-api/Properties/launchSettings.json). The Android emulator can't
/// reach the host machine via `localhost` - it must use the special alias
/// 10.0.2.2. A physical device needs the host machine's real LAN IP instead;
/// change [_lanHost] if you're testing on one.
class ApiConfig {
  ApiConfig._();

  static const String _lanHost = '192.168.1.100';
  static const int _port = 5014;

  static String get baseUrl {
    if (kIsWeb) return 'http://localhost:$_port/api/v1';
    if (Platform.isAndroid) return 'http://10.0.2.2:$_port/api/v1';
    return 'http://localhost:$_port/api/v1';
  }

  /// Swap [baseUrl] for this when running against a physical device on the
  /// same Wi-Fi network as the machine running `dotnet run`.
  static String get lanBaseUrl => 'http://$_lanHost:$_port/api/v1';
}
