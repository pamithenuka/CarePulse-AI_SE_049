import 'dart:convert';

import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:mobile_app/services/api_client.dart';
import 'package:mobile_app/services/api_exception.dart';
import 'package:mobile_app/services/triage_api_service.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();
  setUp(
    () => FlutterSecureStorage.setMockInitialValues({
      'carepulse_session': jsonEncode({
        'token': 'test-jwt',
        'expiresAt': DateTime.now()
            .add(const Duration(hours: 1))
            .toIso8601String(),
      }),
    }),
  );

  for (final severity in ['Mild', 'Moderate', 'Severe']) {
    test(
      'submission preserves exact $severity and the complete backend payload',
      () async {
        final api = TriageApiService(
          api: ApiClient(
            client: MockClient((request) async {
              expect(request.method, 'POST');
              expect(request.url.path, '/api/v1/triage/submit');
              expect(request.headers['Authorization'], 'Bearer test-jwt');
              expect(jsonDecode(request.body), {
                'patientProfileId': 'patient-1',
                'symptoms': 'Headache',
                'duration': '4–7 days',
                'severity': severity,
                'additionalSymptoms': ['Dizziness', 'Fever'],
                'hasPhotoAttachment': false,
                'latitude': 6.92,
                'longitude': 79.86,
              });
              return http.Response(
                jsonEncode({
                  'id': 'case-1',
                  'recommendedSpecialty': 'GENERAL_MEDICINE',
                }),
                200,
              );
            }),
          ),
        );
        final result = await api.submitTriage(
          patientProfileId: 'patient-1',
          symptoms: 'Headache',
          duration: '4–7 days',
          severity: severity,
          additionalSymptoms: ['Dizziness', 'Fever'],
          hasPhotoAttachment: false,
          latitude: 6.92,
          longitude: 79.86,
        );
        expect(result['id'], 'case-1');
        expect(result['recommendedSpecialty'], 'GENERAL_MEDICINE');
      },
    );
  }

  test('503 is propagated exactly once without automatic retry', () async {
    var requests = 0;
    final api = TriageApiService(
      api: ApiClient(
        client: MockClient((request) async {
          requests++;
          return http.Response(
            jsonEncode({'message': 'Check history before retrying.'}),
            503,
          );
        }),
      ),
    );
    await expectLater(
      api.submitTriage(
        patientProfileId: 'patient-1',
        symptoms: 'Headache',
        duration: 'Not sure',
        severity: 'Mild',
        additionalSymptoms: [],
        hasPhotoAttachment: false,
      ),
      throwsA(
        isA<ApiException>().having((error) => error.statusCode, 'status', 503),
      ),
    );
    expect(requests, 1);
  });

  test(
    'status, audit and history preserve response fields and use the shared JWT',
    () async {
      final paths = <String>[];
      final api = TriageApiService(
        api: ApiClient(
          client: MockClient((request) async {
            paths.add(request.url.path);
            expect(request.headers['Authorization'], 'Bearer test-jwt');
            if (request.url.path.endsWith('/audit-log')) {
              return http.Response(
                jsonEncode([
                  {
                    'logMessage': 'Doctor notes',
                    'createdAt': '2026-01-01T12:00:00Z',
                  },
                ]),
                200,
              );
            }
            if (request.url.path.endsWith('/mine')) {
              return http.Response(
                jsonEncode([
                  {'id': 'case-1'},
                ]),
                200,
              );
            }
            return http.Response(
              jsonEncode({
                'id': 'case-1',
                'riskLevel': 'HIGH',
                'status': 'REJECTED',
                'assessmentFailed': true,
              }),
              200,
            );
          }),
        ),
      );
      final status = await api.getStatus('case-1');
      expect(status['status'], 'REJECTED');
      expect(status['assessmentFailed'], isTrue);
      expect(await api.getAuditLog('case-1'), hasLength(1));
      expect(await api.getMine(), hasLength(1));
      expect(paths, [
        '/api/v1/triage/case-1',
        '/api/v1/triage/case-1/audit-log',
        '/api/v1/triage/mine',
      ]);
    },
  );

  test('expired authentication is rejected before making a request', () async {
    FlutterSecureStorage.setMockInitialValues({});
    var requests = 0;
    final api = TriageApiService(
      api: ApiClient(
        client: MockClient((request) async {
          requests++;
          return http.Response('{}', 200);
        }),
      ),
    );
    await expectLater(
      api.getMine(),
      throwsA(
        isA<ApiException>().having((error) => error.statusCode, 'status', 401),
      ),
    );
    expect(requests, 0);
  });
}
