import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../providers/patient_provider.dart';
import '../../widgets/loading_view.dart';

/// A single, unmissable action: broadcast the patient's critical info (blood
/// type, allergies, chronic conditions) to their emergency contacts, mirroring
/// the web app's "Trigger Emergency Alert" action on the patient detail page.
class SosScreen extends StatefulWidget {
  const SosScreen({super.key});

  @override
  State<SosScreen> createState() => _SosScreenState();
}

class _SosScreenState extends State<SosScreen> {
  bool _isSending = false;
  Map<String, dynamic>? _result;
  String? _error;

  Future<void> _confirmAndSend() async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (dialogContext) => AlertDialog(
        title: const Text('Send emergency alert?'),
        content: const Text(
          'This will notify all of your emergency contacts with your critical medical information (blood type, allergies, chronic conditions).',
        ),
        actions: [
          TextButton(onPressed: () => Navigator.of(dialogContext).pop(false), child: const Text('Cancel')),
          FilledButton(
            style: FilledButton.styleFrom(backgroundColor: Theme.of(dialogContext).colorScheme.error),
            onPressed: () => Navigator.of(dialogContext).pop(true),
            child: const Text('Send SOS'),
          ),
        ],
      ),
    );
    if (confirmed != true || !mounted) return;

    setState(() {
      _isSending = true;
      _error = null;
    });

    final provider = context.read<PatientProvider>();
    final result = await provider.triggerEmergencyAlert();

    if (!mounted) return;
    setState(() {
      _isSending = false;
      if (result != null) {
        _result = result;
      } else {
        _error = provider.error ?? 'Failed to send the alert.';
      }
    });
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Emergency SOS')),
      body: Center(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: _result != null ? _buildResult(context) : _buildPrompt(context),
        ),
      ),
    );
  }

  Widget _buildPrompt(BuildContext context) {
    return Column(
      mainAxisSize: MainAxisSize.min,
      children: [
        Icon(Icons.sos, size: 96, color: Theme.of(context).colorScheme.error),
        const SizedBox(height: 16),
        Text(
          'Press the button below to alert your emergency contacts immediately.',
          textAlign: TextAlign.center,
          style: Theme.of(context).textTheme.titleMedium,
        ),
        const SizedBox(height: 32),
        if (_isSending)
          const LoadingView(message: 'Sending alert...')
        else
          SizedBox(
            width: double.infinity,
            height: 64,
            child: FilledButton(
              style: FilledButton.styleFrom(backgroundColor: Theme.of(context).colorScheme.error, textStyle: const TextStyle(fontSize: 20)),
              onPressed: _confirmAndSend,
              child: const Text('SOS'),
            ),
          ),
        if (_error != null) ...[
          const SizedBox(height: 16),
          Text(_error!, style: TextStyle(color: Theme.of(context).colorScheme.error), textAlign: TextAlign.center),
        ],
      ],
    );
  }

  // NotificationDeliveryStatus is serialized as its integer ordinal
  // (Sent=0, Failed=1) - no JsonStringEnumConverter is registered server-side.
  String _deliveryStatusLabel(dynamic rawValue) {
    if (rawValue is String) return rawValue;
    if (rawValue == 0) return 'Sent';
    if (rawValue == 1) return 'Failed';
    return 'Unknown';
  }

  Widget _buildResult(BuildContext context) {
    final notified = (_result!['notifiedContacts'] as List<dynamic>? ?? []);
    return Column(
      mainAxisSize: MainAxisSize.min,
      children: [
        Icon(Icons.check_circle, size: 72, color: Colors.green),
        const SizedBox(height: 16),
        Text('Alert sent', style: Theme.of(context).textTheme.headlineSmall),
        const SizedBox(height: 8),
        Text('${notified.length} contact(s) were notified.', style: Theme.of(context).textTheme.bodyMedium),
        const SizedBox(height: 24),
        ...notified.map((c) => ListTile(
              leading: const Icon(Icons.person_outline),
              title: Text(c['contactName']?.toString() ?? 'Contact'),
              subtitle: Text(c['phoneNumber']?.toString() ?? ''),
              trailing: Text(_deliveryStatusLabel(c['deliveryStatus'])),
            )),
        const SizedBox(height: 16),
        OutlinedButton(onPressed: () => Navigator.of(context).pop(), child: const Text('Done')),
      ],
    );
  }
}
