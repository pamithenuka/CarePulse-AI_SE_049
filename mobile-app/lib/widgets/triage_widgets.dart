import 'package:flutter/material.dart';

import '../theme/app_theme.dart';
import '../screens/emergency/sos_screen.dart';
import './app_card.dart';

enum CareStageState { completed, inProgress, pending, failed }

class TriageCareStage {
  final String id;
  final String title;
  final String detail;
  final CareStageState state;

  const TriageCareStage({
    required this.id,
    required this.title,
    required this.detail,
    required this.state,
  });
}

/// Presentation only: callers provide stages supported by saved case data.
class TriageCareTracker extends StatelessWidget {
  final List<TriageCareStage> stages;

  const TriageCareTracker({super.key, required this.stages});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Text('Your care progress', style: theme.textTheme.titleMedium),
        const SizedBox(height: 16),
        for (var index = 0; index < stages.length; index++)
          Padding(
            padding: EdgeInsets.only(
              bottom: index == stages.length - 1 ? 0 : 16,
            ),
            child: Semantics(
              label:
                  '${stages[index].title}, ${switch (stages[index].state) {
                    CareStageState.completed => 'completed',
                    CareStageState.inProgress => 'in progress',
                    CareStageState.pending => 'pending',
                    CareStageState.failed => 'not completed',
                  }}. ${stages[index].detail}',
              child: ExcludeSemantics(
                child: Row(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Icon(
                      switch (stages[index].state) {
                        CareStageState.completed => Icons.check_circle,
                        CareStageState.inProgress => Icons.schedule,
                        CareStageState.pending => Icons.radio_button_unchecked,
                        CareStageState.failed => Icons.cancel,
                      },
                      key: ValueKey('care-stage-${stages[index].id}'),
                      size: 24,
                      color: switch (stages[index].state) {
                        CareStageState.completed => AppColors.open,
                        CareStageState.inProgress => const Color(0xFF946200),
                        CareStageState.pending => theme.colorScheme.outline,
                        CareStageState.failed => theme.colorScheme.error,
                      },
                    ),
                    const SizedBox(width: 12),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            stages[index].title,
                            style: theme.textTheme.titleSmall,
                          ),
                          const SizedBox(height: 3),
                          Text(
                            stages[index].detail,
                            style: theme.textTheme.bodySmall,
                          ),
                        ],
                      ),
                    ),
                  ],
                ),
              ),
            ),
          ),
      ],
    );
  }
}

/// A non-interactive overview; navigation still follows the validated form.
class TriageStepIndicator extends StatelessWidget {
  final int currentStep;

  const TriageStepIndicator({super.key, required this.currentStep});

  static const _labels = ['Symptoms', 'Details', 'Review'];

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    Widget bar(int index) => Container(
      height: 5,
      decoration: BoxDecoration(
        color: index <= currentStep
            ? theme.colorScheme.primary
            : theme.colorScheme.outlineVariant,
        borderRadius: BorderRadius.circular(3),
      ),
    );

    Widget label(int index, {bool stacked = false}) => Semantics(
      label:
          '${_labels[index]}, step ${index + 1} of 3, '
          '${index == currentStep
              ? 'current'
              : index < currentStep
              ? 'completed'
              : 'upcoming'}',
      child: ExcludeSemantics(
        child: Text(
          stacked ? '${index + 1}. ${_labels[index]}' : _labels[index],
          textAlign: stacked ? TextAlign.start : TextAlign.center,
          style: theme.textTheme.labelMedium?.copyWith(
            fontWeight: index == currentStep
                ? FontWeight.w700
                : FontWeight.w500,
            color: index == currentStep
                ? theme.colorScheme.primary
                : theme.colorScheme.onSurfaceVariant,
          ),
        ),
      ),
    );

    return LayoutBuilder(
      builder: (context, constraints) {
        // Keep full labels readable instead of breaking words at large text sizes.
        final stacked =
            constraints.maxWidth < 400 &&
            MediaQuery.textScalerOf(context).scale(12) > 18;
        final segments = Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            for (var index = 0; index < _labels.length; index++) ...[
              if (index > 0) const SizedBox(width: 12),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    bar(index),
                    if (!stacked) ...[const SizedBox(height: 8), label(index)],
                  ],
                ),
              ),
            ],
          ],
        );
        if (!stacked) return segments;
        return Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            segments,
            for (var index = 0; index < _labels.length; index++)
              Padding(
                padding: const EdgeInsets.only(top: 8),
                child: label(index, stacked: true),
              ),
          ],
        );
      },
    );
  }
}

