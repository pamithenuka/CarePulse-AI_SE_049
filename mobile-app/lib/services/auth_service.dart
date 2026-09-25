import '../models/auth_user.dart';
import 'api_client.dart';

class AuthService {
  final ApiClient _client;

  AuthService(this._client);

  Future<AuthUser> login(String email, String password) async {
    final json = await _client.post('/auth/login', body: {'email': email, 'password': password});
    return AuthUser.fromJson(json as Map<String, dynamic>);
  }

  /// Registers a login account with the Patient role. Profile details are
  /// collected separately in the onboarding form (POST /patients/profile),
  /// matching backend-api's CreateProfileAsync contract.
  Future<AuthUser> register(String fullName, String email, String password) async {
    final json = await _client.post('/auth/register', body: {
      'fullName': fullName,
      'email': email,
      'password': password,
      'role': 'Patient',
    });
    return AuthUser.fromJson(json as Map<String, dynamic>);
  }
}
