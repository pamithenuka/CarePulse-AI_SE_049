import 'dart:convert';
import 'dart:io' show Platform;
import 'package:flutter/foundation.dart' show kIsWeb;
import 'package:http/http.dart' as http;
import '../models/doctor.dart';
import '../models/slot.dart';

class ApiException implements Exception {
  final String message;
  ApiException(this.message);
  @override
  String toString() => message;
}

class ApiClient {
  // 'localhost' works in Chrome (web) since the browser and API are on
  // the same machine. An Android EMULATOR needs 10.0.2.2 instead, which
  // the emulator maps back to your computer. Port 5014 matches the
  // shared backend-api project's configured launch port.
  static String get _baseUrl {
    if (kIsWeb) return 'http://localhost:5014/api/v1';
    if (Platform.isAndroid) return 'http://10.0.2.2:5014/api/v1';
    return 'http://localhost:5014/api/v1';
  }

  Future<List<Doctor>> getDoctors() async {
    final res = await http.get(Uri.parse('$_baseUrl/doctors'));
    _checkOk(res);
    final list = jsonDecode(res.body) as List;
    return list.map((e) => Doctor.fromJson(e as Map<String, dynamic>)).toList();
  }

  Future<List<AppointmentSlot>> getSlots({
    required String doctorId,
    DateTime? date,
  }) async {
    final params = <String, String>{'doctorId': doctorId};
    if (date != null) {
      params['date'] =
          '${date.year.toString().padLeft(4, '0')}-${date.month.toString().padLeft(2, '0')}-${date.day.toString().padLeft(2, '0')}';
    }
    final uri = Uri.parse('$_baseUrl/doctors/slots').replace(queryParameters: params);
    final res = await http.get(uri);
    _checkOk(res);
    final list = jsonDecode(res.body) as List;
    return list.map((e) => AppointmentSlot.fromJson(e as Map<String, dynamic>)).toList();
  }

  Future<void> bookAppointment({required String slotId, required String patientId}) async {
    final res = await http.post(
      Uri.parse('$_baseUrl/appointments/book'),
      headers: {'Content-Type': 'application/json'},
      body: jsonEncode({'slotId': slotId, 'patientId': patientId}),
    );
    if (res.statusCode == 409) {
      throw ApiException('This slot was just booked by someone else. Please pick another time.');
    }
    _checkOk(res);
  }

  void _checkOk(http.Response res) {
    if (res.statusCode < 200 || res.statusCode >= 300) {
      throw ApiException('Request failed (${res.statusCode}): ${res.body}');
    }
  }
}
