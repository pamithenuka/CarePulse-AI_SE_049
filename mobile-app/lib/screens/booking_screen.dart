import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import '../api/api_client.dart';
import '../models/doctor.dart';
import '../models/slot.dart';
import '../theme/app_theme.dart';

// Placeholder until Student 1's patient module (login/profile) exists.
const _demoPatientId = '11111111-1111-1111-1111-111111111111';

class BookingScreen extends StatefulWidget {
  final Doctor doctor;
  const BookingScreen({super.key, required this.doctor});

  @override
  State<BookingScreen> createState() => _BookingScreenState();
}

class _BookingScreenState extends State<BookingScreen> {
  final _api = ApiClient();

  DateTime _selectedDate = DateTime.now();
  List<AppointmentSlot> _slots = [];
  bool _loading = true;
  String? _error;
  String? _bookingSlotId;

  @override
  void initState() {
    super.initState();
    _loadSlots();
  }

  Future<void> _loadSlots() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final slots = await _api.getSlots(
        doctorId: widget.doctor.id,
        date: _selectedDate,
      );
      setState(() => _slots = slots);
    } catch (e) {
      setState(() => _error = e.toString());
    } finally {
      setState(() => _loading = false);
    }
  }

  bool _findingNext = false;

  Future<void> _findNextAvailableDate() async {
    setState(() => _findingNext = true);
    try {
      // No date filter this time - ask for every upcoming open slot for
      // this doctor, already sorted soonest-first by the backend.
      final upcoming = await _api.getSlots(doctorId: widget.doctor.id);

      if (upcoming.isEmpty) {
        if (!mounted) return;
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('No upcoming open slots found for this doctor yet.')),
        );
        return;
      }

      final nextDate = upcoming.first.slotStart;
      setState(() {
        _selectedDate = DateTime(nextDate.year, nextDate.month, nextDate.day);
      });
      await _loadSlots();
    } catch (e) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text(e.toString())),
      );
    } finally {
      if (mounted) setState(() => _findingNext = false);
    }
  }

  // Device feature: the native calendar date picker.
  Future<void> _pickDate() async {
    final picked = await showDatePicker(
      context: context,
      initialDate: _selectedDate,
      firstDate: DateTime.now(),
      lastDate: DateTime.now().add(const Duration(days: 90)),
    );
    if (picked != null) {
      setState(() => _selectedDate = picked);
      _loadSlots();
    }
  }

  Future<void> _bookSlot(AppointmentSlot slot) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Confirm appointment'),
        content: Text(
          '${widget.doctor.fullName}\n'
          '${DateFormat('EEEE, MMM d').format(slot.slotStart)}\n'
          '${DateFormat.jm().format(slot.slotStart)} – ${DateFormat.jm().format(slot.slotEnd)}',
        ),
        actions: [
          TextButton(onPressed: () => Navigator.pop(context, false), child: const Text('Cancel')),
          ElevatedButton(onPressed: () => Navigator.pop(context, true), child: const Text('Confirm')),
        ],
      ),
    );
    if (confirmed != true) return;

    setState(() => _bookingSlotId = slot.slotId);
    try {
      await _api.bookAppointment(slotId: slot.slotId, patientId: _demoPatientId);
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Appointment booked!'), backgroundColor: AppColors.open),
      );
      _loadSlots();
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
      appBar: AppBar(title: Text(widget.doctor.fullName)),
      body: Column(
        children: [
          _buildDoctorHeader(context),
          _buildDatePickerRow(context),
          const Divider(height: 1, color: AppColors.border),
          Expanded(child: _buildSlotList(context)),
        ],
      ),
    );
  }

  Widget _buildDoctorHeader(BuildContext context) {
    return Container(
      width: double.infinity,
      color: AppColors.primaryDark,
      padding: const EdgeInsets.fromLTRB(20, 4, 20, 16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            widget.doctor.specialty,
            style: const TextStyle(color: Color(0xFFCFE1DC), fontSize: 13.5, fontWeight: FontWeight.w500),
          ),
          const SizedBox(height: 4),
          const Text(
            'Pick a date below to see available appointment times.',
            style: TextStyle(color: Color(0xFFA9C4BD), fontSize: 12.5),
          ),
        ],
      ),
    );
  }

  Widget _buildDatePickerRow(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.all(16),
      child: OutlinedButton.icon(
        onPressed: _pickDate,
        icon: const Icon(Icons.calendar_today_rounded, size: 18),
        label: Text(DateFormat('EEEE, MMMM d, yyyy').format(_selectedDate)),
        style: OutlinedButton.styleFrom(
          minimumSize: const Size.fromHeight(48),
          side: const BorderSide(color: AppColors.border),
          foregroundColor: AppColors.primaryDark,
        ),
      ),
    );
  }

  Widget _buildSlotList(BuildContext context) {
    if (_loading) {
      return const Center(child: CircularProgressIndicator());
    }

    if (_error != null) {
      return Center(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Text(_error!, textAlign: TextAlign.center),
        ),
      );
    }

    if (_slots.isEmpty) {
      return Center(
        child: Padding(
          padding: const EdgeInsets.all(32),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Icon(Icons.event_busy_rounded, size: 40, color: AppColors.muted),
              const SizedBox(height: 14),
              Text(
                'No open times on this date',
                style: Theme.of(context).textTheme.titleMedium,
                textAlign: TextAlign.center,
              ),
              const SizedBox(height: 6),
              Text(
                'Try picking a different date, or jump ahead to the next week.',
                style: Theme.of(context).textTheme.bodySmall,
                textAlign: TextAlign.center,
              ),
              const SizedBox(height: 16),
              OutlinedButton.icon(
                onPressed: _findingNext ? null : _findNextAvailableDate,
                icon: _findingNext
                    ? const SizedBox(
                        width: 14,
                        height: 14,
                        child: CircularProgressIndicator(strokeWidth: 2),
                      )
                    : const Icon(Icons.search_rounded, size: 16),
                label: Text(_findingNext ? 'Searching…' : 'Find next available date'),
                style: OutlinedButton.styleFrom(
                  side: const BorderSide(color: AppColors.border),
                  foregroundColor: AppColors.primaryDark,
                ),
              ),
            ],
          ),
        ),
      );
    }

    return ListView.separated(
      padding: const EdgeInsets.fromLTRB(16, 4, 16, 24),
      itemCount: _slots.length + 1,
      separatorBuilder: (_, __) => const SizedBox(height: 8),
      itemBuilder: (context, i) {
        if (i == 0) {
          return Padding(
            padding: const EdgeInsets.only(bottom: 4),
            child: Text(
              'Available times',
              style: Theme.of(context).textTheme.titleMedium,
            ),
          );
        }
        final slot = _slots[i - 1];
        final isBooking = _bookingSlotId == slot.slotId;
        return Card(
          child: ListTile(
            title: Text(
              '${DateFormat.jm().format(slot.slotStart)} – ${DateFormat.jm().format(slot.slotEnd)}',
              style: Theme.of(context).textTheme.titleMedium,
            ),
            trailing: ElevatedButton(
              onPressed: isBooking ? null : () => _bookSlot(slot),
              child: Text(isBooking ? 'Booking…' : 'Book'),
            ),
          ),
        );
      },
    );
  }
}
