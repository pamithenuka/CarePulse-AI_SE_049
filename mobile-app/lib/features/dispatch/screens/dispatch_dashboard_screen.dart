import 'dart:async';

import '../services/location_service.dart';

import 'package:provider/provider.dart';

import '../../../providers/auth_provider.dart';

import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

import '../services/api_service.dart';

class DispatchDashboardScreen extends StatefulWidget {
  const DispatchDashboardScreen({super.key});

  @override
  State<DispatchDashboardScreen> createState() =>
      _DispatchDashboardScreenState();
}

class _DispatchDashboardScreenState extends State<DispatchDashboardScreen>
    with WidgetsBindingObserver {
  final ApiService _apiService = ApiService();
  List<dynamic> _dispatches = [];
  bool _isLoading = true;
  String? _error;
  final LocationService _location = LocationService();
  Timer? _locationTimer;
  bool _available = false;
  bool _busy = false;
  bool _availabilityLoaded = false;
  bool _changingAvailability = false;
  bool _sharing = false;
  bool _sendingLocation = false;
  int _locationGeneration = 0;
  String _locationStatus = 'Location sharing is off. ETA may be unavailable.';

  void _stopSharing() {
    _locationGeneration++;
    _locationTimer?.cancel();
    _locationTimer = null;
    _sharing = false;
  }

  @override
  void dispose() {
    _stopSharing();
    WidgetsBinding.instance.removeObserver(this);
    super.dispose();
  }

  @override
  void didChangeAppLifecycleState(AppLifecycleState state) {
    if (state == AppLifecycleState.paused ||
        state == AppLifecycleState.hidden ||
        state == AppLifecycleState.detached) {
      _stopSharing();
      if (mounted) {
        setState(
          () => _locationStatus =
              'Location sharing paused. Tap Share location when you return.',
        );
      }
    }
  }

  Future<void> _refreshAvailability() async {
    final state = await _apiService.getMyAvailability();
    if (!mounted) return;
    setState(() {
      _available = state['isAvailable'] == true;
      _busy = state['hasActiveDispatch'] == true;
      _availabilityLoaded = true;
      if (!_available || _busy) _stopSharing();
    });
  }

  Future<void> _sendAvailableLocation() async {
    if (!_sharing || _sendingLocation) return;
    final generation = _locationGeneration;
    _sendingLocation = true;
    try {
      await _refreshAvailability();
      if (!_sharing || generation != _locationGeneration) return;
      final position = await _location.getCurrentPosition();
      if (!_sharing || generation != _locationGeneration) return;
      if (DateTime.now().difference(position.timestamp).abs() >
          const Duration(minutes: 1)) {
        throw StateError('GPS fix is stale');
      }
      await _apiService.updateAvailableLocation(
        position.latitude,
        position.longitude,
      );
      if (mounted && generation == _locationGeneration) {
        setState(
          () => _locationStatus =
              'Location updated at ${DateFormat('h:mm a').format(DateTime.now())}. Sharing while this screen is open.',
        );
      }
    } catch (e) {
      if (mounted && generation == _locationGeneration) {
        setState(() {
          _stopSharing();
          _locationStatus = 'Could not share location. Check GPS and connection, then retry. ETA may be unavailable.';
        });
      }
    } finally {
      _sendingLocation = false;
    }
  }

  Future<void> _goAvailable() async {
    setState(() => _changingAvailability = true);
    final generation = _locationGeneration;
    try {
      // Availability remains usable when permission is denied, with unknown ETA.
      final permitted = await _location.requestPermission();
      if (!mounted || generation != _locationGeneration) return;
      if (!_available) await _apiService.setMyAvailability(true);
      if (!mounted || generation != _locationGeneration) return;
      await _refreshAvailability();
      if (!mounted || !_available || generation != _locationGeneration) return;
      if (!permitted) {
        setState(
          () => _locationStatus = 'Location permission or GPS unavailable. Doctors will see ETA unavailable.',
        );
        return;
      }
      setState(() => _sharing = true);
      await _sendAvailableLocation();
      if (_sharing && mounted && generation == _locationGeneration) {
        _locationTimer = Timer.periodic(
          const Duration(seconds: 30),
          (_) => _sendAvailableLocation(),
        );
      }
    } catch (e) {
      if (mounted) {
        setState(
          () => _locationStatus =
              'Could not update availability. Refresh and try again.',
        );
      }
    } finally {
      if (mounted) setState(() => _changingAvailability = false);
    }
  }

  Future<void> _goUnavailable() async {
    setState(() {
      _changingAvailability = true;
      _stopSharing();
    });
    try {
      await _apiService.setMyAvailability(false);
      await _refreshAvailability();
      if (mounted) setState(() => _locationStatus = 'Location sharing is off.');
    } catch (e) {
      if (mounted) {
        setState(
          () => _locationStatus =
              'Could not change availability. Refresh to check your status.',
        );
      }
    } finally {
      if (mounted) setState(() => _changingAvailability = false);
    }
  }

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addObserver(this);
    _loadDispatches();
  }

  Future<void> _loadDispatches() async {
    setState(() {
      _isLoading = true;
      _error = null;
    });
    try {
      final data = await _apiService.getActiveDispatches();
      await _refreshAvailability();
      if (!mounted) return;
      setState(() {
        _dispatches = data['tickets'] ?? [];
        _isLoading = false;
      });
    } catch (e) {
      if (mounted) {
        setState(() {
          _isLoading = false;
          _error = e.toString();
          _dispatches = [];
        });
      }
    }
  }

  Color _statusColor(String status) {
    switch (status) {
      case 'Assigned':
        return const Color(0xFFF59E0B);
      case 'EnRoute':
        return const Color(0xFF3B82F6);
      case 'ArrivedOnSite':
        return const Color(0xFF22C55E);
      case 'Completed':
        return const Color(0xFF6B7280);
      case 'Escalated':
        return const Color(0xFFEF4444);
      default:
        return const Color(0xFF94A3B8);
    }
  }

  IconData _statusIcon(String status) {
    switch (status) {
      case 'Assigned':
        return Icons.assignment_outlined;
      case 'EnRoute':
        return Icons.navigation_outlined;
      case 'ArrivedOnSite':
        return Icons.location_on;
      case 'Completed':
        return Icons.check_circle_outline;
      case 'Escalated':
        return Icons.warning_amber_rounded;
      default:
        return Icons.info_outline;
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFFF4F6F8),
      drawer: Drawer(
        child: SafeArea(
          child: ListView(
            padding: const EdgeInsets.all(20),
            children: [
              Row(
                children: [
                  Expanded(
                    child: Text(
                      'Availability & location',
                      style: Theme.of(context).textTheme.titleLarge,
                    ),
                  ),
                  IconButton(
                    tooltip: 'Close menu',
                    icon: const Icon(Icons.close),
                    onPressed: () => Navigator.pop(context),
                  ),
                ],
              ),
              const SizedBox(height: 16),
              _availabilityCard(context),
            ],
          ),
        ),
      ),
      appBar: AppBar(
        iconTheme: const IconThemeData(color: Colors.white),
        leading: Builder(
          builder: (context) => IconButton(
            icon: const Icon(Icons.location_on_outlined),
            tooltip: 'Availability and location',
            onPressed: () => Scaffold.of(context).openDrawer(),
          ),
        ),
        backgroundColor: const Color(0xFF0B5F6B),
        elevation: 0,
        title: const Text(
          'Nurse home',
          style: TextStyle(fontWeight: FontWeight.w700, color: Colors.white),
        ),
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh, color: Colors.white),
            tooltip: 'Refresh dispatches',
            onPressed: _isLoading ? null : _loadDispatches,
          ),
          IconButton(
            icon: const Icon(Icons.logout, color: Colors.white),
            tooltip: 'Sign out',
            onPressed: () async {
              _stopSharing();
              await context.read<AuthProvider>().logout();
              if (context.mounted) {
                Navigator.pushReplacementNamed(context, '/login');
              }
            },
          ),
        ],
      ),
      body: DecoratedBox(
        decoration: const BoxDecoration(
          gradient: LinearGradient(
            begin: Alignment.topLeft,
            end: Alignment.bottomRight,
            colors: [Color(0xFFDDF3EF), Color(0xFFE5EDF9)],
          ),
        ),
        child: SafeArea(
          child: RefreshIndicator(
            onRefresh: _loadDispatches,
            child: ListView(
              physics: const AlwaysScrollableScrollPhysics(),
              padding: const EdgeInsets.all(20),
              children: [
                _profileCard(context),
                const SizedBox(height: 28),
                Text(
                  'Active dispatches',
                  style: Theme.of(context).textTheme.titleLarge,
                ),
                const SizedBox(height: 16),
                if (_isLoading)
                  const Padding(
                    padding: EdgeInsets.all(32),
                    child: Center(child: CircularProgressIndicator()),
                  )
                else if (_error != null)
                  _messageCard(
                    Icons.cloud_off_outlined,
                    'Could not load dispatches',
                    'Check your connection and try again.',
                    retry: true,
                  )
                else if (_dispatches.isEmpty)
                  _messageCard(
                    Icons.assignment_turned_in_outlined,
                    'No active dispatches',
                    'Your assignments will appear here. Pull down to refresh.',
                  )
                else
                  ..._dispatches.map((dispatch) => _dispatchCard(dispatch)),
              ],
            ),
          ),
        ),
      ),
    );
  }

  Widget _availabilityCard(BuildContext context) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              _busy
                  ? 'On an active assignment'
                  : _available
                  ? 'Available for assignment'
                  : 'Unavailable',
              style: Theme.of(context).textTheme.titleMedium,
            ),
            const SizedBox(height: 8),
            Text(
              _busy
                  ? 'Open your dispatch to use route tracking.'
                  : _locationStatus,
            ),
            if (!_busy)
              Wrap(
                spacing: 12,
                runSpacing: 12,
                children: [
                  FilledButton(
                    onPressed:
                        !_availabilityLoaded ||
                            _changingAvailability ||
                            _sharing
                        ? null
                        : _goAvailable,
                    child: Text(_available ? 'Share location' : 'Go available'),
                  ),
                  if (_available)
                    OutlinedButton(
                      onPressed: _changingAvailability ? null : _goUnavailable,
                      child: const Text('Go unavailable'),
                    ),
                ],
              ),
          ],
        ),
      ),
    );
  }

  Widget _profileCard(BuildContext context) {
    final user = context.watch<AuthProvider>().currentUser;
    final name = user?.fullName.trim() ?? '';
    final email = user?.email.trim() ?? '';
    return Card(
      margin: EdgeInsets.zero,
      child: Padding(
        padding: const EdgeInsets.all(20),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Row(
              children: [
                CircleAvatar(
                  radius: 26,
                  backgroundColor: Color(0xFFDDF3EF),
                  foregroundColor: Color(0xFF075C63),
                  child: Icon(Icons.person_outline_rounded, size: 30),
                ),
                SizedBox(width: 12),
                Expanded(
                  child: Text(
                    'My profile',
                    style: TextStyle(fontWeight: FontWeight.w600),
                  ),
                ),
              ],
            ),
            const SizedBox(height: 16),
            Text(
              name.isEmpty ? 'Nurse account' : name,
              style: Theme.of(context).textTheme.headlineSmall,
            ),
            const SizedBox(height: 8),
            const Chip(
              avatar: Icon(Icons.medical_services_outlined, size: 18),
              label: Text('Nurse'),
              backgroundColor: Color(0xFFE5EDF9),
              side: BorderSide.none,
            ),
            const Divider(height: 28),
            Text('Email address', style: Theme.of(context).textTheme.bodySmall),
            const SizedBox(height: 4),
            Text(email.isEmpty ? 'Email unavailable' : email),
          ],
        ),
      ),
    );
  }

  Widget _messageCard(
    IconData icon,
    String title,
    String message, {
    bool retry = false,
  }) {
    return Card(
      margin: EdgeInsets.zero,
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          children: [
            Icon(icon, size: 40, color: const Color(0xFF0B5F6B)),
            const SizedBox(height: 12),
            Text(
              title,
              style: Theme.of(context).textTheme.titleMedium,
              textAlign: TextAlign.center,
            ),
            const SizedBox(height: 8),
            Text(message, textAlign: TextAlign.center),
            if (retry) ...[
              const SizedBox(height: 12),
              OutlinedButton(
                onPressed: _loadDispatches,
                child: const Text('Try again'),
              ),
            ],
          ],
        ),
      ),
    );
  }

  Widget _dispatchCard(dynamic dispatch) {
    final status = dispatch['status'] ?? 'Unknown';
    final assignedAt = dispatch['assignedAt'] != null
        ? DateFormat('MMM d, h:mm a')
              .format(DateTime.parse(dispatch['assignedAt']).toLocal())
        : 'N/A';

    return Container(
      margin: const EdgeInsets.only(bottom: 12),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: const Color(0xFFE2E8F0)),
        boxShadow: const [
          BoxShadow(
            color: Color(0x0C000000),
            blurRadius: 4,
            offset: Offset(0, 2),
          ),
        ],
      ),
      child: ClipRRect(
        borderRadius: BorderRadius.circular(14),
        child: Container(
          decoration: BoxDecoration(
            border: Border(
              left: BorderSide(color: _statusColor(status), width: 4),
            ),
          ),
          child: Material(
            color: Colors.transparent,
            child: ListTile(
              contentPadding: const EdgeInsets.symmetric(
                horizontal: 16,
                vertical: 10,
              ),
              leading: Container(
                padding: const EdgeInsets.all(10),
                decoration: BoxDecoration(
                  color: _statusColor(status).withValues(alpha: 0.1),
                  borderRadius: BorderRadius.circular(12),
                ),
                child: Icon(_statusIcon(status), color: _statusColor(status)),
              ),
              title: Text(
                (dispatch['patientName'] as String?)?.trim().isNotEmpty == true
                    ? dispatch['patientName'] as String
                    : 'Patient name not provided',
                style: const TextStyle(
                  color: Color(0xFF334155),
                  fontWeight: FontWeight.w600,
                ),
              ),
              subtitle: Padding(
                padding: const EdgeInsets.only(top: 6),
                child: Text(
                  'Assigned: $assignedAt\nProfile address: ${dispatch['patientAddress'] ?? 'Not provided'}\nReference: ${dispatch['id'].toString().length > 8 ? dispatch['id'].toString().substring(0, 8) : dispatch['id']}',
                  style: const TextStyle(
                    color: Color(0xFF64748B),
                    fontSize: 13,
                  ),
                ),
              ),
              trailing: Container(
                padding: const EdgeInsets.symmetric(
                  horizontal: 10,
                  vertical: 6,
                ),
                decoration: BoxDecoration(
                  color: _statusColor(status).withValues(alpha: 0.1),
                  borderRadius: BorderRadius.circular(20),
                ),
                child: Text(
                  status,
                  style: TextStyle(
                    color: _statusColor(status),
                    fontWeight: FontWeight.w600,
                    fontSize: 12,
                  ),
                ),
              ),
              onTap: () async {
                _stopSharing();
                await Navigator.pushNamed(
                  context,
                  '/navigation',
                  arguments: dispatch,
                );
                if (mounted) _loadDispatches();
              },
            ),
          ),
        ),
      ),
    );
  }
}
