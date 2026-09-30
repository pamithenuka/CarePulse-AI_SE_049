import 'api_client.dart';
import 'session_store.dart';

class TriageApiService {
  final ApiClient _api = ApiClient();
  Future<Map<String, dynamic>> submitTriage({required String patientProfileId,
    required String symptoms, required String duration, required String severity,
    required List<String> additionalSymptoms, required bool hasPhotoAttachment,
    double? latitude, double? longitude}) async => Map<String, dynamic>.from(await _api.post(
      'triage/submit', token: await SessionStore.token(), body: {
        'patientProfileId': patientProfileId, 'symptoms': symptoms, 'duration': duration,
        'severity': severity, 'additionalSymptoms': additionalSymptoms, 'hasPhotoAttachment': false,
        'latitude': latitude, 'longitude': longitude,
      }));
  Future<List<dynamic>> getAuditLog(String id) async => List<dynamic>.from(
    await _api.get('triage/$id/audit-log', token: await SessionStore.token()));
  Future<Map<String, dynamic>> getStatus(String id) async => Map<String, dynamic>.from(
    await _api.get('triage/$id', token: await SessionStore.token()));
  Future<List<dynamic>> getMine() async => List<dynamic>.from(
    await _api.get('triage/mine', token: await SessionStore.token()));
}
