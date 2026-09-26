import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import '../services/api_service.dart';

class DispatchDashboardScreen extends StatefulWidget {
  const DispatchDashboardScreen({super.key});

  @override
  State<DispatchDashboardScreen> createState() => _DispatchDashboardScreenState();
}

class _DispatchDashboardScreenState extends State<DispatchDashboardScreen> {
  final ApiService _apiService = ApiService();
  List<dynamic> _dispatches = [];
  bool _isLoading = true;

  @override
  void initState() {
    super.initState();
    _loadDispatches();
  }

  Future<void> _loadDispatches() async {
    setState(() => _isLoading = true);
    try {
      final data = await _apiService.getActiveDispatches();
      setState(() {
        _dispatches = data['tickets'] ?? [];
        _isLoading = false;
      });
    } catch (e) {
      setState(() => _isLoading = false);
      // Show demo data for standalone testing
      setState(() {
        _dispatches = [
          {
            'id': 'demo-1',
            'status': 'Assigned',
            'assignedAt': DateTime.now().toIso8601String(),
            'triageTicketId': 'triage-001',
          },
          {
            'id': 'demo-2',
            'status': 'EnRoute',
            'assignedAt': DateTime.now().subtract(const Duration(hours: 1)).toIso8601String(),
            'triageTicketId': 'triage-002',
          },
        ];
      });
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
      appBar: AppBar(
        backgroundColor: const Color(0xFF0B5F6B),
        elevation: 0,
        title: const Row(
          children: [
            Icon(Icons.dashboard_rounded, color: Colors.white),
            SizedBox(width: 10),
            Text(
              'Active Dispatches',
              style: TextStyle(
                fontWeight: FontWeight.w700,
                color: Colors.white,
              ),
            ),
          ],
        ),
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh, color: Colors.white),
            onPressed: _loadDispatches,
          ),
          IconButton(
            icon: const Icon(Icons.logout, color: Colors.white),
            onPressed: () async {
              await _apiService.clearToken();
              if (mounted) Navigator.pushReplacementNamed(context, '/login');
            },
          ),
        ],
      ),
      body: _isLoading
          ? const Center(child: CircularProgressIndicator(color: Color(0xFF0F766E)))
          : _dispatches.isEmpty
              ? Center(
                  child: Column(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      Icon(Icons.inbox_outlined, size: 64, color: const Color(0xFF94A3B8).withOpacity(0.5)),
                      const SizedBox(height: 16),
                      const Text(
                        'No active dispatches',
                        style: TextStyle(color: Color(0xFF64748B), fontSize: 16),
                      ),
                    ],
                  ),
                )
              : RefreshIndicator(
                  onRefresh: _loadDispatches,
                  color: const Color(0xFF0F766E),
                  child: ListView.builder(
                    padding: const EdgeInsets.all(16),
                    itemCount: _dispatches.length,
                    itemBuilder: (context, index) {
                      final dispatch = _dispatches[index];
                      final status = dispatch['status'] ?? 'Unknown';
                      final assignedAt = dispatch['assignedAt'] != null
                          ? DateFormat('MMM d, h:mm a').format(DateTime.parse(dispatch['assignedAt']))
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
                                contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 10),
                                leading: Container(
                                  padding: const EdgeInsets.all(10),
                                  decoration: BoxDecoration(
                                    color: _statusColor(status).withOpacity(0.1),
                                    borderRadius: BorderRadius.circular(12),
                                  ),
                                  child: Icon(_statusIcon(status), color: _statusColor(status)),
                                ),
                              title: Text(
                                'Dispatch #${dispatch['id'].toString().length > 8 ? dispatch['id'].toString().substring(0, 8) : dispatch['id'].toString()}',
                                style: const TextStyle(
                                  color: Color(0xFF334155),
                                  fontWeight: FontWeight.w600,
                                ),
                              ),
                              subtitle: Padding(
                                padding: const EdgeInsets.only(top: 6),
                                child: Text(
                                  'Assigned: $assignedAt',
                                  style: const TextStyle(color: Color(0xFF64748B), fontSize: 13),
                                ),
                              ),
                              trailing: Container(
                                padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
                                decoration: BoxDecoration(
                                  color: _statusColor(status).withOpacity(0.1),
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
                              onTap: () {
                                Navigator.pushNamed(
                                  context,
                                  '/navigation',
                                  arguments: dispatch,
                                );
                              },
                            ),
                          ),
                        ),
                      ),
                      );
                    },
                  ),
                ),
    );
  }
}
