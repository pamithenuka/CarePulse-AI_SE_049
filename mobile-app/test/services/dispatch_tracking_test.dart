import 'dart:async';
import 'package:flutter_test/flutter_test.dart';
import 'package:geolocator/geolocator.dart';
import 'package:mobile_app/features/dispatch/services/api_service.dart';
import 'package:mobile_app/features/dispatch/services/location_service.dart';

class PendingLocationApi extends ApiService {
  final requests = <Completer<void>>[];
  @override
  Future<void> updateLocation(String id, double lat, double lng, double speed, double heading) {
    final request = Completer<void>();
    requests.add(request);
    return request.future;
  }
}

Position position() => Position(latitude: 6.92, longitude: 79.86,
    timestamp: DateTime.utc(2026, 10, 8), accuracy: 1, altitude: 0,
    altitudeAccuracy: 1, heading: 0, headingAccuracy: 1, speed: 0, speedAccuracy: 1);

Future<void> flush() => Future<void>.delayed(Duration.zero);

void main() {
  test('current session failure stops tracking and reports the error once', () async {
    final stream = StreamController<Position>.broadcast();
    final api = PendingLocationApi();
    final service = LocationService(apiService: api, positionStream: () => stream.stream);
    final errors = <Object>[];
    addTearDown(() async { service.stopTracking(); await stream.close(); });
    service.startTracking('case', onError: errors.add);
    stream.add(position());
    await flush();
    api.requests.single.completeError(Exception('current request failed'));
    await flush();
    stream.add(position());
    await flush();
    expect(errors, hasLength(1));
    expect(api.requests, hasLength(1));
  });

  test('late failure from paused tracking must not cancel restarted tracking', () async {
    final stream = StreamController<Position>.broadcast();
    final api = PendingLocationApi();
    final service = LocationService(apiService: api, positionStream: () => stream.stream);
    final errors = <Object>[];
    final updates = <Position>[];
    addTearDown(() async { service.stopTracking(); await stream.close(); });
    service.startTracking('case', onError: errors.add);
    stream.add(position());
    await flush();
    expect(api.requests, hasLength(1));
    service.stopTracking();
    service.startTracking('case', onError: errors.add, onUpdate: updates.add);
    api.requests.first.completeError(Exception('old request failed'));
    await flush();
    stream.add(position());
    await flush();
    expect(errors, isEmpty, reason: 'An obsolete request must not affect the new session');
    expect(api.requests, hasLength(2), reason: 'New tracking must still send locations');
    api.requests.last.complete();
    await flush();
    expect(updates, hasLength(1));
  });

  test('late success after pause must not publish a location callback', () async {
    final stream = StreamController<Position>.broadcast();
    final api = PendingLocationApi();
    final service = LocationService(apiService: api, positionStream: () => stream.stream);
    final updates = <Position>[];
    addTearDown(() async { service.stopTracking(); await stream.close(); });
    service.startTracking('case', onUpdate: updates.add);
    stream.add(position());
    await flush();
    service.stopTracking();
    api.requests.single.complete();
    await flush();
    expect(updates, isEmpty);
  });
}
