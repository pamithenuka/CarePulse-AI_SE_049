import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:mobile_app/services/api_client.dart';
import 'package:mobile_app/services/api_exception.dart';
import 'package:mobile_app/services/auth_service.dart';

void main() {
  group('AuthService.login', () {
    test('returns an AuthUser when the server responds 200', () async {
      final mockClient = MockClient((request) async {
        expect(request.url.path, contains('/auth/login'));
        final body = jsonDecode(request.body) as Map<String, dynamic>;
        expect(body['email'], 'patient@example.com');

        return http.Response(
          jsonEncode({
            'userId': 'u1',
            'fullName': 'Jane Patient',
            'email': 'patient@example.com',
            'roles': ['Patient'],
            'token': 'fake-jwt',
            'expiresAt': DateTime.now().add(const Duration(hours: 1)).toIso8601String(),
          }),
          200,
          headers: {'content-type': 'application/json'},
        );
      });

      final service = AuthService(ApiClient(client: mockClient));
      final user = await service.login('patient@example.com', 'password123');

      expect(user.fullName, 'Jane Patient');
      expect(user.roles, contains('Patient'));
      expect(user.token, 'fake-jwt');
    });

    test('throws ApiException with the server message on invalid credentials', () async {
      final mockClient = MockClient((request) async {
        return http.Response(jsonEncode({'message': 'Invalid email or password.'}), 401);
      });

      final service = AuthService(ApiClient(client: mockClient));

      await expectLater(
        () => service.login('patient@example.com', 'wrong-password'),
        throwsA(isA<ApiException>().having((e) => e.isUnauthorized, 'isUnauthorized', isTrue)),
      );
    });
  });

  group('AuthService.register', () {
    test('sends the Patient role and returns the created user', () async {
      final mockClient = MockClient((request) async {
        expect(request.url.path, contains('/auth/register'));
        final body = jsonDecode(request.body) as Map<String, dynamic>;
        expect(body['role'], 'Patient');

        return http.Response(
          jsonEncode({
            'userId': 'u2',
            'fullName': body['fullName'],
            'email': body['email'],
            'roles': ['Patient'],
            'token': 'fake-jwt-2',
            'expiresAt': DateTime.now().add(const Duration(hours: 1)).toIso8601String(),
          }),
          200,
          headers: {'content-type': 'application/json'},
        );
      });

      final service = AuthService(ApiClient(client: mockClient));
      final user = await service.register('New Patient', 'new@example.com', 'password123');

      expect(user.fullName, 'New Patient');
      expect(user.roles, ['Patient']);
    });
  });
}
