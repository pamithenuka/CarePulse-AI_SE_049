import 'package:flutter/material.dart';
import 'package:flutter_map/flutter_map.dart';
import 'package:latlong2/latlong.dart';

import '../services/location_service.dart';
import '../services/api_service.dart';

class NavigationScreen extends StatefulWidget {
  final Map<String, dynamic> dispatch;
  const NavigationScreen({super.key, required this.dispatch});
  @override
  State<NavigationScreen> createState() => _NavigationScreenState();
}

class _NavigationScreenState extends State<NavigationScreen> {
  final _location = LocationService();
  final _api = ApiService();
  late Future<Map<String, dynamic>> _patientSummary;
  LatLng? _position;
  String? _error;
  bool _tracking = false, _busy = false;
  late String _status;
  LatLng? get _destination =>
      widget.dispatch['destinationLat'] is num &&
          widget.dispatch['destinationLng'] is num
      ? LatLng(
          (widget.dispatch['destinationLat'] as num).toDouble(),
          (widget.dispatch['destinationLng'] as num).toDouble(),
        )
      : null;
  @override
  void initState() {
    super.initState();
    _patientSummary = _api.getPatientSummary(widget.dispatch['id'] as String);
    _status = widget.dispatch['status'] as String? ?? 'Assigned';
  }

  void _failed(Object e) {
    if (mounted) {
      setState(() {
        _error = e.toString();
        _tracking = false;
      });
    }
  }

  Future<void> _start() async {
    setState(() {
      _busy = true;
      _error = null;
    });
    try {
      if (!await _location.requestPermission()) {
        throw Exception(
          'Enable GPS and location permission to start tracking.',
        );
      }
      final p = await _location.getCurrentPosition();
      await _api.updateLocation(
        widget.dispatch['id'],
        p.latitude,
        p.longitude,
        p.speed * 3.6,
        p.heading,
      );
      if (!mounted) return;
      setState(() {
        _position = LatLng(p.latitude, p.longitude);
        _tracking = true;
        _status = 'EnRoute';
      });
      _location.startTracking(
        widget.dispatch['id'],
        onUpdate: (p) {
          if (mounted) {
            setState(() => _position = LatLng(p.latitude, p.longitude));
          }
        },
        onError: _failed,
      );
    } catch (e) {
      _failed(e);
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  Future<void> _arrive() async {
    setState(() => _busy = true);
    _location.stopTracking();
    try {
      await _api.arrive(widget.dispatch['id']);
      if (mounted) {
        setState(() {
          _tracking = false;
          _status = 'ArrivedOnSite';
        });
      }
    } catch (e) {
      _failed(e);
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  @override
  void dispose() {
    _location.stopTracking();
    super.dispose();
  }

  Widget _patientCard() => FutureBuilder<Map<String, dynamic>>(
    future: _patientSummary,
    builder: (context, snapshot) {
      if (snapshot.connectionState != ConnectionState.done) {
        return const Padding(
          padding: EdgeInsets.all(16),
          child: LinearProgressIndicator(),
        );
      }
      if (snapshot.hasError) {
        return Padding(
          padding: const EdgeInsets.all(16),
          child: Column(
            children: [
              const Text('Patient details could not be loaded.'),
              TextButton(
                onPressed: () => setState(() {
                  _patientSummary = _api.getPatientSummary(
                    widget.dispatch['id'] as String,
                  );
                }),
                child: const Text('Retry patient details'),
              ),
            ],
          ),
        );
      }
      final data = snapshot.data ?? {};
      String value(String key) {
        final text = data[key]?.toString().trim() ?? '';
        return text.isEmpty ? 'Not provided' : text;
      }

      return Card(
        margin: const EdgeInsets.all(16),
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                value('patientName'),
                style: Theme.of(context).textTheme.titleLarge,
              ),
              const SizedBox(height: 12),
              Text('Reported symptoms: ${value('symptoms')}'),
              const Divider(height: 24),
              Text('Phone: ${value('phoneNumber')}'),
              const SizedBox(height: 8),
              Text('Allergies (patient record): ${value('allergies')}'),
              const SizedBox(height: 8),
              Text('Blood type (patient record): ${value('bloodType')}'),
              const SizedBox(height: 8),
              Text('Profile address: ${value('patientAddress')}'),
              const SizedBox(height: 8),
              const Text(
                'The profile address may differ from this visit’s destination. Use the confirmed map location below.',
              ),
            ],
          ),
        ),
      );
    },
  );

  @override
  Widget build(BuildContext context) {
    final destination = _destination;
    return Scaffold(
      appBar: AppBar(title: Text('Dispatch — $_status')),
      body: SafeArea(
        child: ListView(
          children: [
            _patientCard(),
            if (_error != null)
              Padding(
                padding: const EdgeInsets.all(12),
                child: Text(_error!, style: const TextStyle(color: Colors.red)),
              ),
            if (_error != null)
              TextButton(
                onPressed: () => Navigator.pushNamed(context, '/login'),
                child: const Text('Sign in again'),
              ),
            SizedBox(
              height: 300,
              child: destination == null
                  ? const Center(
                      child: Text(
                        'Destination unavailable. Contact the dispatcher.',
                      ),
                    )
                  : FlutterMap(
                      options: MapOptions(
                        initialCenter: destination,
                        initialZoom: 13,
                      ),
                      children: [
                        TileLayer(
                          urlTemplate:
                              'https://tile.openstreetmap.org/{z}/{x}/{y}.png',
                          userAgentPackageName: 'com.carepulse.mobile',
                        ),
                        MarkerLayer(
                          markers: [
                            Marker(
                              point: destination,
                              child: const Icon(
                                Icons.location_on,
                                color: Colors.red,
                                size: 36,
                              ),
                            ),
                            if (_position != null)
                              Marker(
                                point: _position!,
                                child: const Icon(
                                  Icons.person_pin_circle,
                                  color: Colors.blue,
                                  size: 36,
                                ),
                              ),
                          ],
                        ),
                      ],
                    ),
            ),
            Padding(
              padding: const EdgeInsets.all(16),
              child: Column(
                children: [
                  const Text(
                    'Map shows the confirmed destination and last successfully sent position. Use a road navigation service for directions.',
                  ),
                  const SizedBox(height: 8),
                  if (_status != 'ArrivedOnSite') ...[
                    ElevatedButton(
                      onPressed: _busy || destination == null
                          ? null
                          : _tracking
                          ? () {
                              _location.stopTracking();
                              setState(() => _tracking = false);
                            }
                          : _start,
                      child: Text(
                        _tracking
                            ? 'Pause tracking'
                            : 'Start / resume tracking',
                      ),
                    ),
                    OutlinedButton(
                      onPressed: _busy || _status != 'EnRoute' ? null : _arrive,
                      child: const Text('Confirm arrival on site'),
                    ),
                  ] else
                    ElevatedButton(
                      onPressed: () => Navigator.pushNamed(
                        context,
                        '/vitals-entry',
                        arguments: widget.dispatch,
                      ),
                      child: const Text('Record vitals'),
                    ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}
