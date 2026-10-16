import 'dart:async';

import 'package:flutter/material.dart';

import '../services/api_exception.dart';
import '../services/triage_api_service.dart';
import '../theme/app_theme.dart';
import '../widgets/triage_widgets.dart';
import 'doctor_search_screen.dart';
import 'triage_history_screen.dart';
import '../widgets/app_card.dart';

class TriageStatusScreen extends StatefulWidget {
  final Map<String, dynamic> triageData;
  final TriageApiService? apiService;
  const TriageStatusScreen({
    super.key,
    required this.triageData,
    this.apiService,
  });

  @override
  State<TriageStatusScreen> createState() => _TriageStatusScreenState();
}

class _TriageStatusScreenState extends State<TriageStatusScreen> {
  late final _api = widget.apiService ?? TriageApiService();
  late final Map<String, dynamic> _data = Map.of(widget.triageData);
  List<dynamic> _logs = [];
  Timer? _timer;
  bool _fetching = false;
  bool _incomplete = false;
  bool _assessmentVerified = false;
  String? _statusError;
  String? _auditError;

  String get _status => _data['status']?.toString() ?? '';
  String get _risk => _data['riskLevel']?.toString() ?? '';
  bool get _approved => [
    'APPROVED_BY_DOCTOR',
    'DISPATCH_ASSIGNED',
    'VISIT_COMPLETED',
  ].contains(_status);
  bool get _rejected => _status == 'REJECTED';
  bool get _needsReview =>
      _incomplete ||
      _risk == 'HIGH' ||
      _data['requiresDoctorApproval'] == true ||
      [
        'NEEDS_DOCTOR_APPROVAL',
        'APPROVED_BY_DOCTOR',
        'REJECTED',
        'DISPATCH_ASSIGNED',
        'VISIT_COMPLETED',
      ].contains(_status);

  bool get _assessmentConfirmed =>
      !_incomplete &&
      ['LOW', 'MEDIUM', 'HIGH'].contains(_risk) &&
      _assessmentVerified &&
      [
        'COMPLETED',
        'DOCTOR_CONSULTATION_RECOMMENDED',
        'NEEDS_DOCTOR_APPROVAL',
        'APPROVED_BY_DOCTOR',
        'REJECTED',
        'DISPATCH_ASSIGNED',
        'VISIT_COMPLETED',
      ].contains(_status) &&
      [
        null,
        '',
        'Completed',
        'AwaitingApproval',
        'AwaitingDispatch',
        'DispatchAssigned',
        'ArrivedOnSite',
        'Escalated',
        'Rejected',
      ].contains(_data['workflowStatus']);

  dynamic _eventValue(Map event, String name) =>
      event[name] ?? event['${name[0].toUpperCase()}${name.substring(1)}'];

  bool get _executionConfirmed {
    final execution = _data['execution'];
    if (execution is! List) return false;
    var plannerCompleted = false;
    var analysisCompleted = false;
    var validationCompleted = false;
    for (final event in execution.whereType<Map>()) {
      final step = _eventValue(event, 'step');
      final result = _eventValue(event, 'result');
      if (step == 'Validation') validationCompleted = true;
      if (result is! Map) continue;
      if (step == 'Planner' &&
          [0, 'PlanCreated'].contains(_eventValue(result, 'status'))) {
        plannerCompleted = true;
      }
      if (step == 'DomainAnalysis' &&
          _eventValue(result, 'assessmentFailed') == false) {
        analysisCompleted = true;
      }
    }
    return plannerCompleted && analysisCompleted && validationCompleted;
  }

  bool get _executionFailed {
    final execution = _data['execution'];
    if (execution is! List) return false;
    for (final event in execution.whereType<Map>()) {
      final step = _eventValue(event, 'step');
      final result = _eventValue(event, 'result');
      if (step == 'SafeFailure') {
        return true;
      }
      if (result is! Map) continue;
      if (step == 'DomainAnalysis' &&
          _eventValue(result, 'assessmentFailed') == true) {
        return true;
      }
      // ExecutionJson stores the planner enum as 0/1/2 (PlanCreated /
      // ValidationFailed / LlmError). These events survive doctor approval.
      if (step == 'Planner' &&
          [
            1,
            2,
            'ValidationFailed',
            'LlmError',
          ].contains(_eventValue(result, 'status'))) {
        return true;
      }
    }
    return false;
  }

