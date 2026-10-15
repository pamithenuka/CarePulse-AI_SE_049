import 'package:flutter/material.dart';
import 'package:geolocator/geolocator.dart';
import 'package:provider/provider.dart';

import '../features/dispatch/services/location_service.dart';
import '../providers/patient_provider.dart';
import '../services/api_exception.dart';
import '../services/triage_api_service.dart';
import '../widgets/triage_widgets.dart';
import 'triage_history_screen.dart';
import 'triage_status_screen.dart';

class SymptomIntakeScreen extends StatefulWidget {
  final TriageApiService? apiService;
  final LocationService? locationService;
  final bool isActive;

  const SymptomIntakeScreen({
    super.key,
    this.apiService,
    this.locationService,
    this.isActive = true,
  });

  @override
  State<SymptomIntakeScreen> createState() => _SymptomIntakeScreenState();
}

class _SymptomIntakeScreenState extends State<SymptomIntakeScreen> {
  final _symptomsController = TextEditingController();
  final _descriptionForm = GlobalKey<FormState>();
  final _scrollController = ScrollController();
  late final _apiService = widget.apiService ?? TriageApiService();
  int _step = 0;
  bool _isLoading = false;
  bool _locating = false;
  bool _uncertainSubmission = false;
  String? _error;
  Map<String, dynamic>? _submittedResult;
  String _selectedDuration = 'Since this morning';
  String _selectedSeverity = 'Mild';

  static const _symptomExamples = {
    'Headache': 'I have a headache.',
    'Fever': 'I have a fever.',
    'Stomach pain': 'I have stomach pain.',
    'Cough': 'I have a cough.',
    'Dizziness': 'I feel dizzy.',
  };

  String _normalizeExample(String text) =>
      text.toLowerCase().replaceAll(RegExp(r'[\s.!?,;:]+'), ' ').trim();

  bool _hasExample(String phrase) =>
      _normalizeExample(_symptomsController.text)
          .contains(_normalizeExample(phrase));

  String _withExample(String phrase) {
    final text = _symptomsController.text;
    final separator = text.isEmpty || RegExp(r'\s$').hasMatch(text) ? '' : ' ';
    return '$text$separator$phrase';
  }

  void _addExample(String phrase) {
    if (_formLocked || _hasExample(phrase)) return;
    final text = _withExample(phrase);
    // Never truncate the patient's text or insert a partial example.
    if (text.length > 1000) return;
    _symptomsController.value = TextEditingValue(
      text: text,
      selection: TextSelection.collapsed(offset: text.length),
    );
  }

  // Duration is free text in the DTO. Preserve the existing today value;
  // the new ranges are sent as meaningful strings, not inaccurate mappings.
  static const _durations = {
    'Since this morning': 'Started today',
    '1–3 days': '1–3 days',
    '4–7 days': '4–7 days',
    'More than a week': 'More than a week',
    'Not sure': 'Not sure',
  };
  static const _severityDescriptions = {
    'Mild': 'I can continue normal activities.',
    'Moderate': 'Some activities are difficult.',
    'Severe': 'Significant symptoms affect my normal activities.',
  };
  final Map<String, bool> _additionalSymptoms = {
    'Dizziness': false,
    'Fever': false,
    'Chest pain': false,
    'Difficulty breathing': false,
    'Vomiting': false,
  };
  Position? _position;

  bool get _formLocked => _isLoading || _submittedResult != null;

  void _startNewAssessment() {
    setState(() {
      _symptomsController.clear();
      _selectedDuration = 'Since this morning';
      _selectedSeverity = 'Mild';
      _additionalSymptoms.updateAll((key, value) => false);
      _position = null;
      _submittedResult = null;
      _uncertainSubmission = false;
      _error = null;
    });
    _goToStep(0);
  }

  List<String> get _selectedAdditionalSymptoms => _additionalSymptoms.entries
      .where((entry) => entry.value)
      .map((entry) => entry.key)
      .toList();

  void _goToStep(int step) {
    FocusScope.of(context).unfocus();
    setState(() => _step = step);
    if (_scrollController.hasClients) _scrollController.jumpTo(0);
  }

