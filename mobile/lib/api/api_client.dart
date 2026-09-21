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
  // 'localhost' works when running in Chrome (web) since the browser and
  // the API are on the same machine. An Android EMULATOR can't reach your
  // PC's localhost directly - it needs the special address 10.0.2.2,
  // which the emulator maps back to your computer. A real phone on the
  // same Wi-Fi would instead need your PC's actual local IP address.
  static String get _baseUrl {
    if (kIsWeb) return 'http://localhost:5000/api';
    if (Platform.isAndroid) return 'http://10.0.2.2:5000/api';
    return 'http://localhost:5000/api';
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
