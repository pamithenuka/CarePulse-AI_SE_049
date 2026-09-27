import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../models/emergency_contact.dart';
import '../../providers/patient_provider.dart';
import '../../utils/validators.dart';
import '../../widgets/empty_view.dart';

class EmergencyContactsScreen extends StatelessWidget {
  const EmergencyContactsScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final patient = context.watch<PatientProvider>();
    final contacts = patient.profile?.emergencyContacts ?? [];

    return Scaffold(
      appBar: AppBar(title: const Text('Emergency Contacts')),
      body: RefreshIndicator(
        onRefresh: () => context.read<PatientProvider>().loadMyProfile(),
        child: contacts.isEmpty
            ? ListView(
                children: const [
                  SizedBox(height: 120),
                  EmptyView(message: 'No emergency contacts yet.', icon: Icons.contacts_outlined),
                ],
              )
            : ListView.builder(
                padding: const EdgeInsets.all(16),
                itemCount: contacts.length,
                itemBuilder: (context, index) => _ContactCard(contact: contacts[index], canDelete: contacts.length > 1),
              ),
      ),
      floatingActionButton: FloatingActionButton(
        onPressed: () => showModalBottomSheet(
          context: context,
          isScrollControlled: true,
          builder: (_) => const _ContactFormSheet(),
        ),
        child: const Icon(Icons.person_add_alt),
      ),
    );
  }
}

class _ContactCard extends StatelessWidget {
  final EmergencyContact contact;
  final bool canDelete;

  const _ContactCard({required this.contact, required this.canDelete});

  Future<void> _confirmDelete(BuildContext context) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (dialogContext) => AlertDialog(
        title: const Text('Remove contact'),
        content: Text('Remove ${contact.fullName} from your emergency contacts?'),
        actions: [
          TextButton(onPressed: () => Navigator.of(dialogContext).pop(false), child: const Text('Cancel')),
          TextButton(onPressed: () => Navigator.of(dialogContext).pop(true), child: const Text('Remove')),
        ],
      ),
    );
    if (confirmed == true && context.mounted) {
      final provider = context.read<PatientProvider>();
      final ok = await provider.deleteEmergencyContact(contact.id!);
      if (!ok && context.mounted) {
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(provider.error ?? 'Failed to remove contact.')));
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Card(
      margin: const EdgeInsets.only(bottom: 12),
      child: ListTile(
        leading: CircleAvatar(child: Text(contact.fullName.isNotEmpty ? contact.fullName[0].toUpperCase() : '?')),
        title: Text(contact.fullName),
        subtitle: Text('${contact.relationshipToPatient} • ${contact.phoneNumber}'),
        trailing: Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            if (contact.isPrimary) const Padding(padding: EdgeInsets.only(right: 4), child: Icon(Icons.star, color: Colors.amber, size: 20)),
            IconButton(
              icon: const Icon(Icons.edit_outlined),
              onPressed: () => showModalBottomSheet(
                context: context,
                isScrollControlled: true,
                builder: (_) => _ContactFormSheet(existing: contact),
              ),
            ),
            IconButton(
              icon: const Icon(Icons.delete_outline),
              onPressed: canDelete ? () => _confirmDelete(context) : null,
              tooltip: canDelete ? null : 'At least one contact is required',
            ),
          ],
        ),
      ),
    );
  }
}

class _ContactFormSheet extends StatefulWidget {
  final EmergencyContact? existing;

  const _ContactFormSheet({this.existing});

  @override
  State<_ContactFormSheet> createState() => _ContactFormSheetState();
}

class _ContactFormSheetState extends State<_ContactFormSheet> {
  final _formKey = GlobalKey<FormState>();
  late final _nameController = TextEditingController(text: widget.existing?.fullName ?? '');
  late final _relationshipController = TextEditingController(text: widget.existing?.relationshipToPatient ?? '');
  late final _phoneController = TextEditingController(text: widget.existing?.phoneNumber ?? '');
  late bool _isPrimary = widget.existing?.isPrimary ?? false;
  bool _isSaving = false;
  String? _error;

  @override
  void dispose() {
    _nameController.dispose();
    _relationshipController.dispose();
    _phoneController.dispose();
    super.dispose();
  }

  Future<void> _save() async {
    if (!_formKey.currentState!.validate()) return;
    setState(() {
      _isSaving = true;
      _error = null;
    });

    final contact = EmergencyContact(
      fullName: _nameController.text.trim(),
      relationshipToPatient: _relationshipController.text.trim(),
      phoneNumber: _phoneController.text.trim(),
      isPrimary: _isPrimary,
    );

    final provider = context.read<PatientProvider>();
    final ok = widget.existing == null
        ? await provider.addEmergencyContact(contact)
        : await provider.updateEmergencyContact(widget.existing!.id!, contact);

    if (!mounted) return;
    if (ok) {
      Navigator.of(context).pop();
    } else {
      setState(() {
        _isSaving = false;
        _error = provider.error ?? 'Failed to save.';
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: EdgeInsets.only(left: 16, right: 16, top: 16, bottom: MediaQuery.of(context).viewInsets.bottom + 16),
      child: Form(
        key: _formKey,
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text(widget.existing == null ? 'Add contact' : 'Edit contact', style: Theme.of(context).textTheme.titleMedium),
            const SizedBox(height: 16),
            TextFormField(
              controller: _nameController,
              decoration: const InputDecoration(labelText: 'Full name'),
              validator: (v) => Validators.requiredField(v, label: 'Full name'),
            ),
            const SizedBox(height: 12),
            TextFormField(
              controller: _relationshipController,
              decoration: const InputDecoration(labelText: 'Relationship'),
              validator: (v) => Validators.requiredField(v, label: 'Relationship'),
            ),
            const SizedBox(height: 12),
            TextFormField(
              controller: _phoneController,
              keyboardType: TextInputType.phone,
              decoration: const InputDecoration(labelText: 'Phone number', hintText: '07XXXXXXXX'),
              validator: Validators.phoneNumber,
            ),
            CheckboxListTile(
              contentPadding: EdgeInsets.zero,
              title: const Text('Primary contact'),
              value: _isPrimary,
              onChanged: (v) => setState(() => _isPrimary = v ?? false),
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
    );
  }
}
