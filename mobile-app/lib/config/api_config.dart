import 'package:flutter/foundation.dart';

class ApiConfig {
  ApiConfig._();
  static const _configured = String.fromEnvironment('API_BASE_URL');
  static String get baseUrl {
    if (_configured.isNotEmpty) return _configured.replaceFirst(RegExp(r'/$'), '');
    final host = !kIsWeb && defaultTargetPlatform == TargetPlatform.android ? '10.0.2.2' : 'localhost';
    return 'http://$host:5014/api/v1';
  }
}
