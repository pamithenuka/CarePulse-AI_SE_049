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

  // New fields
  String _selectedDuration = 'Since this morning';
  final List<String> _durations = ['Since this morning', 'For two days', 'More than a week', 'Other'];
  
  String _selectedSeverity = 'Mild';
  final List<String> _severities = ['Mild', 'Moderate', 'Severe'];

  final Map<String, bool> _additionalSymptoms = {
    'Dizziness': false,
    'Fever': false,
    'Chest pain': false,
    'Difficulty breathing': false,
    'Vomiting': false,
  };

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
      final selectedAdditionalSymptoms = _additionalSymptoms.entries
          .where((e) => e.value)
          .map((e) => e.key)
          .toList();

      final result = await _apiService.submitTriage(
        patientId: _patientId, 
        symptoms: _symptomsController.text,
        duration: _selectedDuration,
        severity: _selectedSeverity,
        additionalSymptoms: selectedAdditionalSymptoms,
        hasPhotoAttachment: false,
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
        title: const Text('Report Symptoms'),
        backgroundColor: Colors.blue.shade800,
        foregroundColor: Colors.white,
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(20.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            const Text(
              "What are you experiencing?",
              style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold),
            ),
            const SizedBox(height: 10),
            TextField(
              controller: _symptomsController,
              maxLines: 4,
              decoration: InputDecoration(
                hintText: "E.g., I have severe chest pain...",
                border: OutlineInputBorder(
                  borderRadius: BorderRadius.circular(10),
                ),
                filled: true,
                fillColor: Colors.grey.shade100,
              ),
            ),
            const SizedBox(height: 20),

            const Text(
              "How long have you had this?",
              style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold),
            ),
            const SizedBox(height: 10),
            DropdownButtonFormField<String>(
              value: _selectedDuration,
              items: _durations.map((d) => DropdownMenuItem(value: d, child: Text(d))).toList(),
              onChanged: (val) => setState(() => _selectedDuration = val!),
              decoration: InputDecoration(
                border: OutlineInputBorder(borderRadius: BorderRadius.circular(10)),
                contentPadding: const EdgeInsets.symmetric(horizontal: 10, vertical: 15),
              ),
            ),
            const SizedBox(height: 20),

            const Text(
              "How severe is it?",
              style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold),
            ),
            Column(
              children: _severities.map((s) => RadioListTile<String>(
                title: Text(s),
                value: s,
                groupValue: _selectedSeverity,
                onChanged: (val) => setState(() => _selectedSeverity = val!),
                contentPadding: EdgeInsets.zero,
              )).toList(),
            ),
            const SizedBox(height: 10),

            const Text(
              "Other symptoms",
              style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold),
            ),
            Column(
              children: _additionalSymptoms.keys.map((s) => CheckboxListTile(
                title: Text(s),
                value: _additionalSymptoms[s],
                onChanged: (val) => setState(() => _additionalSymptoms[s] = val!),
                contentPadding: EdgeInsets.zero,
                controlAffinity: ListTileControlAffinity.leading,
              )).toList(),
            ),

            const SizedBox(height: 20),
            OutlinedButton.icon(
              onPressed: () {
                ScaffoldMessenger.of(context).showSnackBar(
                  const SnackBar(content: Text("Photo attachment not supported yet.")),
                );
              },
              icon: const Icon(Icons.camera_alt),
              label: const Text("Add Photo"),
              style: OutlinedButton.styleFrom(
                padding: const EdgeInsets.all(15),
              ),
            ),
            const SizedBox(height: 30),
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
                  : const Text("SUBMIT SYMPTOMS"),
            ),
          ],
        ),
      ),
    );
  }
}
