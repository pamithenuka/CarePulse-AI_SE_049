import 'dart:async';
import 'package:flutter/material.dart';
import '../services/triage_api_service.dart';

class TriageStatusScreen extends StatefulWidget {
  final Map<String, dynamic> triageData;

  const TriageStatusScreen({super.key, required this.triageData});

  @override
  State<TriageStatusScreen> createState() => _TriageStatusScreenState();
}

class _TriageStatusScreenState extends State<TriageStatusScreen> {
  final TriageApiService _apiService = TriageApiService();
  List<dynamic> _auditLogs = [];
  Timer? _timer;
  bool _isApproved = false;

  @override
  void initState() {
    super.initState();
    _fetchAuditLogs();
    
    // Poll the audit log to see if it gets approved (Mocking live status updates)
    _timer = Timer.periodic(const Duration(seconds: 5), (timer) {
      if (!_isApproved) {
        _fetchAuditLogs();
      }
    });
  }

  @override
  void dispose() {
    _timer?.cancel();
    super.dispose();
  }

  Future<void> _fetchAuditLogs() async {
    try {
      final logs = await _apiService.getAuditLog(widget.triageData['id']);
      if (mounted) {
        setState(() {
          _auditLogs = logs;
          // Check if any log contains "approved by doctor"
          _isApproved = logs.any((log) => log['logMessage'].toString().toLowerCase().contains('approved'));
        });
      }
    } catch (e) {
      debugPrint("Error fetching logs: $e");
    }
  }

  Widget _buildStep(String title, String subtitle, bool isActive, bool isCompleted) {
    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Column(
          children: [
            Container(
              width: 30,
              height: 30,
              decoration: BoxDecoration(
                shape: BoxShape.circle,
                color: isCompleted ? Colors.green : (isActive ? Colors.blue : Colors.grey.shade300),
              ),
              child: Icon(
                isCompleted ? Icons.check : (isActive ? Icons.circle : null),
                color: Colors.white,
                size: 20,
              ),
            ),
            Container(
              width: 2,
              height: 50,
              color: isCompleted ? Colors.green : Colors.grey.shade300,
            ),
          ],
        ),
        const SizedBox(width: 15),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                title,
                style: TextStyle(
                  fontSize: 16,
                  fontWeight: FontWeight.bold,
                  color: (isActive || isCompleted) ? Colors.black87 : Colors.grey,
                ),
              ),
              const SizedBox(height: 4),
              Text(
                subtitle,
                style: TextStyle(
                  color: Colors.grey.shade600,
                  fontSize: 13,
                ),
              ),
            ],
          ),
        ),
      ],
    );
  }

  @override
  Widget build(BuildContext context) {
    final riskLevel = widget.triageData['riskLevel'];
    final riskScore = widget.triageData['riskScore'];
    final isHighRisk = riskLevel == 'HIGH';

    return Scaffold(
      appBar: AppBar(
        title: const Text('Triage Status'),
        backgroundColor: Colors.blue.shade800,
        foregroundColor: Colors.white,
      ),
      body: Padding(
        padding: const EdgeInsets.all(20.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Container(
              padding: const EdgeInsets.all(15),
              decoration: BoxDecoration(
                color: isHighRisk ? Colors.red.shade50 : Colors.green.shade50,
                borderRadius: BorderRadius.circular(10),
                border: Border.all(color: isHighRisk ? Colors.red.shade200 : Colors.green.shade200),
              ),
              child: Row(
                children: [
                  Icon(
                    isHighRisk ? Icons.warning_amber_rounded : Icons.check_circle_outline,
                    color: isHighRisk ? Colors.red : Colors.green,
                    size: 40,
                  ),
                  const SizedBox(width: 15),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          "Risk Level: $riskLevel",
                          style: TextStyle(
                            fontSize: 18,
                            fontWeight: FontWeight.bold,
                            color: isHighRisk ? Colors.red.shade800 : Colors.green.shade800,
                          ),
                        ),
                        Text("Score: $riskScore/10"),
                      ],
                    ),
                  )
                ],
              ),
            ),
            const SizedBox(height: 30),
            const Text(
              "Live Tracking",
              style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold),
            ),
            const SizedBox(height: 20),
            
            // Stepper UI
            _buildStep("Submitted", "Intake form received", true, true),
            _buildStep("AI Risk Assessment", "Scored $riskScore/10 ($riskLevel)", true, true),
            
            if (isHighRisk)
              _buildStep("Doctor Approval", "Waiting for clinical review", !_isApproved, _isApproved),
            
            _buildStep("Status", _isApproved || !isHighRisk ? "Pending Dispatch" : "Requires Authorization", _isApproved || !isHighRisk, false),
            
            const Spacer(),
            SizedBox(
              width: double.infinity,
              child: ElevatedButton(
                onPressed: () => Navigator.pop(context),
                style: ElevatedButton.styleFrom(
                  padding: const EdgeInsets.all(15),
                ),
                child: const Text("RETURN TO HOME"),
              ),
            )
          ],
        ),
      ),
    );
  }
}
