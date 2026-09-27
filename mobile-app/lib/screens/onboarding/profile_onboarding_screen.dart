import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../models/emergency_contact.dart';
import '../../providers/patient_provider.dart';
import '../../utils/validators.dart';

/// One-time form a Patient completes right after registering, mirroring
/// backend-api's CreatePatientProfileDto. Requires at least one emergency
/// contact, same as the web app's Admin onboarding form.
class ProfileOnboardingScreen extends StatefulWidget {
  const ProfileOnboardingScreen({super.key});

  @override
  State<ProfileOnboardingScreen> createState() => _ProfileOnboardingScreenState();
}

class _ContactFormRow {
  final nameController = TextEditingController();
  final relationshipController = TextEditingController();
  final phoneController = TextEditingController();
  bool isPrimary;

  _ContactFormRow({this.isPrimary = false});

  void dispose() {
    nameController.dispose();
    relationshipController.dispose();
    phoneController.dispose();
  }
}

class _ProfileOnboardingScreenState extends State<ProfileOnboardingScreen> {
  final _formKey = GlobalKey<FormState>();
  final _fullNameController = TextEditingController();
  final _phoneController = TextEditingController();
  final _addressController = TextEditingController();
  final _nationalIdController = TextEditingController();
  final _allergiesController = TextEditingController();

  DateTime? _dateOfBirth;
  String _gender = 'Female';
  String? _bloodType;
  String? _error;
  bool _isSaving = false;

  final List<_ContactFormRow> _contacts = [_ContactFormRow(isPrimary: true)];

  static const _genders = ['Female', 'Male', 'Other'];
  static const _bloodTypes = ['A+', 'A-', 'B+', 'B-', 'AB+', 'AB-', 'O+', 'O-'];

  @override
  void dispose() {
    _fullNameController.dispose();
    _phoneController.dispose();
    _addressController.dispose();
    _nationalIdController.dispose();
    _allergiesController.dispose();
    for (final c in _contacts) {
      c.dispose();
    }
    super.dispose();
  }

  Future<void> _pickDateOfBirth() async {
    final now = DateTime.now();
    final picked = await showDatePicker(
      context: context,
      initialDate: DateTime(now.year - 30, now.month, now.day),
      firstDate: DateTime(1900),
      lastDate: now,
    );
    if (picked != null) setState(() => _dateOfBirth = picked);
  }

  void _addContactRow() => setState(() => _contacts.add(_ContactFormRow()));

  void _removeContactRow(int index) => setState(() {
        _contacts[index].dispose();
        _contacts.removeAt(index);
      });

