import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:provider/provider.dart';
import '../../../providers/auth_provider.dart';
import '../../../services/api_exception.dart';
import '../services/api_service.dart';

class VitalsEntryScreen extends StatefulWidget {
  final Map<String, dynamic> dispatch;

  const VitalsEntryScreen({super.key, required this.dispatch});

  @override
  State<VitalsEntryScreen> createState() => _VitalsEntryScreenState();
}

class _VitalsEntryScreenState extends State<VitalsEntryScreen> {
  final _formKey = GlobalKey<FormState>();
  final _apiService = ApiService();
  bool _isSubmitting = false;
  bool _needsLogin = false;
  final _draftStorage = const FlutterSecureStorage();
  String get _draftKey => 'vitals_${context.read<AuthProvider>().currentUser?.userId}_${widget.dispatch['id']}';
  List<TextEditingController> get _fields => [_heartRateController, _systolicController, _diastolicController, _tempController, _oxygenController, _notesController];

  @override
  void initState() {
    super.initState();
    _restoreDraft();
  }

  Future<void> _restoreDraft() async {
    try {
      final key = _draftKey;
      final raw = await _draftStorage.read(key: key);
      if (raw == null || !mounted) return;
      final draft = jsonDecode(raw) as Map<String, dynamic>;
      if (DateTime.parse(draft['expiresAt']).isBefore(DateTime.now())) {
        await _draftStorage.delete(key: key);
        return;
      }
      final values = draft['values'] as List;
      if (values.length != _fields.length) return;
      for (var i = 0; i < _fields.length; i++) {
        if (_fields[i].text.isEmpty) _fields[i].text = values[i] as String;
      }
    } catch (_) { /* An unavailable draft never fabricates measurements. */ }
  }


  final _heartRateController = TextEditingController();
  final _systolicController = TextEditingController();
  final _diastolicController = TextEditingController();
  final _tempController = TextEditingController();
  final _oxygenController = TextEditingController();
  final _notesController = TextEditingController();