  void _readAssessmentState() {
    // A planner/tool failure may require review with a LOW/MEDIUM score.
    // Keep that assessment incomplete through later approval/dispatch updates.
    _incomplete =
        _incomplete ||
        _executionFailed ||
        _data['assessmentFailed'] == true ||
        ['ManualReviewRequired', 'Failed'].contains(_data['workflowStatus']) ||
        (_data['requiresDoctorApproval'] == true &&
            ['LOW', 'MEDIUM'].contains(_risk));
    // Approval replaces the workflow's earlier completion/failure status.
    // Keep verified evidence across partial polls; on a fresh history load,
    // require execution evidence rather than treating approval as AI success.
    final workflow = _data['workflowStatus'];
    if (!_incomplete &&
        _data['assessmentFailed'] == false &&
        (['Completed', 'AwaitingApproval'].contains(workflow) ||
            ((workflow == null || workflow == '') &&
                _data['workflowId'] == null) ||
            _executionConfirmed)) {
      _assessmentVerified = true;
    }
  }

  TriageCareStage _nextCareStage() {
    const id = 'next-care';
    const title = 'Next care step';
    if (_rejected) {
      return const TriageCareStage(
        id: id,
        title: title,
        detail: 'Not authorized. Follow the doctor’s advice for next steps.',
        state: CareStageState.pending,
      );
    }
    if (_status == 'VISIT_COMPLETED' ||
        _data['dispatchStatus'] == 'Completed') {
      return const TriageCareStage(
        id: id,
        title: title,
        detail: 'Your care team recorded the nurse visit as completed.',
        state: CareStageState.completed,
      );
    }
    if (_data['workflowStatus'] == 'Escalated') {
      return const TriageCareStage(
        id: id,
        title: title,
        detail: 'Your nurse visit needs further care team review.',
        state: CareStageState.inProgress,
      );
    }
    final visit = switch (_data['dispatchStatus']) {
      'Assigned' => 'Nurse visit: assigned',
      'EnRoute' => 'Nurse visit: on the way',
      'ArrivedOnSite' => 'Nurse visit: nurse has arrived',
      _ => _status == 'DISPATCH_ASSIGNED' ? 'Nurse visit: assigned' : null,
    };
    if (visit != null) {
      return TriageCareStage(
        id: id,
        title: title,
        detail: visit,
        state: CareStageState.inProgress,
      );
    }
    return TriageCareStage(
      id: id,
      title: title,
      detail: _needsReview
          ? _approved
                ? 'Waiting for your care team. Approval does not confirm a nurse visit.'
                : 'A doctor’s decision is needed before an authorized care step.'
          : _risk == 'LOW' && _assessmentConfirmed
          ? 'Self-care guidance is available. Recovery is not tracked here.'
          : _risk == 'MEDIUM' && _assessmentConfirmed
          ? 'Find a doctor for a consultation. Booking is not tracked here.'
          : 'Your next care step has not been confirmed.',
      state: CareStageState.pending,
    );
  }

  List<TriageCareStage> _careStages() => [
    TriageCareStage(
      id: 'submitted',
      title: 'Symptoms submitted',
      detail: _data['id']?.toString().isNotEmpty == true
          ? 'Your symptoms are saved with this case.'
          : 'Checking your saved case.',
      state: _data['id']?.toString().isNotEmpty == true
          ? CareStageState.completed
          : CareStageState.pending,
    ),
    TriageCareStage(
      id: 'assessment',
      title: _assessmentConfirmed ? 'AI assessment completed' : 'AI assessment',
      detail: _incomplete
          ? _approved || _rejected
                ? 'The automated assessment remains incomplete after doctor review.'
                : 'The automated assessment is incomplete. Manual review is required.'
          : _assessmentConfirmed
          ? 'The saved assessment is available below.'
          : 'Assessment completion has not yet been confirmed.',
      state: _incomplete
          ? CareStageState.failed
          : _assessmentConfirmed
          ? CareStageState.completed
          : _data['workflowStatus'] == 'Running'
          ? CareStageState.inProgress
          : CareStageState.pending,
    ),
    if (_needsReview)
      TriageCareStage(
        id: 'doctor-review',
        title: 'Doctor review',
        detail: _rejected
            ? 'The doctor did not approve this case.'
            : _approved
            ? 'The doctor approved this case.'
            : _status == 'NEEDS_DOCTOR_APPROVAL'
            ? 'Waiting for a doctor’s decision.'
            : 'Review status is not confirmed.',
        state: _rejected
            ? CareStageState.failed
            : _approved
            ? CareStageState.completed
            : _status == 'NEEDS_DOCTOR_APPROVAL'
            ? CareStageState.inProgress
            : CareStageState.pending,
      ),
    _nextCareStage(),
  ];

