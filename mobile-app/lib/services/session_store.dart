import 'dart:convert';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'api_exception.dart';

class SessionStore {
  static const storage = FlutterSecureStorage();
  static Future<String> token() async {
    final raw = await storage.read(key: 'carepulse_session');
    if (raw == null) throw ApiException(401, 'Please sign in.');
    final session = jsonDecode(raw) as Map<String, dynamic>;
    if (DateTime.parse(session['expiresAt'] as String).isBefore(DateTime.now())) {
      throw ApiException(401, 'Your session expired. Sign in again; your assigned case is saved.');
    }
    return session['token'] as String;
  }
}
