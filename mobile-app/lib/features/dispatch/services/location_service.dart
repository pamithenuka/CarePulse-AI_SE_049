import 'dart:async';
import 'package:flutter/foundation.dart';
import 'package:geolocator/geolocator.dart';
import 'api_service.dart';

class LocationService {
  final ApiService _apiService = ApiService();
  StreamSubscription<Position>? _positionSubscription;

  /// Check and request location permissions
  Future<bool> requestPermission() async {
    bool serviceEnabled = await Geolocator.isLocationServiceEnabled();
    if (!serviceEnabled) {
      return false;
    }

    LocationPermission permission = await Geolocator.checkPermission();
    if (permission == LocationPermission.denied) {
      permission = await Geolocator.requestPermission();
      if (permission == LocationPermission.denied) {
        return false;
      }
    }

    if (permission == LocationPermission.deniedForever) {
      return false;
    }

    return true;
  }

  /// Get the current position once
  Future<Position> getCurrentPosition() async {
    return await Geolocator.getCurrentPosition(
      locationSettings: const LocationSettings(
        accuracy: LocationAccuracy.high,
      ),
    );
  }

  /// Start continuous GPS telemetry tracking and send updates to backend
  void startTracking(String dispatchId, {Function(Position)? onUpdate}) {
    const locationSettings = LocationSettings(
      accuracy: LocationAccuracy.high,
      distanceFilter: 10, // meters
    );

    _positionSubscription = Geolocator.getPositionStream(locationSettings: locationSettings).listen(
      (Position position) {
        debugPrint('GPS Update: ${position.latitude}, ${position.longitude}');

        // Send telemetry to backend
        _apiService.updateLocation(
          dispatchId,
          position.latitude,
          position.longitude,
          position.speed * 3.6, // m/s to km/h
          position.heading,
        );

        onUpdate?.call(position);
      },
    );
  }

  /// Stop GPS tracking
  void stopTracking() {
    _positionSubscription?.cancel();
    _positionSubscription = null;
  }
}