  @override
  void initState() {
    super.initState();
    _readAssessmentState();
    _refresh();
    _timer = Timer.periodic(const Duration(seconds: 5), (_) {
      if (!['VISIT_COMPLETED', 'REJECTED'].contains(_status)) {
        _refresh();
      }
    });
  }

  @override
  void dispose() {
    _timer?.cancel();
    super.dispose();
  }

  String _failureMessage(Object error, String fallback) =>
      error is ApiException ? error.message : fallback;

  Future<void> _refresh() async {
    if (_fetching) {
      return;
    }
    _fetching = true;
    setState(() {});
    final id = _data['id']?.toString();
    if (id == null || id.isEmpty) {
      setState(() {
        _statusError =
            'This case could not be opened. Check your triage history.';
        _fetching = false;
      });
      return;
    }
    try {
      try {
        final status = await _api.getStatus(id);
        if (status['id'] != null && status['id'].toString() != id) {
          throw StateError('Unexpected case response');
        }
        if (!mounted) {
          return;
        }
        setState(() {
          // Preserve fields absent from partial responses. Present fields,
          // including empty/null values, belong to the authoritative response.
          _data.addAll(status);
          _readAssessmentState();
          _statusError = null;
        });
      } catch (error) {
        if (!mounted) {
          return;
        }
        setState(
          () => _statusError = _failureMessage(
            error,
            'We could not refresh your case. The last saved information is shown.',
          ),
        );
      }
      // Audit failures must not hide a confirmed doctor-status update.
      try {
        final logs = await _api.getAuditLog(id);
        if (!mounted) {
          return;
        }
        setState(() {
          _logs = logs
              .where(
                (log) =>
                    log is Map &&
                    (log['triageTicketId'] == null ||
                        log['triageTicketId'].toString() == id),
              )
              .toList();
          _auditError = null;
        });
      } catch (error) {
        if (!mounted) {
          return;
        }
        setState(
          () => _auditError = _failureMessage(
            error,
            'We could not refresh the history. Previously loaded notes may be out of date.',
          ),
        );
      }
    } finally {
      if (mounted) {
        setState(() => _fetching = false);
      }
    }
  }

  String? _doctorNotes() {
    if (!_approved && !_rejected) {
      return null;
    }
    final decision = _rejected ? 'rejected by doctor' : 'approved by doctor';
    for (final log in _logs.reversed) {
      final message = log['logMessage']?.toString() ?? '';
      if (!message.toLowerCase().contains(decision)) {
        continue;
      }
      final marker = message.toLowerCase().indexOf('notes:');
      if (marker < 0) {
        continue;
      }
      final notes = message.substring(marker + 6).trim();
      if (notes.isNotEmpty) {
        return notes;
      }
    }
    return null;
  }

  String _formatDate(dynamic value) {
    final date = DateTime.tryParse(value?.toString() ?? '')?.toLocal();
    if (date == null) {
      return '';
    }
    final localizations = MaterialLocalizations.of(context);
    return '${localizations.formatMediumDate(date)} · ${localizations.formatTimeOfDay(TimeOfDay.fromDateTime(date))}';
  }

  String _readableLog(String message) {
    var readable = message;
    for (final status in [
      'DOCTOR_CONSULTATION_RECOMMENDED',
      'NEEDS_DOCTOR_APPROVAL',
      'APPROVED_BY_DOCTOR',
      'DISPATCH_ASSIGNED',
      'VISIT_COMPLETED',
      'REJECTED',
      'COMPLETED',
    ]) {
      readable = readable.replaceAll(status, triageStatusLabel(status));
    }
    for (final action in [
      'SELF_CARE_MONITORING',
      'DOCTOR_CONSULTATION',
      'DOCTOR_APPROVAL_REQUIRED',
    ]) {
      readable = readable.replaceAll(action, triageActionLabel(action));
    }
    for (final risk in ['LOW', 'MEDIUM', 'HIGH']) {
      readable = readable.replaceAll(
        RegExp('\\b$risk\\b'),
        '${risk[0]}${risk.substring(1).toLowerCase()}',
      );
    }
    for (final specialty in [
      'GENERAL_MEDICINE',
      'DERMATOLOGY',
      'CARDIOLOGY',
      'NEUROLOGY',
      'ORTHOPEDICS',
      'PEDIATRICS',
      'ENT',
      'OPHTHALMOLOGY',
      'GYNECOLOGY',
      'PSYCHIATRY',
    ]) {
      readable = readable.replaceAll(
        specialty,
        triageSpecialtyLabel(specialty),
      );
    }
    if (_incomplete) {
      readable = readable.replaceAll(
        'Symptoms analyzed by AI',
        'Symptoms recorded; automated assessment needs review',
      );
    }
    return readable;
  }

