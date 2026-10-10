import 'dart:convert';
import 'dart:io';
import 'package:flutter_test/flutter_test.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import '../lib/services/triage_api_service.dart';
import '../lib/features/dispatch/services/api_service.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();
  HttpOverrides.global = null; // Real loopback HTTP, not flutter_test's fake 400 client.
  const phase = String.fromEnvironment('QM_PHASE');
  test('QM client workflow: $phase', () async {
    final path = Platform.environment['CAREPULSE_CLIENT_SESSION']!;
    final session = jsonDecode(File(path).readAsStringSync()) as Map<String, dynamic>;
    void signIn(String role) => FlutterSecureStorage.setMockInitialValues({
      'carepulse_session': jsonEncode({'token': session['${role}Token'],
        'expiresAt': DateTime.now().add(const Duration(hours: 1)).toIso8601String()})});
    final triage = TriageApiService();
    if (phase == 'patient') {
      signIn('patient');
      final result = await triage.submitTriage(patientProfileId: session['patientId'],
        symptoms: 'Synthetic severe chest pain for client integration', duration: '1 hour',
        severity: 'Severe', additionalSymptoms: [], hasPhotoAttachment: false,
        latitude: 6.9271, longitude: 79.8612);
      session['triageId'] = result['id'];
      expect(session['triageId'], isNotNull);
      final status = await triage.getStatus(session['triageId']);
      expect(status['patientProfileId'], session['patientId']);
      expect(status['requiresDoctorApproval'], isTrue);
      File(path).writeAsStringSync(jsonEncode(session));
    } else if (phase == 'nurse') {
      signIn('nurse');
      final nurse = ApiService();
      final active = await nurse.getActiveDispatches();
      expect((active['tickets'] as List).any((t) => t['id'] == session['dispatchId']), isTrue);
      await nurse.updateLocation(session['dispatchId'], 6.9271, 79.8612, 0, 0);
      await nurse.arrive(session['dispatchId']);
      await nurse.completeOnsite(session['dispatchId'], {'heartRate': 80, 'bloodPressure': '120/80',
        'bodyTempC': 37, 'oxygenSaturation': 98, 'clinicalNotes': 'Synthetic client integration vitals'});
      final after = await nurse.getActiveDispatches();
      expect((after['tickets'] as List).any((t) => t['id'] == session['dispatchId']), isFalse);
      signIn('patient');
      expect((await triage.getStatus(session['triageId']))['status'], 'VISIT_COMPLETED');
    } else {
      fail('QM_PHASE must be patient or nurse');
    }
  });
}
