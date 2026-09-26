import 'package:flutter/material.dart';
import 'package:flutter_map/flutter_map.dart';
import 'package:latlong2/latlong.dart';
import 'package:geolocator/geolocator.dart';
import '../services/location_service.dart';

class NavigationScreen extends StatefulWidget {
  final Map<String, dynamic> dispatch;

  const NavigationScreen({super.key, required this.dispatch});

  @override
  State<NavigationScreen> createState() => _NavigationScreenState();
}

class _NavigationScreenState extends State<NavigationScreen> {
  final LocationService _locationService = LocationService();
  final MapController _mapController = MapController();
  LatLng? _currentPosition;
  bool _isTracking = false;
  String _status = 'Loading...';

  // Simulated destination for demo
  final LatLng _destination = const LatLng(6.9271, 79.8612); // Colombo

  @override
  void initState() {
    super.initState();
    _status = widget.dispatch['status'] ?? 'Assigned';
    _initLocation();
  }

  Future<void> _initLocation() async {
    final hasPermission = await _locationService.requestPermission();
    if (!hasPermission) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Location permission denied'), backgroundColor: Colors.redAccent),
        );
      }
      // Use a default position for demo
      setState(() {
        _currentPosition = const LatLng(6.9100, 79.8500);
      });
      return;
    }

    try {
      final position = await _locationService.getCurrentPosition();
      setState(() {
        _currentPosition = LatLng(position.latitude, position.longitude);
      });
    } catch (e) {
      // Fallback position for demo
      setState(() {
        _currentPosition = const LatLng(6.9100, 79.8500);
      });
    }
  }

  void _toggleTracking() {
    setState(() => _isTracking = !_isTracking);

    if (_isTracking) {
      _locationService.startTracking(
        widget.dispatch['id'],
        onUpdate: (Position pos) {
          if (mounted) {
            setState(() {
              _currentPosition = LatLng(pos.latitude, pos.longitude);
            });
          }
        },
      );
      setState(() => _status = 'EnRoute');
    } else {
      _locationService.stopTracking();
    }
  }

  void _markArrived() {
    _locationService.stopTracking();
    setState(() {
      _isTracking = false;
      _status = 'ArrivedOnSite';
    });
  }

  @override
  void dispose() {
    _locationService.stopTracking();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFFF4F6F8),
      appBar: AppBar(
        backgroundColor: const Color(0xFF0B5F6B),
        elevation: 0,
        leading: IconButton(
          icon: const Icon(Icons.arrow_back_ios, color: Colors.white),
          onPressed: () => Navigator.pop(context),
        ),
        title: Text(
          'Dispatch #${widget.dispatch['id'].toString().length > 8 ? widget.dispatch['id'].toString().substring(0, 8) : widget.dispatch['id'].toString()}',
          style: const TextStyle(color: Colors.white, fontWeight: FontWeight.w600),
        ),
        actions: [
          Container(
            margin: const EdgeInsets.only(right: 12),
            padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
            decoration: BoxDecoration(
              color: _status == 'EnRoute'
                  ? const Color(0xFF0F766E).withOpacity(0.15)
                  : const Color(0xFF22C55E).withOpacity(0.15),
              borderRadius: BorderRadius.circular(20),
            ),
            child: Center(
              child: Text(
                _status,
                style: TextStyle(
                  color: _status == 'EnRoute' ? const Color(0xFF0F766E) : const Color(0xFF16A34A),
                  fontWeight: FontWeight.w600,
                  fontSize: 13,
                ),
              ),
            ),
          ),
        ],
      ),
      body: Column(
        children: [
          // Map Area
          Expanded(
            flex: 3,
            child: _currentPosition == null
                ? const Center(child: CircularProgressIndicator(color: Color(0xFF3B82F6)))
                : ClipRRect(
                    borderRadius: const BorderRadius.only(
                      bottomLeft: Radius.circular(24),
                      bottomRight: Radius.circular(24),
                    ),
                    child: FlutterMap(
                      mapController: _mapController,
                      options: MapOptions(
                        initialCenter: _currentPosition!,
                        initialZoom: 14.0,
                      ),
                      children: [
                        TileLayer(
                          urlTemplate: 'https://tile.openstreetmap.org/{z}/{x}/{y}.png',
                          userAgentPackageName: 'com.carepulse.mobile',
                        ),
                        MarkerLayer(
                          markers: [
                            // Nurse's current position
                            Marker(
                              point: _currentPosition!,
                              width: 40,
                              height: 40,
                              child: Container(
                                decoration: BoxDecoration(
                                  color: const Color(0xFF3B82F6),
                                  shape: BoxShape.circle,
                                  border: Border.all(color: Colors.white, width: 3),
                                  boxShadow: [
                                    BoxShadow(
                                      color: const Color(0xFF3B82F6).withOpacity(0.5),
                                      blurRadius: 10,
                                      spreadRadius: 2,
                                    ),
                                  ],
                                ),
                                child: const Icon(Icons.person, color: Colors.white, size: 20),
                              ),
                            ),
                            // Destination
                            Marker(
                              point: _destination,
                              width: 40,
                              height: 40,
                              child: Container(
                                decoration: BoxDecoration(
                                  color: const Color(0xFFEF4444),
                                  shape: BoxShape.circle,
                                  border: Border.all(color: Colors.white, width: 3),
                                  boxShadow: [
                                    BoxShadow(
                                      color: const Color(0xFFEF4444).withOpacity(0.5),
                                      blurRadius: 10,
                                      spreadRadius: 2,
                                    ),
                                  ],
                                ),
                                child: const Icon(Icons.emergency, color: Colors.white, size: 20),
                              ),
                            ),
                          ],
                        ),
                        // Route line
                        PolylineLayer(
                          polylines: [
                            Polyline(
                              points: [_currentPosition!, _destination],
                              color: const Color(0xFF3B82F6),
                              strokeWidth: 4,
                            ),
                          ],
                        ),
                      ],
                    ),
                  ),
          ),

          // Controls Area
          Expanded(
            flex: 1,
            child: Padding(
              padding: const EdgeInsets.all(24),
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  if (_status != 'ArrivedOnSite') ...[
                    SizedBox(
                      width: double.infinity,
                      height: 52,
                      child: ElevatedButton.icon(
                        onPressed: _toggleTracking,
                        icon: Icon(_isTracking ? Icons.stop : Icons.navigation),
                        label: Text(
                          _isTracking ? 'Stop Tracking' : 'Start Navigation',
                          style: const TextStyle(fontSize: 16, fontWeight: FontWeight.w600),
                        ),
                        style: ElevatedButton.styleFrom(
                          backgroundColor: _isTracking ? const Color(0xFFEF4444) : const Color(0xFF3B82F6),
                          foregroundColor: Colors.white,
                          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(14)),
                          elevation: 0,
                        ),
                      ),
                    ),
                    const SizedBox(height: 12),
                    SizedBox(
                      width: double.infinity,
                      height: 52,
                      child: OutlinedButton.icon(
                        onPressed: _isTracking ? _markArrived : null,
                        icon: const Icon(Icons.location_on),
                        label: const Text(
                          'Mark Arrived On Site',
                          style: TextStyle(fontSize: 16, fontWeight: FontWeight.w600),
                        ),
                        style: OutlinedButton.styleFrom(
                          foregroundColor: const Color(0xFF22C55E),
                          side: const BorderSide(color: Color(0xFF22C55E)),
                          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(14)),
                        ),
                      ),
                    ),
                  ] else ...[
                    SizedBox(
                      width: double.infinity,
                      height: 52,
                      child: ElevatedButton.icon(
                        onPressed: () {
                          Navigator.pushNamed(
                            context,
                            '/vitals-entry',
                            arguments: widget.dispatch,
                          );
                        },
                        icon: const Icon(Icons.medical_services),
                        label: const Text(
                          'Record Vitals & Complete',
                          style: TextStyle(fontSize: 16, fontWeight: FontWeight.w600),
                        ),
                        style: ElevatedButton.styleFrom(
                          backgroundColor: const Color(0xFF22C55E),
                          foregroundColor: Colors.white,
                          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(14)),
                          elevation: 0,
                        ),
                      ),
                    ),
                  ],
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }
}
