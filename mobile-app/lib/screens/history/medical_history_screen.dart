import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../models/medical_history.dart';
import '../../providers/patient_provider.dart';
import '../../utils/validators.dart';
import '../../widgets/empty_view.dart';

class MedicalHistoryScreen extends StatelessWidget {
  const MedicalHistoryScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final patient = context.watch<PatientProvider>();
    final histories = [...(patient.profile?.medicalHistories ?? [])]
      ..sort((a, b) => b.diagnosedOn.compareTo(a.diagnosedOn));

    return Scaffold(
      appBar: AppBar(title: const Text('Medical History')),
      body: RefreshIndicator(
        onRefresh: () => context.read<PatientProvider>().loadMyProfile(),
        child: histories.isEmpty
            ? ListView(
                children: const [
                  SizedBox(height: 120),
                  EmptyView(message: 'No medical history recorded yet.', icon: Icons.medical_information_outlined),
                ],
              )
            : ListView.builder(
                padding: const EdgeInsets.all(16),
                itemCount: histories.length,
                itemBuilder: (context, index) => _HistoryCard(history: histories[index]),
              ),
      ),
      floatingActionButton: FloatingActionButton(
        onPressed: () => showModalBottomSheet(
          context: context,
          isScrollControlled: true,
          builder: (_) => const _AddHistorySheet(),
        ),
        child: const Icon(Icons.add),
      ),
    );
  }
}

class _HistoryCard extends StatelessWidget {
  final MedicalHistory history;

  const _HistoryCard({required this.history});

  @override
  Widget build(BuildContext context) {
    return Card(
      margin: const EdgeInsets.only(bottom: 12),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Expanded(child: Text(history.conditionName, style: Theme.of(context).textTheme.titleMedium)),
                if (history.isChronic) const Chip(label: Text('Chronic'), visualDensity: VisualDensity.compact),
                if (history.isResolved) const Padding(padding: EdgeInsets.only(left: 6), child: Icon(Icons.check_circle, color: Colors.green, size: 20)),
              ],
            ),
            Text(
              'Diagnosed ${history.diagnosedOn.year}-${history.diagnosedOn.month.toString().padLeft(2, '0')}-${history.diagnosedOn.day.toString().padLeft(2, '0')}',
              style: Theme.of(context).textTheme.bodySmall,
            ),
            if (history.notes?.isNotEmpty == true) ...[
              const SizedBox(height: 8),
              Text(history.notes!),
            ],
            if (history.currentMedications?.isNotEmpty == true) ...[
              const SizedBox(height: 8),
              Text('Medications: ${history.currentMedications}', style: Theme.of(context).textTheme.bodySmall),
            ],
          ],
        ),
      ),
    );
  }
}

class _AddHistorySheet extends StatefulWidget {
  const _AddHistorySheet();

  @override
  State<_AddHistorySheet> createState() => _AddHistorySheetState();
}

class _AddHistorySheetState extends State<_AddHistorySheet> {
  final _formKey = GlobalKey<FormState>();
  final _conditionController = TextEditingController();
  final _notesController = TextEditingController();
  final _medicationsController = TextEditingController();
  DateTime _diagnosedOn = DateTime.now();
  bool _isChronic = false;
  bool _isSaving = false;
  String? _error;

  @override
  void dispose() {
    _conditionController.dispose();
    _notesController.dispose();
    _medicationsController.dispose();
    super.dispose();
  }

  Future<void> _pickDate() async {
    final picked = await showDatePicker(
      context: context,
      initialDate: _diagnosedOn,
      firstDate: DateTime(1900),
      lastDate: DateTime.now(),
    );
    if (picked != null) setState(() => _diagnosedOn = picked);
  }

  Future<void> _save() async {
    if (!_formKey.currentState!.validate()) return;
    setState(() {
      _isSaving = true;
      _error = null;
    });
    final ok = await context.read<PatientProvider>().addMedicalHistory(
          conditionName: _conditionController.text.trim(),
          notes: _notesController.text.trim().isEmpty ? null : _notesController.text.trim(),
          diagnosedOn: _diagnosedOn,
          isChronic: _isChronic,
          currentMedications: _medicationsController.text.trim().isEmpty ? null : _medicationsController.text.trim(),
        );
    if (!mounted) return;
    if (ok) {
      Navigator.of(context).pop();
    } else {
      setState(() {
        _isSaving = false;
        _error = context.read<PatientProvider>().error ?? 'Failed to save.';
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: EdgeInsets.only(left: 16, right: 16, top: 16, bottom: MediaQuery.of(context).viewInsets.bottom + 16),
      child: Form(
        key: _formKey,
        child: SingleChildScrollView(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Text('Report a condition', style: Theme.of(context).textTheme.titleMedium),
              const SizedBox(height: 4),
              Text('Self-reported entries are visible to your care team.', style: Theme.of(context).textTheme.bodySmall),
              const SizedBox(height: 16),
              TextFormField(
                controller: _conditionController,
                decoration: const InputDecoration(labelText: 'Condition'),
                validator: (v) => Validators.requiredField(v, label: 'Condition'),
              ),
              const SizedBox(height: 12),
              InkWell(
                onTap: _pickDate,
                child: InputDecorator(
                  decoration: const InputDecoration(labelText: 'Diagnosed on'),
                  child: Text('${_diagnosedOn.year}-${_diagnosedOn.month.toString().padLeft(2, '0')}-${_diagnosedOn.day.toString().padLeft(2, '0')}'),
                ),
              ),
              const SizedBox(height: 12),
              TextFormField(
                controller: _medicationsController,
                decoration: const InputDecoration(labelText: 'Current medications (optional)'),
              ),
              const SizedBox(height: 12),
              TextFormField(
                controller: _notesController,
                decoration: const InputDecoration(labelText: 'Notes (optional)'),
                maxLines: 2,
              ),
              CheckboxListTile(
                contentPadding: EdgeInsets.zero,
                title: const Text('Ongoing / chronic condition'),
                value: _isChronic,
                onChanged: (v) => setState(() => _isChronic = v ?? false),
              ),
              if (_error != null) ...[
                const SizedBox(height: 8),
                Text(_error!, style: TextStyle(color: Theme.of(context).colorScheme.error)),
              ],
              const SizedBox(height: 16),
              FilledButton(
                onPressed: _isSaving ? null : _save,
                child: _isSaving
                    ? const SizedBox(height: 20, width: 20, child: CircularProgressIndicator(strokeWidth: 2))
                    : const Text('Save'),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