  void _showDetails() => showModalBottomSheet<void>(
    context: context,
    isScrollControlled: true,
    useSafeArea: true,
    showDragHandle: true,
    builder: (context) => DraggableScrollableSheet(
      expand: false,
      initialChildSize: 0.65,
      minChildSize: 0.35,
      maxChildSize: 0.95,
      builder: (context, controller) => ListView(
        controller: controller,
        padding: const EdgeInsets.fromLTRB(20, 0, 20, 24),
        children: [
          Text(
            'Triage details and history',
            style: Theme.of(context).textTheme.titleLarge,
          ),
          const SizedBox(height: 12),
          Text(triageStatusLabel(_status)),
          if (_auditError != null)
            Padding(
              padding: const EdgeInsets.symmetric(vertical: 12),
              child: Text(_auditError!),
            ),
          const SizedBox(height: 16),
          if (_logs.isEmpty) const Text('No history is available yet.'),
          for (final log in _logs)
            Padding(
              padding: const EdgeInsets.only(bottom: 12),
              child: TriageSection(
                title: _formatDate(log['createdAt']),
                child: Text(_readableLog(log['logMessage']?.toString() ?? '')),
              ),
            ),
          TextButton(
            onPressed: () => Navigator.pop(context),
            child: const Text('Close'),
          ),
        ],
      ),
    ),
  );

