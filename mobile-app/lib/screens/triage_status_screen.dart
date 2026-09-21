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
    
    // Only poll if it's high risk and needs approval
    if (widget.triageData['requiresDoctorApproval'] == true) {
      _timer = Timer.periodic(const Duration(seconds: 5), (timer) {
        if (!_isApproved) {
          _fetchAuditLogs();
        }
      });
    }
  }

  @override
  void dispose() {
    _timer?.cancel();
    super.dispose();
  }

  Future<void> _fetchAuditLogs() async {
    try {
      debugPrint("Triage ID: ${widget.triageData['id']}");
      final logs = await _apiService.getAuditLog(widget.triageData['id']);
      debugPrint("Audit logs received: $logs");
      if (mounted) {
        setState(() {
          _auditLogs = logs;
          _isApproved = logs.any((log) => log['logMessage'].toString().toLowerCase().contains('approved by doctor'));
        });
      }
    } catch (e) {
      debugPrint("Error fetching logs: $e");
    }
  }

  void _showTriageDetails() {
    showDialog(
      context: context,
      builder: (context) {
        return AlertDialog(
          title: const Text("Triage Details"),
          content: _auditLogs.isEmpty
              ? const Text("No triage details available.")
              : SizedBox(
                  width: double.maxFinite,
                  height: 300,
                  child: ListView.builder(
                    shrinkWrap: true,
                    itemCount: _auditLogs.length,
                    itemBuilder: (context, index) {
                      final log = _auditLogs[index];
                      return Card(
                        margin: const EdgeInsets.only(bottom: 10),
                        child: Padding(
                          padding: const EdgeInsets.all(12.0),
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(
                                log['logMessage'] ?? '',
                                style: const TextStyle(fontSize: 14),
                              ),
                              const SizedBox(height: 6),
                              Text(
                                log['createdAt'] ?? '',
                                style: TextStyle(
                                  fontSize: 12,
                                  color: Colors.grey.shade600,
                                ),
                              ),
                            ],
                          ),
                        ),
                      );
                    },
                  ),
                ),
          actions: [
            TextButton(
              onPressed: () => Navigator.of(context).pop(),
              child: const Text("CLOSE"),
            ),
          ],
        );
      },
    );
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

  Widget _buildLowRiskUI(int riskScore) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const Row(
          children: [
            Text("🟢 ", style: TextStyle(fontSize: 24)),
            Text("Low Risk", style: TextStyle(fontSize: 24, fontWeight: FontWeight.bold, color: Colors.green)),
          ],
        ),
        const SizedBox(height: 10),
        Text("Risk Assessment: $riskScore/10", style: const TextStyle(fontSize: 16, fontWeight: FontWeight.bold)),
        const SizedBox(height: 20),
        const Text(
          "The information provided does not indicate an immediate emergency.\n\nYou can monitor your symptoms and seek medical care if they worsen.",
          style: TextStyle(fontSize: 16),
        ),
        const SizedBox(height: 30),
        SizedBox(
          width: double.infinity,
          child: ElevatedButton(
            onPressed: () {}, // Stub
            style: ElevatedButton.styleFrom(padding: const EdgeInsets.all(15)),
            child: const Text("Book a Doctor"),
          ),
        ),
        const SizedBox(height: 10),
        SizedBox(
          width: double.infinity,
          child: OutlinedButton(
            onPressed: _showTriageDetails,
            style: OutlinedButton.styleFrom(padding: const EdgeInsets.all(15)),
            child: const Text("View Triage Details"),
          ),
        ),
        const SizedBox(height: 30),
        _buildStep("Submitted", "Intake form received", true, true),
        _buildStep("Evaluated", "Risk Assessed", true, true),
        _buildStep("Recommendation", "Self-care Monitoring", true, true),
      ],
    );
  }

  Widget _buildMediumRiskUI(int riskScore) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const Row(
          children: [
            Text("🟡 ", style: TextStyle(fontSize: 24)),
            Text("Medical Consultation Recommended", style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold, color: Colors.orange)),
          ],
        ),
        const SizedBox(height: 10),
        Text("Risk Assessment: $riskScore/10", style: const TextStyle(fontSize: 16, fontWeight: FontWeight.bold)),
        const SizedBox(height: 20),
        const Text(
          "Based on the information provided, a medical consultation is recommended.",
          style: TextStyle(fontSize: 16),
        ),
        const SizedBox(height: 30),
        Row(
          children: [
            Expanded(
              child: ElevatedButton(
                onPressed: () {}, // Stub
                style: ElevatedButton.styleFrom(padding: const EdgeInsets.all(15)),
                child: const Text("Find a Doctor"),
              ),
            ),
            const SizedBox(width: 10),
            Expanded(
              child: ElevatedButton(
                onPressed: () {}, // Stub
                style: ElevatedButton.styleFrom(padding: const EdgeInsets.all(15)),
                child: const Text("Book Appointment"),
              ),
            ),
          ],
        ),
        const SizedBox(height: 10),
        SizedBox(
          width: double.infinity,
          child: OutlinedButton(
            onPressed: _showTriageDetails,
            style: OutlinedButton.styleFrom(padding: const EdgeInsets.all(15)),
            child: const Text("View Triage Details"),
          ),
        ),
        const SizedBox(height: 30),
        _buildStep("Submitted", "Intake form received", true, true),
        _buildStep("Evaluated", "Risk Assessed", true, true),
        _buildStep("Recommendation", "Consultation Recommended", true, true),
      ],
    );
  }

  Widget _buildHighRiskUI(int riskScore) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const Row(
          children: [
            Text("🔴 ", style: TextStyle(fontSize: 24)),
            Text("High Risk", style: TextStyle(fontSize: 24, fontWeight: FontWeight.bold, color: Colors.red)),
          ],
        ),
        const SizedBox(height: 10),
        Text("Risk Assessment: $riskScore/10", style: const TextStyle(fontSize: 16, fontWeight: FontWeight.bold)),
        const SizedBox(height: 20),
        const Text(
          "Doctor approval is required before emergency action can proceed.\n\nWaiting for doctor review...",
          style: TextStyle(fontSize: 16, color: Colors.red),
        ),
        const SizedBox(height: 30),
        
        _buildStep("Submitted", "Intake form received", true, true),
        _buildStep("Evaluated", "Risk Assessed", true, true),
        _buildStep("Pending Doctor Review", "Waiting for clinical review", !_isApproved, _isApproved),
        _buildStep("Doctor Decision", _isApproved ? "Approved" : "Requires Authorization", _isApproved, false),
      ],
    );
  }

  @override
  Widget build(BuildContext context) {
    final riskLevel = widget.triageData['riskLevel'];
    final riskScore = widget.triageData['riskScore'];

    return Scaffold(
      appBar: AppBar(
        title: const Text('Triage Status'),
        backgroundColor: Colors.blue.shade800,
        foregroundColor: Colors.white,
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(20.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            if (riskLevel == 'LOW') _buildLowRiskUI(riskScore),
            if (riskLevel == 'MEDIUM') _buildMediumRiskUI(riskScore),
            if (riskLevel == 'HIGH') _buildHighRiskUI(riskScore),
            
            const SizedBox(height: 30),
            SizedBox(
              width: double.infinity,
              child: TextButton(
                onPressed: () => Navigator.pop(context),
                style: TextButton.styleFrom(
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
