import 'package:flutter/material.dart';
import '../services/triage_api_service.dart';
import 'triage_status_screen.dart';

class SymptomIntakeScreen extends StatefulWidget {
  const SymptomIntakeScreen({super.key});

  @override
  State<SymptomIntakeScreen> createState() => _SymptomIntakeScreenState();
}

class _SymptomIntakeScreenState extends State<SymptomIntakeScreen> {
  final _symptomsController = TextEditingController();
  final TriageApiService _apiService = TriageApiService();
  bool _isLoading = false;

  // Mock patient ID for testing
  final String _patientId = "3fa85f64-5717-4562-b3fc-2c963f66afa6";

  Future<void> _submitTriage() async {
    if (_symptomsController.text.isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text("Please describe your symptoms.")),
      );
      return;
    }

    setState(() => _isLoading = true);

    try {
      final result = await _apiService.submitTriage(
        _patientId, 
        _symptomsController.text
      );

      if (!mounted) return;

      // Navigate to status screen with the result
      Navigator.pushReplacement(
        context,
        MaterialPageRoute(
          builder: (context) => TriageStatusScreen(triageData: result),
        ),
      );
    } catch (e) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text("Error: ${e.toString()}")),
      );
    } finally {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Emergency Triage Intake'),
        backgroundColor: Colors.blue.shade800,
        foregroundColor: Colors.white,
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(20.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            const Text(
              "Describe your symptoms in detail:",
              style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold),
            ),
            const SizedBox(height: 10),
            TextField(
              controller: _symptomsController,
              maxLines: 5,
              decoration: InputDecoration(
                hintText: "E.g., I have severe chest pain and shortness of breath...",
                border: OutlineInputBorder(
                  borderRadius: BorderRadius.circular(10),
                ),
                filled: true,
                fillColor: Colors.grey.shade100,
              ),
            ),
            const SizedBox(height: 20),
            OutlinedButton.icon(
              onPressed: () {
                // Mock action for optional photo attachment
                ScaffoldMessenger.of(context).showSnackBar(
                  const SnackBar(content: Text("Photo attachment not supported yet in backend.")),
                );
              },
              icon: const Icon(Icons.camera_alt),
              label: const Text("Attach Photo (Optional)"),
              style: OutlinedButton.styleFrom(
                padding: const EdgeInsets.all(15),
              ),
            ),
            const SizedBox(height: 40),
            ElevatedButton(
              onPressed: _isLoading ? null : _submitTriage,
              style: ElevatedButton.styleFrom(
                backgroundColor: Colors.red.shade600,
                foregroundColor: Colors.white,
                padding: const EdgeInsets.all(16),
                textStyle: const TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(10),
                )
              ),
              child: _isLoading 
                  ? const CircularProgressIndicator(color: Colors.white)
                  : const Text("SUBMIT TRIAGE"),
            ),
          ],
        ),
      ),
    );
  }
}