/// Shared spacing and hierarchy for the intake, assessment and history screens.
class TriageSection extends StatelessWidget {
  final String title;
  final Widget child;
  final Widget? trailing;

  const TriageSection({
    super.key,
    required this.title,
    required this.child,
    this.trailing,
  });

  @override
  Widget build(BuildContext context) => AppCard(
    child: Padding(
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Row(
            children: [
              Expanded(
                child: Text(
                  title,
                  style: Theme.of(context).textTheme.titleMedium,
                ),
              ),
              ?trailing,
            ],
          ),
          const SizedBox(height: 12),
          child,
        ],
      ),
    ),
  );
}

/// Opens the existing contact-alert confirmation; it does not send an alert.
class TriageSosAction extends StatelessWidget {
  const TriageSosAction({super.key});

  @override
  Widget build(BuildContext context) => IconButton(
    tooltip: 'Emergency SOS',
    iconSize: 40,
    icon: Container(
      width: 40,
      height: 40,
      alignment: Alignment.center,
      decoration: BoxDecoration(
        color: Theme.of(context).colorScheme.errorContainer,
        borderRadius: BorderRadius.circular(12),
      ),
      child: FittedBox(
        fit: BoxFit.scaleDown,
        child: Text(
          'SOS',
          style: TextStyle(
            fontSize: 14,
            fontWeight: FontWeight.bold,
            color: Theme.of(context).colorScheme.onErrorContainer,
          ),
        ),
      ),
    ),
    onPressed: () =>
        Navigator.of(context)
            .push(MaterialPageRoute(builder: (_) => const SosScreen())),
  );
}

/// Presentation only: backend status/action values are never changed.
String triageStatusLabel(String? status) => switch (status) {
  'COMPLETED' => 'Assessment complete',
  'DOCTOR_CONSULTATION_RECOMMENDED' => 'Medical consultation recommended',
  'NEEDS_DOCTOR_APPROVAL' => 'Waiting for doctor review',
  'APPROVED_BY_DOCTOR' => 'Approved by a doctor',
  'REJECTED' => 'Not approved by the doctor',
  'DISPATCH_ASSIGNED' => 'Nurse visit assigned',
  'VISIT_COMPLETED' => 'Nurse visit completed',
  _ => 'Checking your case status',
};

String triageActionLabel(String? action) => switch (action) {
  'SELF_CARE_MONITORING' => 'Self-care and monitoring recommended',
  'DOCTOR_CONSULTATION' => 'Medical consultation recommended',
  'DOCTOR_APPROVAL_REQUIRED' ||
  'Immediate manual review' => 'Doctor review required',
  _ => 'Discuss the next steps with your care team',
};

String triageSpecialtyLabel(String specialty) => switch (specialty) {
  'GENERAL_MEDICINE' => 'General medicine',
  'DERMATOLOGY' => 'Dermatology',
  'CARDIOLOGY' => 'Cardiology',
  'NEUROLOGY' => 'Neurology',
  'ORTHOPEDICS' => 'Orthopedics',
  'PEDIATRICS' => 'Pediatrics',
  'ENT' => 'Ear, nose and throat',
  'OPHTHALMOLOGY' => 'Ophthalmology',
  'GYNECOLOGY' => 'Gynecology',
  'PSYCHIATRY' => 'Psychiatry',
  _ => 'Ask your care team',
};
