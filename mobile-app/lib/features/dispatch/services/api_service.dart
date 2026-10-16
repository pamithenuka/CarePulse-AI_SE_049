import '../../../services/api_client.dart';
import '../../../services/session_store.dart';

class ApiService {
  final ApiClient _api;
  ApiService({ApiClient? api}) : _api = api ?? ApiClient();
  Future<Map<String, dynamic>> getMyAvailability() async =>
      Map<String, dynamic>.from(
        await _api.get(
          'dispatch/nurses/me/availability',
          token: await SessionStore.token(),
        ),
      );

  Future<void> setMyAvailability(bool available) async => _api.put(
    'dispatch/nurses/me/availability',
    token: await SessionStore.token(),
    body: {'isAvailable': available},
  );

  Future<void> updateAvailableLocation(double lat, double lng) async =>
      _api.put(
        'dispatch/nurses/me/location',
        token: await SessionStore.token(),
        body: {'latitude': lat, 'longitude': lng, 'speedKmh': 0, 'heading': 0},
      );

  Future<Map<String, dynamic>> getActiveDispatches({
    int page = 1,
    int pageSize = 100,
  }) async => Map<String, dynamic>.from(
    await _api.get(
      'dispatch/active',
      token: await SessionStore.token(),
      query: {'page': page, 'pageSize': pageSize},
    ),
  );
  Future<Map<String, dynamic>> getPatientSummary(String id) async =>
      Map<String, dynamic>.from(
        await _api.get(
          'dispatch/$id/patient-summary',
          token: await SessionStore.token(),
        ),
      );
  Future<void> updateLocation(
    String id,
    double lat,
    double lng,
    double speed,
    double heading,
  ) async {
    await _api.put(
      'dispatch/$id/location',
      token: await SessionStore.token(),
      body: {
        'latitude': lat,
        'longitude': lng,
        'speedKmh': speed < 0 ? 0 : speed,
        'heading': heading < 0 ? 0 : heading,
      },
    );
  }

  Future<void> arrive(String id) async =>
      _api.put('dispatch/$id/arrive', token: await SessionStore.token());
  Future<List<dynamic>> getVitals(String id) async => List<dynamic>.from(
    await _api.get('dispatch/$id/vitals', token: await SessionStore.token()),
  );
  Future<void> saveVitals(String id, Map<String, dynamic> vitals) async =>
      _api.put(
        'dispatch/$id/vitals',
        token: await SessionStore.token(),
        body: vitals,
      );
  Future<void> completeVisit(String id) async =>
      _api.post('dispatch/$id/complete', token: await SessionStore.token());
  Future<Map<String, dynamic>> completeOnsite(
    String id,
    Map<String, dynamic> vitals,
  ) async => Map<String, dynamic>.from(
    await _api.post(
      'dispatch/$id/complete-onsite',
      token: await SessionStore.token(),
      body: vitals,
    ),
  );
}