  Future<void> _submit() async {
    final dobError = Validators.dateOfBirth(_dateOfBirth);
    if (dobError != null) {
      setState(() => _error = dobError);
      return;
    }
    if (!_formKey.currentState!.validate()) return;

    setState(() {
      _error = null;
      _isSaving = true;
    });

    final contacts = _contacts
        .map((c) => EmergencyContact(
              fullName: c.nameController.text.trim(),
              relationshipToPatient: c.relationshipController.text.trim(),
              phoneNumber: c.phoneController.text.trim(),
              isPrimary: c.isPrimary,
            ))
        .toList();

    final patientProvider = context.read<PatientProvider>();
    final ok = await patientProvider.createProfile(
      fullName: _fullNameController.text.trim(),
      dateOfBirth: _dateOfBirth!,
      gender: _gender,
      bloodType: _bloodType,
      phoneNumber: _phoneController.text.trim(),
      address: _addressController.text.trim().isEmpty ? null : _addressController.text.trim(),
      nationalId: _nationalIdController.text.trim(),
      allergies: _allergiesController.text.trim().isEmpty ? null : _allergiesController.text.trim(),
      emergencyContacts: contacts,
    );

    if (!mounted) return;
    setState(() {
      _isSaving = false;
      if (!ok) _error = patientProvider.error ?? 'Failed to create your profile.';
    });
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Complete your profile'), automaticallyImplyLeading: false),
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(16),
          child: Form(
            key: _formKey,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Text('Tell us about yourself', style: Theme.of(context).textTheme.titleMedium),
                const SizedBox(height: 4),
                Text(
                  'This information helps clinicians treat you quickly in an emergency.',
                  style: Theme.of(context).textTheme.bodySmall,
                ),
                const SizedBox(height: 16),
                TextFormField(
                  controller: _fullNameController,
                  decoration: const InputDecoration(labelText: 'Full name'),
                  validator: (v) => Validators.requiredField(v, label: 'Full name'),
                ),
                const SizedBox(height: 16),
                InkWell(
                  onTap: _pickDateOfBirth,
                  child: InputDecorator(
                    decoration: const InputDecoration(labelText: 'Date of birth'),
                    child: Text(_dateOfBirth == null
                        ? 'Tap to select'
                        : '${_dateOfBirth!.year}-${_dateOfBirth!.month.toString().padLeft(2, '0')}-${_dateOfBirth!.day.toString().padLeft(2, '0')}'),
                  ),
                ),
                const SizedBox(height: 16),
                DropdownButtonFormField<String>(
                  initialValue: _gender,
                  decoration: const InputDecoration(labelText: 'Gender'),
                  items: _genders.map((g) => DropdownMenuItem(value: g, child: Text(g))).toList(),
                  onChanged: (v) => setState(() => _gender = v ?? _gender),
                ),
                const SizedBox(height: 16),
                DropdownButtonFormField<String>(
                  initialValue: _bloodType,
                  decoration: const InputDecoration(labelText: 'Blood type (optional)'),
                  items: _bloodTypes.map((b) => DropdownMenuItem(value: b, child: Text(b))).toList(),
                  onChanged: (v) => setState(() => _bloodType = v),
                ),
                const SizedBox(height: 16),
                TextFormField(
                  controller: _phoneController,
                  keyboardType: TextInputType.phone,
                  decoration: const InputDecoration(labelText: 'Phone number', hintText: '07XXXXXXXX'),
                  validator: Validators.phoneNumber,
                ),
                const SizedBox(height: 16),
                TextFormField(
                  controller: _addressController,
                  decoration: const InputDecoration(labelText: 'Address (optional)'),
                ),
                const SizedBox(height: 16),
                TextFormField(
                  controller: _nationalIdController,
                  decoration: const InputDecoration(labelText: 'National ID', hintText: '9 digits + V/X, or 12 digits'),
                  validator: Validators.nationalId,
                ),
                const SizedBox(height: 16),
                TextFormField(
                  controller: _allergiesController,
                  decoration: const InputDecoration(labelText: 'Known allergies (optional)'),
                  maxLines: 2,
                ),
                const SizedBox(height: 24),
                Row(
                  children: [
                    Expanded(child: Text('Emergency contacts', style: Theme.of(context).textTheme.titleMedium)),
                    IconButton(icon: const Icon(Icons.add_circle_outline), onPressed: _addContactRow),
                  ],
                ),
                ..._contacts.asMap().entries.map((entry) => _buildContactCard(entry.key, entry.value)),
                if (_error != null) ...[
                  const SizedBox(height: 8),
                  Text(_error!, style: TextStyle(color: Theme.of(context).colorScheme.error)),
                ],
                const SizedBox(height: 24),
                FilledButton(
                  onPressed: _isSaving ? null : _submit,
                  child: _isSaving
                      ? const SizedBox(height: 20, width: 20, child: CircularProgressIndicator(strokeWidth: 2))
                      : const Text('Save and continue'),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }

  Widget _buildContactCard(int index, _ContactFormRow contact) {
    return Card(
      margin: const EdgeInsets.symmetric(vertical: 8),
      child: Padding(
        padding: const EdgeInsets.all(12),
        child: Column(
          children: [
            Row(
              children: [
                Expanded(child: Text('Contact ${index + 1}', style: Theme.of(context).textTheme.labelLarge)),
                if (_contacts.length > 1)
                  IconButton(icon: const Icon(Icons.remove_circle_outline), onPressed: () => _removeContactRow(index)),
              ],
            ),
            TextFormField(
              controller: contact.nameController,
              decoration: const InputDecoration(labelText: 'Full name'),
              validator: (v) => Validators.requiredField(v, label: 'Contact name'),
            ),
            const SizedBox(height: 12),
            TextFormField(
              controller: contact.relationshipController,
              decoration: const InputDecoration(labelText: 'Relationship'),
              validator: (v) => Validators.requiredField(v, label: 'Relationship'),
            ),
            const SizedBox(height: 12),
            TextFormField(
              controller: contact.phoneController,
              keyboardType: TextInputType.phone,
              decoration: const InputDecoration(labelText: 'Phone number', hintText: '07XXXXXXXX'),
              validator: Validators.phoneNumber,
            ),
            CheckboxListTile(
              contentPadding: EdgeInsets.zero,
              title: const Text('Primary contact'),
              value: contact.isPrimary,
              onChanged: (v) => setState(() => contact.isPrimary = v ?? false),
            ),
          ],
        ),
      ),
    );
  }
}