  Widget _resultCard() {
    final scheme = Theme.of(context).colorScheme;
    final knownRisk = ['LOW', 'MEDIUM', 'HIGH'].contains(_risk);
    final title = _incomplete
        ? 'Your assessment needs doctor review'
        : !_assessmentConfirmed
        ? 'Your assessment is being checked'
        : switch (_risk) {
            'LOW' => 'Self-Care and Monitoring Recommended',
            'MEDIUM' => 'Medical Consultation Recommended',
            'HIGH' => 'Urgent Medical Review Required',
            _ => 'Your assessment is being checked',
          };
    final icon = _incomplete
        ? Icons.medical_information_outlined
        : switch (_risk) {
            'LOW' => Icons.health_and_safety_outlined,
            'MEDIUM' => Icons.local_hospital_outlined,
            'HIGH' => Icons.priority_high_rounded,
            _ => Icons.info_outline,
          };
    final color = _incomplete
        ? scheme.error
        : switch (_risk) {
            'LOW' => AppColors.open,
            'MEDIUM' => AppColors.booked,
            'HIGH' => scheme.error,
            _ => scheme.primary,
          };
    return AppCard(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Icon(icon, size: 24, color: color),
                const SizedBox(width: 12),
                Expanded(
                  child: Text(
                    title,
                    style: Theme.of(context).textTheme.titleMedium
                        ?.copyWith(color: color),
                  ),
                ),
              ],
            ),
            const SizedBox(height: 12),
            if (_incomplete)
              Text(
                _approved || _rejected
                    ? 'The automated assessment is incomplete. Follow the doctor’s notes, not unconfirmed AI recommendations.'
                    : 'The automated assessment is incomplete. A doctor needs to review your case before its recommendations can be relied on.',
              )
            else if (knownRisk) ...[
              Text(
                '${_risk == 'LOW'
                    ? 'Low'
                    : _risk == 'MEDIUM'
                    ? 'Medium'
                    : 'High'} risk · Assessment score: ${_data['riskScore'] ?? 'Unavailable'}/10',
              ),
              const SizedBox(height: 8),
              Text(
                'This AI assessment is a guide, not a diagnosis or a clinically validated emergency decision.',
                style: Theme.of(context).textTheme.bodySmall,
              ),
            ] else
              const Text(
                'We are checking the saved assessment. Refresh the status or open your history.',
              ),
            const Divider(height: 32),
            TriageCareTracker(stages: _careStages()),
          ],
        ),
      ),
    );
  }

  Widget _reviewCard() {
    final notes = _doctorNotes();
    final title = _rejected
        ? 'Not approved by the doctor'
        : _approved
        ? 'Approved by a doctor'
        : _status == 'NEEDS_DOCTOR_APPROVAL'
        ? 'Waiting for doctor review'
        : 'Checking doctor review status';
    return TriageSection(
      title: title,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Text(
            _rejected
                ? 'The doctor did not authorize this case to proceed. Read their notes for the next steps.'
                : _approved
                ? 'The doctor has authorized this case. Any nurse assignment is handled separately by your care team.'
                : 'Your case needs a doctor’s decision. We will update this screen when the decision is confirmed.',
          ),
          if (notes != null) ...[
            const SizedBox(height: 12),
            Text(
              "Doctor's notes",
              style: Theme.of(context).textTheme.titleSmall,
            ),
            const SizedBox(height: 4),
            Text(notes),
          ],
        ],
      ),
    );
  }

  Widget _emergencyWarning() => Container(
    key: const ValueKey('triage-emergency-warning'),
    padding: const EdgeInsets.all(16),
    decoration: BoxDecoration(
      color: Theme.of(context).colorScheme.errorContainer,
      borderRadius: BorderRadius.circular(16),
    ),
    child: Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Text(
          'Do not wait if this is an emergency',
          style: Theme.of(context).textTheme.titleMedium
              ?.copyWith(color: Theme.of(context).colorScheme.onErrorContainer),
        ),
        const SizedBox(height: 8),
        const Text(
          'If you think this is an emergency, seek emergency care now. Do not wait for this assessment or doctor review. Doctor approval does not replace emergency medical care.',
        ),
      ],
    ),
  );

  void _findDoctor() => Navigator.push(
    context,
    MaterialPageRoute(builder: (_) => const DoctorSearchScreen()),
  );

  Widget _nextAction() {
    final nextCare = _nextCareStage();
    final action = _rejected
        ? 'Read the doctor’s notes and contact your care team about an alternative next step. This case was not authorized to proceed.'
        : _incomplete
        ? _approved
              ? 'Use your doctor’s notes for next steps. The automated assessment remains incomplete; confirm any saved recommendations with your care team.'
              : 'Contact your care team for manual review. Do not rely on the incomplete automated recommendations.'
        : _approved
        ? nextCare.state == CareStageState.completed
              ? 'Your nurse visit was recorded as completed. Contact your care team if you need further help.'
              : nextCare.state == CareStageState.inProgress
              ? 'Follow your care team’s instructions for your nurse visit.'
              : 'Your care team will confirm the next step. Approval alone does not mean a nurse visit has been assigned.'
        : _needsReview
        ? 'Your case is waiting for a doctor’s decision. Do not wait for that decision if you think this is an emergency.'
        : _assessmentConfirmed
        ? triageActionLabel(_data['recommendedAction']?.toString())
        : 'Your assessment is being checked. Contact your care team if you need help now.';
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 4),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Text(
            _incomplete ? 'What to do next' : 'Recommended next step',
            style: Theme.of(context).textTheme.titleMedium,
          ),
          const SizedBox(height: 8),
          Text(action),
          if (_assessmentConfirmed && !_needsReview && _risk == 'LOW') ...[
            const SizedBox(height: 8),
            const Text(
              'Monitor your symptoms and seek medical care if they worsen.',
            ),
            TextButton(
              onPressed: _findDoctor,
              child: const Text('Explore consultation options'),
            ),
          ],
          if (_assessmentConfirmed && !_needsReview && _risk == 'MEDIUM') ...[
            const SizedBox(height: 12),
            FilledButton.icon(
              onPressed: _findDoctor,
              icon: const Icon(Icons.search),
              label: const Text('Find a Doctor'),
            ),
          ],
        ],
      ),
    );
  }

  Widget _summary(String symptoms, String specialty) => TriageSection(
    title: 'Assessment summary',
    child: Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        if (symptoms.isNotEmpty) ...[
          Text(
            'Symptoms you submitted',
            style: Theme.of(context).textTheme.labelLarge,
          ),
          const SizedBox(height: 4),
          Text(symptoms),
        ],
        if (specialty.isNotEmpty) ...[
          const SizedBox(height: 12),
          Text(
            _incomplete
                ? 'Suggested specialty (unconfirmed)'
                : 'Recommended specialty',
            style: Theme.of(context).textTheme.labelLarge,
          ),
          const SizedBox(height: 4),
          Text(triageSpecialtyLabel(specialty)),
        ],
        if (!_incomplete &&
            _needsReview &&
            _data['recommendedAction'] != null) ...[
          const SizedBox(height: 12),
          Text(
            'Assessment originally suggested: ${triageActionLabel(_data['recommendedAction']?.toString())}',
            style: Theme.of(context).textTheme.bodySmall,
          ),
        ],
        if (_data['followUpRecommended'] == true) ...[
          const SizedBox(height: 12),
          Text(
            _incomplete
                ? 'A follow-up suggestion was saved. Confirm it with your care team during manual review.'
                : 'Follow-up is recommended. Discuss the next steps with a healthcare professional.',
            style: Theme.of(context).textTheme.bodySmall,
          ),
        ],
      ],
    ),
  );

  @override
  Widget build(BuildContext context) {
    final reason = _data['reason']?.toString().trim() ?? '';
    final symptoms = _data['symptoms']?.toString().trim() ?? '';
    final specialty = _data['recommendedSpecialty']?.toString().trim() ?? '';
    return Scaffold(
      appBar: AppBar(
        title: const Text('Your assessment'),
        actions: const [TriageSosAction()],
      ),
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(20),
          child: Center(
            child: ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 640),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  if (_risk == 'HIGH') ...[
                    _emergencyWarning(),
                    const SizedBox(height: 16),
                  ],
                  _resultCard(),
                  const SizedBox(height: 16),
                  if (_needsReview) ...[
                    _reviewCard(),
                    const SizedBox(height: 16),
                  ] else ...[
                    Text(
                      _assessmentConfirmed
                          ? 'Assessment complete'
                          : 'Checking assessment status',
                      style: Theme.of(context).textTheme.titleSmall,
                    ),
                    const SizedBox(height: 16),
                  ],
                  _nextAction(),
                  const SizedBox(height: 20),
                  _summary(symptoms, specialty),
                  const SizedBox(height: 16),
                  if (reason.isNotEmpty) ...[
                    AppCard(
                      clipBehavior: Clip.antiAlias,
                      child: ExpansionTile(
                        title: Text(
                          _incomplete
                              ? 'Saved assessment information'
                              : 'Assessment explanation',
                        ),
                        subtitle: _incomplete
                            ? const Text(
                                'Incomplete — confirm with your care team',
                              )
                            : null,
                        childrenPadding: const EdgeInsets.fromLTRB(
                          16,
                          0,
                          16,
                          16,
                        ),
                        expandedCrossAxisAlignment: CrossAxisAlignment.stretch,
                        children: [Text(_readableLog(reason))],
                      ),
                    ),
                    const SizedBox(height: 12),
                  ],
                  OutlinedButton.icon(
                    onPressed: _showDetails,
                    icon: const Icon(Icons.receipt_long_outlined),
                    label: const Text('View Triage Details'),
                  ),
                  TextButton.icon(
                    onPressed: () => Navigator.push(
                      context,
                      MaterialPageRoute(
                        builder: (_) => TriageHistoryScreen(apiService: _api),
                      ),
                    ),
                    icon: const Icon(Icons.history),
                    label: const Text('My triage history'),
                  ),
                  const SizedBox(height: 16),
                  if (_risk != 'HIGH') ...[
                    Text(
                      'Emergency help',
                      style: Theme.of(context).textTheme.titleSmall,
                    ),
                    const SizedBox(height: 8),
                    const Text(
                      'If you think this is an emergency, seek emergency care now. Do not wait for this assessment or doctor review.',
                    ),
                    const SizedBox(height: 8),
                  ],
                  Text(
                    'SOS opens a confirmation to alert your emergency contacts; it does not automatically dispatch an ambulance or nurse.',
                    style: Theme.of(context).textTheme.bodySmall,
                  ),
                  if (_statusError != null || _auditError != null)
                    Semantics(
                      liveRegion: true,
                      child: Text(
                        [
                          _statusError,
                          _auditError,
                        ].whereType<String>().join('\n'),
                        style: TextStyle(
                          color: Theme.of(context).colorScheme.error,
                        ),
                      ),
                    ),
                  TextButton.icon(
                    onPressed: _fetching ? null : _refresh,
                    icon: const Icon(Icons.refresh),
                    label: Text(
                      _fetching ? 'Checking for updates…' : 'Refresh status',
                    ),
                  ),
                  TextButton(
                    onPressed: () => Navigator.of(context)
                        .pushNamedAndRemoveUntil('/patient', (route) => false),
                    child: const Text('Return to Home'),
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}
