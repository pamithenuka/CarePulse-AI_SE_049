import '../../../services/api_client.dart';
import '../../../services/session_store.dart';

class ApiService {
  final ApiClient _api;
  ApiService({ApiClient? api}) : _api = api ?? ApiClient();
  Future<Map<String, dynamic>> getActiveDispatches({int page = 1, int pageSize = 100}) async =>
    Map<String, dynamic>.from(await _api.get('dispatch/active', token: await SessionStore.token(),
      query: {'page': page, 'pageSize': pageSize}));
  Future<void> updateLocation(String id, double lat, double lng, double speed, double heading) async {
    await _api.put('dispatch/$id/location', token: await SessionStore.token(), body: {
      'latitude': lat, 'longitude': lng, 'speedKmh': speed < 0 ? 0 : speed, 'heading': heading < 0 ? 0 : heading,
    });
  }
  Future<void> arrive(String id) async => _api.put('dispatch/$id/arrive', token: await SessionStore.token());
  Future<void> escalate(String id, String notes) async => _api.post('dispatch/$id/escalate', token: await SessionStore.token(), body: {'notes': notes});
  Future<Map<String, dynamic>> completeOnsite(String id, Map<String, dynamic> vitals) async =>
    Map<String, dynamic>.from(await _api.post('dispatch/$id/complete-onsite', token: await SessionStore.token(), body: vitals));
}
