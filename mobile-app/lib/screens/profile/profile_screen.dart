import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../providers/auth_provider.dart';
import '../../providers/patient_provider.dart';
import '../../utils/validators.dart';
import '../../widgets/error_view.dart';
import '../../widgets/loading_view.dart';

class ProfileScreen extends StatelessWidget {
  const ProfileScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final patient = context.watch<PatientProvider>();
    final profile = patient.profile;

    return Scaffold(
      appBar: AppBar(
        title: const Text('My Profile'),
        actions: [
          IconButton(
            icon: const Icon(Icons.logout),
            tooltip: 'Log out',
            onPressed: () => context.read<AuthProvider>().logout(),
          ),
        ],
      ),
      body: RefreshIndicator(
        onRefresh: () => context.read<PatientProvider>().loadMyProfile(),
        child: profile == null
            ? (patient.isLoading
                ? const LoadingView()
                : ErrorView(message: patient.error ?? 'Profile unavailable.', onRetry: () => context.read<PatientProvider>().loadMyProfile()))
            : ListView(
                padding: const EdgeInsets.all(16),
                children: [
                  _ProfileHeader(fullName: profile.fullName, age: profile.age, gender: profile.gender),
                  const SizedBox(height: 16),
                  Card(
                    child: Padding(
                      padding: const EdgeInsets.all(16),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          _InfoRow(label: 'Blood type', value: profile.bloodType ?? 'Unknown'),
                          _InfoRow(label: 'National ID', value: profile.nationalId),
                          _InfoRow(
                            label: 'Date of birth',
                            value:
                                '${profile.dateOfBirth.year}-${profile.dateOfBirth.month.toString().padLeft(2, '0')}-${profile.dateOfBirth.day.toString().padLeft(2, '0')}',
                          ),
                          _InfoRow(label: 'Allergies', value: profile.allergies?.isNotEmpty == true ? profile.allergies! : 'None recorded'),
                          _InfoRow(label: 'Status', value: profile.status),
                        ],
                      ),
                    ),
                  ),
                  const SizedBox(height: 16),
                  Card(
                    child: Padding(
                      padding: const EdgeInsets.all(16),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Row(
                            children: [
                              Expanded(child: Text('Contact details', style: Theme.of(context).textTheme.titleMedium)),
                              TextButton.icon(
                                icon: const Icon(Icons.edit_outlined, size: 18),
                                label: const Text('Edit'),
                                onPressed: () => _showEditSheet(context, profile.phoneNumber, profile.address),
                              ),
                            ],
                          ),
                          _InfoRow(label: 'Phone', value: profile.phoneNumber),
                          _InfoRow(label: 'Address', value: profile.address?.isNotEmpty == true ? profile.address! : 'Not provided'),
                        ],
                      ),
                    ),
                  ),
                ],
              ),
      ),
    );
  }

  void _showEditSheet(BuildContext context, String currentPhone, String? currentAddress) {
    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      builder: (_) => _EditContactSheet(currentPhone: currentPhone, currentAddress: currentAddress),
    );
  }
}

class _ProfileHeader extends StatelessWidget {
  final String fullName;
  final int age;
  final String gender;

  const _ProfileHeader({required this.fullName, required this.age, required this.gender});

  @override
  Widget build(BuildContext context) {
    return Row(
      children: [
        CircleAvatar(
          radius: 32,
          backgroundColor: Theme.of(context).colorScheme.primaryContainer,
          child: Text(fullName.isNotEmpty ? fullName[0].toUpperCase() : '?', style: const TextStyle(fontSize: 24)),
        ),
        const SizedBox(width: 16),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(fullName, style: Theme.of(context).textTheme.titleLarge),
              Text('$age years old, $gender', style: Theme.of(context).textTheme.bodyMedium),
            ],
          ),
        ),
      ],
    );
  }
}

class _InfoRow extends StatelessWidget {
  final String label;
  final String value;

  const _InfoRow({required this.label, required this.value});

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 6),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          SizedBox(width: 120, child: Text(label, style: Theme.of(context).textTheme.bodySmall)),
          Expanded(child: Text(value, style: Theme.of(context).textTheme.bodyMedium)),
        ],
      ),
    );
  }
}

class _EditContactSheet extends StatefulWidget {
  final String currentPhone;
  final String? currentAddress;

  const _EditContactSheet({required this.currentPhone, required this.currentAddress});

  @override
  State<_EditContactSheet> createState() => _EditContactSheetState();
}

class _EditContactSheetState extends State<_EditContactSheet> {
  final _formKey = GlobalKey<FormState>();
  late final _phoneController = TextEditingController(text: widget.currentPhone);
  late final _addressController = TextEditingController(text: widget.currentAddress ?? '');
  bool _isSaving = false;
  String? _error;

  @override
  void dispose() {
    _phoneController.dispose();
    _addressController.dispose();
    super.dispose();
  }

  Future<void> _save() async {
    if (!_formKey.currentState!.validate()) return;
    setState(() {
      _isSaving = true;
      _error = null;
    });
    final ok = await context.read<PatientProvider>().updateProfile(
          phoneNumber: _phoneController.text.trim(),
          address: _addressController.text.trim(),
        );
    if (!mounted) return;
    if (ok) {
      Navigator.of(context).pop();
    } else {
      setState(() {
        _isSaving = false;
        _error = context.read<PatientProvider>().error ?? 'Failed to update.';
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: EdgeInsets.only(
        left: 16,
        right: 16,
        top: 16,
        bottom: MediaQuery.of(context).viewInsets.bottom + 16,
      ),
      child: Form(
        key: _formKey,
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text('Update contact details', style: Theme.of(context).textTheme.titleMedium),
            const SizedBox(height: 16),
            TextFormField(
              controller: _phoneController,
              keyboardType: TextInputType.phone,
              decoration: const InputDecoration(labelText: 'Phone number'),
              validator: Validators.phoneNumber,
            ),
            const SizedBox(height: 12),
            TextFormField(
              controller: _addressController,
              decoration: const InputDecoration(labelText: 'Address'),
            ),
            if (_error != null) ...[
              const SizedBox(height: 12),
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
    );
  }
}
