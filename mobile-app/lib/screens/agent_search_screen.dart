import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import '../api/api_client.dart';
import '../models/agent_response.dart';
import '../models/slot.dart';
import '../theme/app_theme.dart';

// Placeholder until Student 1's patient module (login/profile) exists.
const _demoPatientId = '11111111-1111-1111-1111-111111111111';

class AgentSearchScreen extends StatefulWidget {
  const AgentSearchScreen({super.key});

  @override
  State<AgentSearchScreen> createState() => _AgentSearchScreenState();
}

class _AgentSearchScreenState extends State<AgentSearchScreen> {
  final _api = ApiClient();
  final _controller = TextEditingController();

  bool _loading = false;
  String? _error;
  AgentSearchResponse? _result;
  String? _bookingSlotId;

  Future<void> _submit() async {
    final message = _controller.text.trim();
    if (message.isEmpty) return;

    setState(() {
      _loading = true;
      _error = null;
      _result = null;
    });

    try {
      final response = await _api.searchAgent(message);
      setState(() => _result = response);
    } catch (e) {
      setState(() => _error = e.toString());
    } finally {
      setState(() => _loading = false);
    }
  }

  Future<void> _bookSlot(AppointmentSlot slot) async {
    setState(() => _bookingSlotId = slot.slotId);
    try {
      await _api.bookAppointment(slotId: slot.slotId, patientId: _demoPatientId);
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Appointment booked!'), backgroundColor: AppColors.open),
      );
      // Remove the booked slot from the results shown, since it's no
      // longer open.
      setState(() {
        _result = AgentSearchResponse(
          reply: _result!.reply,
          matchingSlots: _result!.matchingSlots.where((s) => s.slotId != slot.slotId).toList(),
        );
      });
    } catch (e) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text(e.toString()), backgroundColor: AppColors.booked),
      );
    } finally {
      if (mounted) setState(() => _bookingSlotId = null);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Ask CarePulse')),
      body: Column(
        children: [
          Expanded(child: _buildBody(context)),
          _buildInputBar(context),
        ],
      ),
    );
  }

  Widget _buildBody(BuildContext context) {
    if (_loading) {
      return const Center(child: CircularProgressIndicator());
    }

    if (_error != null) {
      return Padding(
        padding: const EdgeInsets.all(24),
        child: Center(
          child: Text(_error!, textAlign: TextAlign.center, style: const TextStyle(color: AppColors.booked)),
        ),
      );
    }

    if (_result == null) {
      return Padding(
        padding: const EdgeInsets.all(32),
        child: Center(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              const Icon(Icons.forum_outlined, size: 44, color: AppColors.muted),
              const SizedBox(height: 16),
              Text(
                'Ask for an appointment in your own words.',
                style: Theme.of(context).textTheme.titleMedium,
                textAlign: TextAlign.center,
              ),
              const SizedBox(height: 8),
              Text(
                'e.g. "I need to see a cardiologist sometime next week in the morning"',
                style: Theme.of(context).textTheme.bodySmall,
                textAlign: TextAlign.center,
              ),
            ],
          ),
        ),
      );
    }

    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        Card(
          child: Padding(
            padding: const EdgeInsets.all(16),
            child: Text(
              // Strip basic markdown bold markers (**) since this is a
              // plain Text widget, not a markdown renderer.
              _result!.reply.replaceAll('**', ''),
              style: Theme.of(context).textTheme.bodyMedium,
            ),
          ),
        ),
        if (_result!.matchingSlots.isNotEmpty) ...[
          const SizedBox(height: 16),
          Text('Matching appointments', style: Theme.of(context).textTheme.titleMedium),
          const SizedBox(height: 10),
          ..._result!.matchingSlots.map((slot) {
            final isBooking = _bookingSlotId == slot.slotId;
            return Padding(
              padding: const EdgeInsets.only(bottom: 8),
              child: Card(
                child: ListTile(
                  title: Text(slot.doctorName, style: Theme.of(context).textTheme.titleMedium),
                  subtitle: Text(
                    '${slot.specialty} · ${DateFormat('EEE, MMM d').format(slot.slotStart)}, '
                    '${DateFormat.jm().format(slot.slotStart)}',
                  ),
                  trailing: ElevatedButton(
                    onPressed: isBooking ? null : () => _bookSlot(slot),
                    child: Text(isBooking ? 'Booking…' : 'Book'),
                  ),
                ),
              ),
            );
          }),
        ],
      ],
    );
  }

  Widget _buildInputBar(BuildContext context) {
    return SafeArea(
      child: Container(
        padding: const EdgeInsets.all(12),
        decoration: const BoxDecoration(
          color: AppColors.surface,
          border: Border(top: BorderSide(color: AppColors.border)),
        ),
        child: Row(
          children: [
            Expanded(
              child: TextField(
                controller: _controller,
                minLines: 1,
                maxLines: 3,
                textInputAction: TextInputAction.send,
                onSubmitted: (_) => _submit(),
                decoration: const InputDecoration(
                  hintText: 'Ask about an appointment…',
                ),
              ),
            ),
            const SizedBox(width: 8),
            IconButton.filled(
              onPressed: _loading ? null : _submit,
              icon: const Icon(Icons.send_rounded),
            ),
          ],
        ),
      ),
    );
  }
}
