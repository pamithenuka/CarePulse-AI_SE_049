import 'dart:convert';
import 'package:http/http.dart' as http;
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter/foundation.dart';

class ApiService {
  // Use localhost for Chrome/Web, and 10.0.2.2 for Android Emulators
  static const String baseUrl = kIsWeb ? 'http://localhost:5014/api/v1' : 'http://10.0.2.2:5014/api/v1';
  final FlutterSecureStorage _storage = const FlutterSecureStorage();

  Future<String?> getToken() async {
    return await _storage.read(key: 'jwt_token');
  }

  Future<void> saveToken(String token) async {
    await _storage.write(key: 'jwt_token', value: token);
  }

  Future<void> clearToken() async {
    await _storage.delete(key: 'jwt_token');
  }

  Future<Map<String, String>> _authHeaders() async {
    final token = await getToken();
    return {
      'Content-Type': 'application/json',
      if (token != null) 'Authorization': 'Bearer $token',
    };
  }

  /// POST /api/v1/auth/login (Nurse Login)
  Future<Map<String, dynamic>> login(String email, String password) async {
    final response = await http.post(
      Uri.parse('$baseUrl/auth/login'),
      headers: {'Content-Type': 'application/json'},
      body: jsonEncode({'email': email, 'password': password}),
    );
    if (response.statusCode == 200) {
      final data = jsonDecode(response.body);
      await saveToken(data['token']);
      return data;
    }
    throw Exception('Login failed: ${response.body}');
  }

  /// GET /api/v1/dispatch/active
  Future<Map<String, dynamic>> getActiveDispatches({int page = 1, int pageSize = 10}) async {
    final headers = await _authHeaders();
    final response = await http.get(
      Uri.parse('$baseUrl/dispatch/active?page=$page&pageSize=$pageSize'),
      headers: headers,
    );
    if (response.statusCode == 200) {
      return jsonDecode(response.body);
    }
    throw Exception('Failed to fetch dispatches: ${response.body}');
  }

  /// PUT /api/v1/dispatch/{id}/location
  Future<void> updateLocation(String dispatchId, double lat, double lng, double speed, double heading) async {
    final headers = await _authHeaders();
    await http.put(
      Uri.parse('$baseUrl/dispatch/$dispatchId/location'),
      headers: headers,
      body: jsonEncode({
        'latitude': lat,
        'longitude': lng,
        'speedKmh': speed,
        'heading': heading,
      }),
    );
  }

  /// POST /api/v1/dispatch/{id}/complete-onsite
  Future<Map<String, dynamic>> completeOnsite(String dispatchId, Map<String, dynamic> vitals) async {
    final headers = await _authHeaders();
    final response = await http.post(
      Uri.parse('$baseUrl/dispatch/$dispatchId/complete-onsite'),
      headers: headers,
      body: jsonEncode(vitals),
    );
    if (response.statusCode == 200) {
      return jsonDecode(response.body);
    }
    throw Exception('Failed to complete onsite: ${response.body}');
  }
}