  Future<void> _submitVitals() async {
    if (!_formKey.currentState!.validate()) return;

    setState(() => _isSubmitting = true);
    try {
      final key = _draftKey;
      await _draftStorage.write(key: key, value: jsonEncode({
        'expiresAt': DateTime.now().add(const Duration(hours: 24)).toIso8601String(),
        'values': _fields.map((field) => field.text).toList(),
      }));
      await _apiService.completeOnsite(
        widget.dispatch['id'],
        {
          'heartRate': int.parse(_heartRateController.text),
          'bloodPressure': '${_systolicController.text}/${_diastolicController.text}',
          'bodyTempC': double.parse(_tempController.text),
          'oxygenSaturation': int.parse(_oxygenController.text),
          'clinicalNotes': _notesController.text,
        },
      );
      await _draftStorage.delete(key: key);
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Vitals recorded successfully!'), backgroundColor: Color(0xFF22C55E)),
        );
        Navigator.pushNamedAndRemoveUntil(context, '/dispatch-dashboard', (route) => false);
      }
    } catch (e) {
      if (mounted) {
        setState(() => _needsLogin = e is ApiException && e.statusCode == 401);
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Error: ${e.toString()}'), backgroundColor: Colors.redAccent),
        );
      }
    } finally {
      if (mounted) setState(() => _isSubmitting = false);
    }
  }

  void _escalateEmergency() {
    showDialog(
      context: context,
      builder: (ctx) => AlertDialog(
        backgroundColor: const Color(0xFF1E293B),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
        title: const Row(
          children: [
            Icon(Icons.warning_amber_rounded, color: Color(0xFFEF4444), size: 28),
            SizedBox(width: 10),
            Text('Escalate Emergency', style: TextStyle(color: Colors.white)),
          ],
        ),
        content: const Text(
          'Record an escalation on the staff dashboard? Contact your supervising doctor directly for an immediate response.',
          style: TextStyle(color: Colors.white70),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx),
            child: const Text('Cancel', style: TextStyle(color: Colors.white54)),
          ),
          ElevatedButton(
            onPressed: () async {
              Navigator.pop(ctx);
              try {
                await _apiService.escalate(widget.dispatch['id'], _notesController.text.trim().length >= 3 ? _notesController.text.trim() : 'Nurse requests urgent doctor review.');
                if (mounted) ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Escalation recorded. Contact your supervisor for an immediate response.')));
              } catch (e) {
                if (mounted) ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(e.toString())));
              }
            },
            style: ElevatedButton.styleFrom(
              backgroundColor: const Color(0xFFEF4444),
              foregroundColor: Colors.white,
              shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
            ),
            child: const Text('Escalate Now'),
          ),
        ],
      ),
    );
  }

  @override
  void dispose() {
    _heartRateController.dispose();
    _systolicController.dispose();
    _diastolicController.dispose();
    _tempController.dispose();
    _oxygenController.dispose();
    _notesController.dispose();
    super.dispose();
  }

  Widget _buildField({
    required String label,
    required String hint,
    required TextEditingController controller,
    required String? Function(String?) validator,
    TextInputType keyboardType = TextInputType.number,
    String? suffix,
    int maxLines = 1,
  }) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(label, style: const TextStyle(color: Color(0xFF475569), fontSize: 13, fontWeight: FontWeight.w500)),
          const SizedBox(height: 6),
          TextFormField(
            controller: controller,
            keyboardType: keyboardType,
            maxLines: maxLines,
            style: const TextStyle(color: Color(0xFF0F172A)),
            validator: validator,
            decoration: InputDecoration(
              hintText: hint,
              hintStyle: const TextStyle(color: Color(0xFF94A3B8)),
              suffixText: suffix,
              suffixStyle: const TextStyle(color: Color(0xFF64748B)),
              filled: true,
              fillColor: Colors.white,
              border: OutlineInputBorder(
                borderRadius: BorderRadius.circular(12),
                borderSide: const BorderSide(color: Color(0xFFE2E8F0)),
              ),
              enabledBorder: OutlineInputBorder(
                borderRadius: BorderRadius.circular(12),
                borderSide: const BorderSide(color: Color(0xFFE2E8F0)),
              ),
              focusedBorder: OutlineInputBorder(
                borderRadius: BorderRadius.circular(12),
                borderSide: const BorderSide(color: Color(0xFF0F766E), width: 2),
              ),
              errorBorder: OutlineInputBorder(
                borderRadius: BorderRadius.circular(12),
                borderSide: const BorderSide(color: Color(0xFFEF4444)),
              ),
            ),
          ),
        ],
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFFF4F6F8),
      appBar: AppBar(
        backgroundColor: const Color(0xFF0B5F6B),
        elevation: 0,
        leading: IconButton(
          icon: const Icon(Icons.arrow_back_ios, color: Colors.white),
          onPressed: () => Navigator.pop(context),
        ),
        title: const Text(
          'On-Site Vitals',
          style: TextStyle(color: Colors.white, fontWeight: FontWeight.w700),
        ),
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(24),
        child: Form(
          key: _formKey,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              if (_needsLogin) TextButton(
                onPressed: () => Navigator.pushNamed(context, '/login'),
                child: const Text('Sign in again. Your attempted submission is saved for 24 hours.'),
              ),
              // Section Header
              Container(
                padding: const EdgeInsets.all(16),
                decoration: BoxDecoration(
                  color: Colors.white,
                  borderRadius: BorderRadius.circular(14),
                  border: Border.all(color: const Color(0xFFE2E8F0)),
                  boxShadow: const [
                    BoxShadow(color: Color(0x0C000000), blurRadius: 4, offset: Offset(0, 2)),
                  ],
                ),
                child: const Row(
                  children: [
                    Icon(Icons.monitor_heart, color: Color(0xFFEF4444), size: 28),
                    SizedBox(width: 12),
                    Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text('Patient Vitals Recording', style: TextStyle(color: Color(0xFF1E293B), fontWeight: FontWeight.w600, fontSize: 16)),
                        SizedBox(height: 2),
                        Text('Enter all measurements carefully', style: TextStyle(color: Color(0xFF64748B), fontSize: 13)),
                      ],
                    ),
                  ],
                ),
              ),
              const SizedBox(height: 24),

              // Vitals Fields
              _buildField(
                label: 'Heart Rate',
                hint: 'e.g. 72',
                controller: _heartRateController,
                suffix: 'bpm',
                validator: (v) {
                  if (v == null || v.isEmpty) return 'Required';
                  final n = int.tryParse(v);
                  if (n == null || n < 0 || n > 350) return 'Enter a valid number';
                  return null;
                },
              ),
              Row(
                children: [
                  Expanded(
                    child: _buildField(
                      label: 'Systolic BP',
                      hint: 'e.g. 120',
                      controller: _systolicController,
                      suffix: 'mmHg',
                      validator: (v) {
                        if (v == null || v.isEmpty) return 'Required';
                        final n = int.tryParse(v);
                        if (n == null || n < 0 || n > 350) return 'Invalid';
                        return null;
                      },
                    ),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: _buildField(
                      label: 'Diastolic BP',
                      hint: 'e.g. 80',
                      controller: _diastolicController,
                      suffix: 'mmHg',
                      validator: (v) {
                        if (v == null || v.isEmpty) return 'Required';
                        final n = int.tryParse(v);
                        if (n == null || n < 0 || n > 350) return 'Invalid';
                        if (n > (int.tryParse(_systolicController.text) ?? 350)) return 'Exceeds systolic';
                        return null;
                      },
                    ),
                  ),
                ],
              ),
              _buildField(
                label: 'Body Temperature',
                hint: 'e.g. 37.0',
                controller: _tempController,
                suffix: '°C',
                validator: (v) {
                  if (v == null || v.isEmpty) return 'Required';
                  final n = double.tryParse(v);
                  if (n == null || !n.isFinite || n < 30 || n > 45) return '30-45°C range';
                  return null;
                },
              ),
              _buildField(
                label: 'Oxygen Saturation',
                hint: 'e.g. 98',
                controller: _oxygenController,
                suffix: '%',
                validator: (v) {
                  if (v == null || v.isEmpty) return 'Required';
                  final n = int.tryParse(v);
                  if (n == null || n < 0 || n > 100) return '0-100% range';
                  return null;
                },
              ),
              _buildField(
                label: 'Clinical Notes',
                hint: 'Describe the patient condition...',
                controller: _notesController,
                keyboardType: TextInputType.multiline,
                maxLines: 4,
                validator: (v) => (v == null || v.trim().isEmpty) ? 'Notes are required' : v.length > 2000 ? 'Maximum 2000 characters' : null,
              ),

              const SizedBox(height: 8),

              // Submit Button
              SizedBox(
                width: double.infinity,
                height: 52,
                child: ElevatedButton.icon(
                  onPressed: _isSubmitting ? null : _submitVitals,
                  icon: _isSubmitting
                      ? const SizedBox(width: 20, height: 20, child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white))
                      : const Icon(Icons.check_circle),
                  label: Text(
                    _isSubmitting ? 'Submitting...' : 'Complete & Submit Vitals',
                    style: const TextStyle(fontSize: 16, fontWeight: FontWeight.w600),
                  ),
                  style: ElevatedButton.styleFrom(
                    backgroundColor: const Color(0xFF0F766E),
                    foregroundColor: Colors.white,
                    shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(14)),
                    elevation: 0,
                  ),
                ),
              ),
              const SizedBox(height: 16),

              // Escalation Button
              SizedBox(
                width: double.infinity,
                height: 52,
                child: OutlinedButton.icon(
                  onPressed: _escalateEmergency,
                  icon: const Icon(Icons.warning_amber_rounded),
                  label: const Text(
                    'Escalate Emergency',
                    style: TextStyle(fontSize: 16, fontWeight: FontWeight.w600),
                  ),
                  style: OutlinedButton.styleFrom(
                    foregroundColor: const Color(0xFFEF4444),
                    side: const BorderSide(color: Color(0xFFEF4444)),
                    shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(14)),
                    backgroundColor: Colors.white,
                  ),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
