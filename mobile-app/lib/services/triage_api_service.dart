import 'dart:convert';
import 'package:http/http.dart' as http;

class TriageApiService {
  // Using 10.0.2.2 for Android emulator to reach localhost. 
  // Change to localhost if running on Web/Desktop, or actual IP if on physical device.
  static const String baseUrl = 'http://10.0.2.2:5014/api/v1/triage';

  Future<Map<String, dynamic>> submitTriage({
    required String patientId,
    required String symptoms,
    required String duration,
    required String severity,
    required List<String> additionalSymptoms,
    required bool hasPhotoAttachment,
  }) async {
    final response = await http.post(
      Uri.parse('$baseUrl/submit'),
      headers: {'Content-Type': 'application/json'},
      body: jsonEncode({
        'patientId': patientId,
        'symptoms': symptoms,
        'duration': duration,
        'severity': severity,
        'additionalSymptoms': additionalSymptoms,
        'hasPhotoAttachment': hasPhotoAttachment,
      }),
    );

    if (response.statusCode == 200 || response.statusCode == 201) {
      return jsonDecode(response.body);
    } else {
      throw Exception('Failed to submit triage: ${response.statusCode} - ${response.body}');
    }
  }

  // Mocks polling the triage status for the status screen
  Future<List<dynamic>> getAuditLog(String triageId) async {
    final response = await http.get(Uri.parse('$baseUrl/$triageId/audit-log'));

    if (response.statusCode == 200) {
      return jsonDecode(response.body);
    } else {
      throw Exception('Failed to get audit log');
    }
  }
}
