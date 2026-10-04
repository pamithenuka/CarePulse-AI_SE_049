import 'dart:convert';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:mobile_app/features/dispatch/services/api_service.dart';
import 'package:mobile_app/services/api_client.dart';
import 'package:mobile_app/services/api_exception.dart';
import 'package:mobile_app/services/session_store.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  void session({bool expired = false}) {
    FlutterSecureStorage.setMockInitialValues({
      'carepulse_session': jsonEncode({
        'token': 'synthetic-shared-token',
        'expiresAt': DateTime.now().add(Duration(hours: expired ? -1 : 1)).toIso8601String(),
      }),
    });
  }

  test('expired shared session rejects before sending telemetry', () async {
    session(expired: true);
    await expectLater(SessionStore.token(), throwsA(isA<ApiException>().having((e) => e.statusCode, 'status', 401)));
  });

  test('dispatch sends the shared JWT and reports a server-rejected location', () async {
    session();
    final api = ApiService(api: ApiClient(client: MockClient((request) async {
      expect(request.headers['Authorization'], 'Bearer synthetic-shared-token');
      expect(request.method, 'PUT');
      expect(request.url.path, endsWith('/dispatch/synthetic-id/location'));
      expect(jsonDecode(request.body)['latitude'], 6.92);
      return http.Response('', 401);
    })));
    await expectLater(api.updateLocation('synthetic-id', 6.92, 79.86, 0, 0),
      throwsA(isA<ApiException>().having((e) => e.statusCode, 'status', 401)));
  });

  test('completion conflict propagates instead of showing success', () async {
    session();
    final api = ApiService(api: ApiClient(client: MockClient((request) async =>
      http.Response(jsonEncode({'message': 'Record arrival first.'}), 409))));
    await expectLater(api.completeOnsite('synthetic-id', {}),
      throwsA(isA<ApiException>().having((e) => e.statusCode, 'status', 409)));
  });
}