  Future<void> _captureLocation() async {
    if (_locating || _formLocked) return;
    setState(() => _locating = true);
    try {
      final location = widget.locationService ?? LocationService();
      if (!await location.requestPermission()) {
        throw Exception(
          'Enable location permission, or ask staff to confirm your destination.',
        );
      }
      final position = await location.getCurrentPosition();
      if (mounted) setState(() => _position = position);
    } catch (error) {
      if (mounted) {
        ScaffoldMessenger.of(context)
            .showSnackBar(SnackBar(content: Text(error.toString())));
      }
    } finally {
      if (mounted) setState(() => _locating = false);
    }
  }

  @override
  void dispose() {
    _symptomsController.dispose();
    _scrollController.dispose();
    super.dispose();
  }

  void _openHistory() => Navigator.of(context).push(
    MaterialPageRoute(
      builder: (_) => TriageHistoryScreen(apiService: _apiService),
    ),
  );

  void _openResult(Map<String, dynamic> result) => Navigator.of(context).push(
    MaterialPageRoute(
      builder: (_) =>
          TriageStatusScreen(triageData: result, apiService: _apiService),
    ),
  );

  Future<void> _submitTriage() async {
    if (_isLoading ||
        _uncertainSubmission ||
        _submittedResult != null ||
        _locating) {
      return;
    }
    final symptoms = _symptomsController.text.trim();
    if (symptoms.length < 5 || symptoms.length > 1000) {
      _goToStep(0);
      return;
    }
    final patient = context.read<PatientProvider>().profile;
    if (patient == null) {
      setState(
        () => _error =
            'Complete your patient profile before requesting an assessment.',
      );
      return;
    }
    setState(() {
      _isLoading = true;
      _error = null;
    });
    try {
      final result = await _apiService.submitTriage(
        patientProfileId: patient.id,
        symptoms: symptoms,
        duration: _selectedDuration,
        severity: _selectedSeverity,
        additionalSymptoms: _selectedAdditionalSymptoms,
        hasPhotoAttachment: false,
        latitude: _position?.latitude,
        longitude: _position?.longitude,
      );
      if (!mounted) return;
      setState(() => _submittedResult = result);
      _openResult(result);
    } catch (error) {
      if (!mounted) return;
      // A failure may follow a saved ticket. Never resubmit automatically.
      final uncertain =
          error is! ApiException ||
          error.statusCode == 408 ||
          error.statusCode >= 500;
      setState(() {
        _uncertainSubmission = uncertain;
        _error = uncertain
            ? 'We could not confirm the outcome. Your case may already be saved. Check your triage history before submitting again.'
            : error.message;
      });
    } finally {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  Widget _descriptionStep() => Form(
    key: _descriptionForm,
    child: Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Text(
          'How are you feeling today?',
          style: Theme.of(context).textTheme.headlineSmall,
        ),
        const SizedBox(height: 8),
        const Text(
          'Tell us what is bothering you and when it started. Your own words are enough.',
        ),
        const SizedBox(height: 16),
        TextFormField(
          controller: _symptomsController,
          enabled: !_formLocked,
          minLines: 3,
          maxLines: 5,
          maxLength: 1000,
          textCapitalization: TextCapitalization.sentences,
          scrollPadding: const EdgeInsets.all(24),
          style: Theme.of(context).textTheme.bodyLarge,
          decoration: const InputDecoration(
            labelText: 'Your symptoms',
            floatingLabelBehavior: FloatingLabelBehavior.always,
            alignLabelWithHint: true,
            hintText: 'What symptoms have you noticed?',
            counterText: '',
            contentPadding: EdgeInsets.all(16),
            errorMaxLines: 3,
          ),
          validator: (value) {
            final text = value?.trim() ?? '';
            if (text.length < 5) {
              return 'Please describe your symptoms (at least 5 characters).';
            }
            if (text.length > 1000) return 'Use no more than 1,000 characters.';
            return null;
          },
        ),
        const SizedBox(height: 8),
        ValueListenableBuilder<TextEditingValue>(
          valueListenable: _symptomsController,
          builder: (context, value, child) {
            final privacyHint = Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Icon(
                  Icons.lock_outline,
                  size: 16,
                  color: Theme.of(context).colorScheme.onSurfaceVariant,
                ),
                const SizedBox(width: 6),
                Expanded(
                  child: Text(
                    'Leave out names and contact details.',
                    style: Theme.of(context).textTheme.bodySmall,
                  ),
                ),
              ],
            );
            final counter = Semantics(
              label: '${value.text.length} of 1,000 characters used',
              child: ExcludeSemantics(
                child: Text(
                  '${value.text.length} / 1,000',
                  style: Theme.of(context).textTheme.bodySmall,
                ),
              ),
            );
            if (MediaQuery.textScalerOf(context).scale(12.5) > 18) {
              return Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  privacyHint,
                  const SizedBox(height: 4),
                  Align(alignment: Alignment.centerRight, child: counter),
                ],
              );
            }
            return Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Expanded(child: privacyHint),
                const SizedBox(width: 12),
                counter,
              ],
            );
          },
        ),
        const SizedBox(height: 16),
        Text(
          'Try an example (optional)',
          style: Theme.of(context).textTheme.labelLarge,
        ),
        const SizedBox(height: 4),
        ValueListenableBuilder<TextEditingValue>(
          valueListenable: _symptomsController,
          builder: (context, value, child) => Wrap(
            spacing: 8,
            runSpacing: 0,
            children: _symptomExamples.entries.map((entry) {
              final alreadyAdded = _hasExample(entry.value);
              final fits = _withExample(entry.value).length <= 1000;
              return ActionChip(
                label: Text(entry.key),
                avatar: alreadyAdded ? const Icon(Icons.check, size: 16) : null,
                tooltip: alreadyAdded
                    ? 'Example already included'
                    : !fits
                    ? 'Not enough space for this example'
                    : 'Add: ${entry.value}',
                onPressed: _formLocked || alreadyAdded || !fits
                    ? null
                    : () => _addExample(entry.value),
                materialTapTargetSize: MaterialTapTargetSize.padded,
              );
            }).toList(),
          ),
        ),
        const SizedBox(height: 16),
        DecoratedBox(
          decoration: BoxDecoration(
            color: Theme.of(context).colorScheme.surfaceContainerLow,
            borderRadius: BorderRadius.circular(12),
          ),
          child: Padding(
            padding: const EdgeInsets.all(12),
            child: Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Icon(
                  Icons.info_outline,
                  size: 20,
                  color: Theme.of(context).colorScheme.primary,
                ),
                const SizedBox(width: 10),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        'How this works',
                        style: Theme.of(context).textTheme.labelLarge,
                      ),
                      const SizedBox(height: 4),
                      Text(
                        'Answer a few questions and review your details. The AI assessment suggests next steps, but is not a diagnosis.',
                        style: Theme.of(context).textTheme.bodySmall,
                      ),
                    ],
                  ),
                ),
              ],
            ),
          ),
        ),
      ],
    ),
  );

  Widget _detailsStep() => Column(
    crossAxisAlignment: CrossAxisAlignment.stretch,
    children: [
      Text(
        'A Few More Details',
        style: Theme.of(context).textTheme.headlineSmall,
      ),
      const SizedBox(height: 8),
      const Text('These details help put your symptoms in context.'),
      const SizedBox(height: 20),
      TriageSection(
        title: 'How long have you felt this way?',
        child: DropdownButtonFormField<String>(
          initialValue: _selectedDuration,
          isExpanded: true,
          decoration: const InputDecoration(labelText: 'Symptom duration'),
          items: _durations.entries
              .map(
                (entry) => DropdownMenuItem(
                  value: entry.key,
                  child: Text(entry.value),
                ),
              )
              .toList(),
          onChanged: (value) => setState(() => _selectedDuration = value!),
        ),
      ),
      const SizedBox(height: 16),
      TriageSection(
        title: 'How much are symptoms affecting you?',
        child: Column(
          children: _severityDescriptions.entries
              .map(
                (entry) => Padding(
                  padding: const EdgeInsets.only(bottom: 8),
                  child: Semantics(
                    selected: _selectedSeverity == entry.key,
                    child: OutlinedButton(
                      style: OutlinedButton.styleFrom(
                        backgroundColor: _selectedSeverity == entry.key
                            ? Theme.of(context).colorScheme.primaryContainer
                            : null,
                        padding: const EdgeInsets.all(14),
                      ),
                      onPressed: () =>
                          setState(() => _selectedSeverity = entry.key),
                      child: Row(
                        children: [
                          Icon(
                            _selectedSeverity == entry.key
                                ? Icons.radio_button_checked
                                : Icons.radio_button_off,
                          ),
                          const SizedBox(width: 12),
                          Expanded(
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text(
                                  entry.key,
                                  style: const TextStyle(
                                    fontWeight: FontWeight.w600,
                                  ),
                                ),
                                Text(entry.value),
                              ],
                            ),
                          ),
                        ],
                      ),
                    ),
                  ),
                ),
              )
              .toList(),
        ),
      ),
      const SizedBox(height: 16),
      TriageSection(
        title: 'Anything else? (optional)',
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text(
              'Select any additional symptoms. There is no need to repeat what you have already described.',
            ),
            const SizedBox(height: 12),
            Wrap(
              spacing: 8,
              runSpacing: 8,
              children: _additionalSymptoms.entries
                  .map(
                    (entry) => FilterChip(
                      label: Text(entry.key),
                      selected: entry.value,
                      onSelected: (value) => setState(
                        () => _additionalSymptoms[entry.key] = value,
                      ),
                    ),
                  )
                  .toList(),
            ),
          ],
        ),
      ),
    ],
  );

  Widget _reviewSection(String title, String value, int step) => Padding(
    padding: const EdgeInsets.only(bottom: 16),
    child: TriageSection(
      title: title,
      trailing: IconButton(
        tooltip: 'Edit $title',
        onPressed: _formLocked ? null : () => _goToStep(step),
        icon: const Icon(Icons.edit_outlined),
      ),
      child: Text(value),
    ),
  );

  Widget _reviewStep() => Column(
    crossAxisAlignment: CrossAxisAlignment.stretch,
    children: [
      Text(
        'Review and Submit',
        style: Theme.of(context).textTheme.headlineSmall,
      ),
      const SizedBox(height: 8),
      const Text('Check your information before requesting an assessment.'),
      const SizedBox(height: 20),
      _reviewSection('Symptoms', _symptomsController.text.trim(), 0),
      _reviewSection('Duration', _durations[_selectedDuration]!, 1),
      _reviewSection(
        'Severity',
        '$_selectedSeverity — ${_severityDescriptions[_selectedSeverity]}',
        1,
      ),
      _reviewSection(
        'Additional symptoms',
        _selectedAdditionalSymptoms.isEmpty
            ? 'None selected'
            : _selectedAdditionalSymptoms.join(', '),
        1,
      ),
      TriageSection(
        title: 'Location sharing (optional)',
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            const Text(
              'Share your current location with this case to help staff confirm a destination if a nurse visit is later authorized. Sharing does not request a dispatch.',
            ),
            const SizedBox(height: 12),
            OutlinedButton.icon(
              onPressed: _formLocked || _locating ? null : _captureLocation,
              icon: const Icon(Icons.my_location),
              label: Text(
                _locating
                    ? 'Getting your location…'
                    : _position == null
                    ? 'Share current location'
                    : 'Update shared location',
              ),
            ),
            if (_position != null) ...[
              const Text(
                'Your location will be included with this submission.',
              ),
              TextButton(
                onPressed: _formLocked
                    ? null
                    : () => setState(() => _position = null),
                child: const Text('Remove shared location'),
              ),
            ],
          ],
        ),
      ),
      const SizedBox(height: 16),
      const Text(
        'This AI assessment does not replace professional medical advice.',
      ),
      if (_error != null) ...[
        const SizedBox(height: 16),
        Semantics(
          liveRegion: true,
          child: Text(
            _error!,
            style: TextStyle(color: Theme.of(context).colorScheme.error),
          ),
        ),
      ],
      if (_uncertainSubmission) ...[
        const SizedBox(height: 12),
        OutlinedButton.icon(
          onPressed: _openHistory,
          icon: const Icon(Icons.history),
          label: const Text('Check triage history'),
        ),
        TextButton(
          onPressed: () => setState(() {
            _uncertainSubmission = false;
            _error = null;
          }),
          child: const Text('I checked my history — no case was saved'),
        ),
      ],
      const SizedBox(height: 20),
      FilledButton(
        onPressed: _isLoading || _locating || _uncertainSubmission
            ? null
            : _submittedResult != null
            ? () => _openResult(_submittedResult!)
            : _submitTriage,
        child: _isLoading
            ? const Row(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  SizedBox(
                    width: 20,
                    height: 20,
                    child: CircularProgressIndicator(strokeWidth: 2),
                  ),
                  SizedBox(width: 12),
                  Flexible(child: Text('Assessing your symptoms…')),
                ],
              )
            : Text(
                _submittedResult != null
                    ? 'View your assessment'
                    : _error != null && !_uncertainSubmission
                    ? 'Try assessment again'
                    : 'Assess My Symptoms',
              ),
      ),
      if (_submittedResult != null) ...[
        const Padding(
          padding: EdgeInsets.only(top: 12),
          child: Text(
            'This form has already been submitted. You can view it again or start a new assessment.',
          ),
        ),
        TextButton(
          onPressed: _startNewAssessment,
          child: const Text('Start a new assessment'),
        ),
      ],
    ],
  );

  @override
  Widget build(BuildContext context) => PopScope(
    canPop: !widget.isActive || (_step == 0 && !_isLoading),
    onPopInvokedWithResult: (didPop, result) {
      if (widget.isActive && !didPop && !_formLocked && _step > 0) {
        _goToStep(_step - 1);
      }
    },
    child: Scaffold(
      appBar: AppBar(
        title: const Text('Symptom assessment'),
        leading: _step > 0
            ? IconButton(
                tooltip: 'Previous step',
                onPressed: _formLocked ? null : () => _goToStep(_step - 1),
                icon: const Icon(Icons.arrow_back),
              )
            : null,
        actions: [
          IconButton(
            tooltip: 'My triage history',
            onPressed: _isLoading ? null : _openHistory,
            icon: const Icon(Icons.history),
          ),
          const TriageSosAction(),
        ],
      ),
      body: SafeArea(
        child: SingleChildScrollView(
          controller: _scrollController,
          keyboardDismissBehavior: ScrollViewKeyboardDismissBehavior.onDrag,
          padding: const EdgeInsets.all(20),
          child: Center(
            child: ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 640),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  Text(
                    'Step ${_step + 1} of 3',
                    style: Theme.of(context).textTheme.labelLarge,
                  ),
                  const SizedBox(height: 8),
                  TriageStepIndicator(currentStep: _step),
                  const SizedBox(height: 20),
                  if (_step == 0) _descriptionStep(),
                  if (_step == 1) _detailsStep(),
                  if (_step == 2) _reviewStep(),
                  if (_step < 2) ...[
                    const SizedBox(height: 24),
                    FilledButton(
                      style: _step == 0
                          ? FilledButton.styleFrom(
                              minimumSize: const Size(48, 52),
                              padding: const EdgeInsets.symmetric(
                                horizontal: 20,
                                vertical: 14,
                              ),
                            )
                          : null,
                      onPressed: () {
                        if (_step == 0 &&
                            !_descriptionForm.currentState!.validate()) {
                          return;
                        }
                        _goToStep(_step + 1);
                      },
                      child: _step == 0
                          ? const Row(
                              children: [
                                Expanded(
                                  child: Text(
                                    'Continue',
                                    textAlign: TextAlign.center,
                                  ),
                                ),
                                Icon(Icons.arrow_forward, size: 20),
                              ],
                            )
                          : const Text('Review my symptoms'),
                    ),
                  ],
                  if (_step > 0)
                    TextButton(
                      onPressed: _formLocked
                          ? null
                          : () => _goToStep(_step - 1),
                      child: const Text('Back'),
                    ),
                ],
              ),
            ),
          ),
        ),
      ),
    ),
  );
}
