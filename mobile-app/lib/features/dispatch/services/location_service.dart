import 'dart:async';
import 'package:geolocator/geolocator.dart';
import 'api_service.dart';

class LocationService {
  final ApiService _apiService;
  final Stream<Position> Function() _positionStream;
  LocationService({ApiService? apiService, Stream<Position> Function()? positionStream})
      : _apiService = apiService ?? ApiService(),
        _positionStream = positionStream ?? (() => Geolocator.getPositionStream(
          locationSettings: const LocationSettings(accuracy: LocationAccuracy.high, distanceFilter: 10)));
  StreamSubscription<Position>? _positionSubscription;
  int _generation = 0;
  Future<bool> requestPermission() async {
    if (!await Geolocator.isLocationServiceEnabled()) return false;
    var permission = await Geolocator.checkPermission();
    if (permission == LocationPermission.denied) permission = await Geolocator.requestPermission();
    return permission != LocationPermission.denied && permission != LocationPermission.deniedForever;
  }
  Future<Position> getCurrentPosition() => Geolocator.getCurrentPosition(
    locationSettings: const LocationSettings(accuracy: LocationAccuracy.high, timeLimit: Duration(seconds: 20)));
  void startTracking(String id, {void Function(Position)? onUpdate, void Function(Object)? onError}) {
    stopTracking();
    final generation = _generation;
    var sending = false;
    _positionSubscription = _positionStream().listen((position) async {
        if (generation != _generation || sending) return;
        sending = true;
        try {
          await _apiService.updateLocation(id, position.latitude, position.longitude, position.speed * 3.6, position.heading);
          if (generation == _generation) onUpdate?.call(position);
        } catch (e) {
          if (generation == _generation) { stopTracking(); onError?.call(e); }
        }
        finally { sending = false; }
      }, onError: (Object e) {
        if (generation == _generation) { stopTracking(); onError?.call(e); }
      });
  }
  // HTTP requests already in flight cannot be recalled. Invalidate their callbacks
  // before cancelling the GPS stream so they cannot alter a newer session.
  void stopTracking() { _generation++; _positionSubscription?.cancel(); _positionSubscription = null; }
}
