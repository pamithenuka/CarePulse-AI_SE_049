import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/screens/emergency/sos_screen.dart';
import 'package:mobile_app/screens/triage_status_screen.dart';
import 'package:mobile_app/widgets/triage_widgets.dart';
import 'package:mobile_app/theme/app_theme.dart';

import 'support/triage_fakes.dart';

List<TriageCareStage> stages(WidgetTester tester) =>
    tester.widget<TriageCareTracker>(find.byType(TriageCareTracker)).stages;

Future<void> show(
  WidgetTester tester,
  FakeTriageApi api, {
  double scale = 1,
}) async {
  await tester.pumpWidget(
    triageHarness(
      TriageStatusScreen(triageData: api.current, apiService: api),
      textScale: scale,
    ),
  );
  await tester.pumpAndSettle();
}

void verifyIcons(WidgetTester tester) {
  for (final stage in stages(tester)) {
    final scheme = Theme.of(tester.element(find.byType(TriageCareTracker)))
        .colorScheme;
    expect(
      tester.widget<Icon>(find.byKey(ValueKey('care-stage-${stage.id}'))).color,
      switch (stage.state) {
        CareStageState.completed => AppColors.open,
        CareStageState.inProgress => const Color(0xFF946200),
        CareStageState.pending => scheme.outline,
        CareStageState.failed => scheme.error,
      },
    );
    expect(
      tester.widget<Icon>(find.byKey(ValueKey('care-stage-${stage.id}'))).icon,
      switch (stage.state) {
        CareStageState.completed => Icons.check_circle,
        CareStageState.inProgress => Icons.schedule,
        CareStageState.pending => Icons.radio_button_unchecked,
        CareStageState.failed => Icons.cancel,
      },
    );
  }
}

void main() {
  testWidgets(
    'unknown initial risk does not become a sticky manual-review failure',
    (tester) async {
      final api = FakeTriageApi()..current = assessment(level: 'HIGH');
      await tester.pumpWidget(
        triageHarness(
          TriageStatusScreen(
            triageData: {
              'id': 'case-1',
              'status': 'NEEDS_DOCTOR_APPROVAL',
              'requiresDoctorApproval': true,
            },
            apiService: api,
          ),
        ),
      );
      await tester.pumpAndSettle();
      expect(stages(tester)[1].state, CareStageState.completed);
      await tester.pumpWidget(const SizedBox());
    },
  );

  testWidgets(
    'approved manual review acknowledges the doctor without validating AI',
    (tester) async {
      final api = FakeTriageApi()
        ..current = {
          ...assessment(level: 'MEDIUM', status: 'APPROVED_BY_DOCTOR'),
          'requiresDoctorApproval': true,
          'assessmentFailed': true,
          'workflowStatus': 'AwaitingDispatch',
        };
      await show(tester, api);
      expect(stages(tester)[1].state, CareStageState.failed);
      expect(stages(tester)[2].state, CareStageState.completed);
      expect(
        find.textContaining('Use your doctor’s notes for next steps.'),
        findsOneWidget,
      );
      expect(find.text('Find a Doctor'), findsNothing);
      await tester.pumpWidget(const SizedBox());
    },
  );

  testWidgets(
    'tracker announces completed, active, pending and unsuccessful stages',
    (tester) async {
      final semantics = tester.ensureSemantics();
      await tester.pumpWidget(
        triageHarness(
          Scaffold(
            body: TriageCareTracker(
              stages: [
                for (final state in CareStageState.values)
                  TriageCareStage(
                    id: state.name,
                    title: state.name,
                    detail: 'Saved progress.',
                    state: state,
                  ),
              ],
            ),
          ),
        ),
      );
      verifyIcons(tester);
      for (final label in [
        'completed, completed',
        'inProgress, in progress',
        'pending, pending',
        'failed, not completed',
      ]) {
        expect(
          find.bySemanticsLabel('$label. Saved progress.'),
          findsOneWidget,
        );
      }
      semantics.dispose();
    },
  );

  testWidgets(
    'approved history without prior assessment evidence stays unconfirmed',
    (tester) async {
      final api = FakeTriageApi()
        ..current = {
          ...assessment(level: 'HIGH', status: 'APPROVED_BY_DOCTOR'),
          'workflowStatus': 'AwaitingDispatch',
          'workflowId': 'workflow-1',
        };
      await show(tester, api);
      expect(stages(tester)[1].state, CareStageState.pending);
      expect(stages(tester)[2].state, CareStageState.completed);
      api.current['execution'] = [
        {
          'Step': 'Planner',
          'Result': {'Status': 0},
        },
        {
          'Step': 'DomainAnalysis',
          'Result': {'AssessmentFailed': false},
        },
        {'Step': 'Validation', 'Result': {}},
        {'Step': 'DoctorApproved', 'Result': {}},
      ];
      await tester.pump(const Duration(seconds: 5));
      await tester.pumpAndSettle();
      expect(stages(tester)[1].state, CareStageState.completed);
      await tester.pumpWidget(const SizedBox());
    },
  );

  testWidgets('doctor notes from another case are never shown', (tester) async {
    final api = FakeTriageApi()
      ..current = assessment(level: 'HIGH', status: 'APPROVED_BY_DOCTOR')
      ..logs = [
        {
          'triageTicketId': 'other-case',
          'logMessage':
              'Triage ticket approved by doctor. Notes: Wrong patient.',
        },
      ];
    await show(tester, api);
    expect(find.text('Wrong patient.'), findsNothing);
    await tester.pumpWidget(const SizedBox());
  });

  for (final scenario in [
    (
      risk: 'LOW',
      status: 'COMPLETED',
      manual: false,
      expected: [
        CareStageState.completed,
        CareStageState.completed,
        CareStageState.pending,
      ],
    ),
    (
      risk: 'MEDIUM',
      status: 'DOCTOR_CONSULTATION_RECOMMENDED',
      manual: false,
      expected: [
        CareStageState.completed,
        CareStageState.completed,
        CareStageState.pending,
      ],
    ),
    (
      risk: 'HIGH',
      status: 'NEEDS_DOCTOR_APPROVAL',
      manual: false,
      expected: [
        CareStageState.completed,
        CareStageState.completed,
        CareStageState.inProgress,
        CareStageState.pending,
      ],
    ),
    (
      risk: 'HIGH',
      status: 'APPROVED_BY_DOCTOR',
      manual: false,
      expected: [
        CareStageState.completed,
        CareStageState.completed,
        CareStageState.completed,
        CareStageState.pending,
      ],
    ),
    (
      risk: 'HIGH',
      status: 'REJECTED',
      manual: false,
      expected: [
        CareStageState.completed,
        CareStageState.completed,
        CareStageState.failed,
        CareStageState.pending,
      ],
    ),
    (
      risk: 'LOW',
      status: 'NEEDS_DOCTOR_APPROVAL',
      manual: true,
      expected: [
        CareStageState.completed,
        CareStageState.failed,
        CareStageState.inProgress,
        CareStageState.pending,
      ],
    ),
    (
      risk: 'MEDIUM',
      status: 'NEEDS_DOCTOR_APPROVAL',
      manual: true,
      expected: [
        CareStageState.completed,
        CareStageState.failed,
        CareStageState.inProgress,
        CareStageState.pending,
      ],
    ),
  ]) {
    testWidgets('${scenario.risk} ${scenario.status} manual=${scenario.manual} '
        'has evidence-driven stages and icons', (tester) async {
      final api = FakeTriageApi()
        ..current = {
          ...assessment(level: scenario.risk, status: scenario.status),
          if (scenario.manual) ...{
            'workflowStatus': 'ManualReviewRequired',
            'requiresDoctorApproval': true,
          },
        };
      await show(tester, api);
      expect(stages(tester).map((stage) => stage.state), scenario.expected);
      expect(stages(tester).first.title, 'Symptoms submitted');
      expect(
        stages(tester)[1].title,
        scenario.manual ? 'AI assessment' : 'AI assessment completed',
      );
      expect(stages(tester).last.title, 'Next care step');
      verifyIcons(tester);
      if (!scenario.manual && scenario.risk != 'HIGH') {
        expect(
          stages(tester).any((stage) => stage.id == 'doctor-review'),
          isFalse,
        );
        expect(stages(tester).last.detail, contains('not tracked here'));
      }
      if (scenario.manual) {
        expect(find.text('AI assessment completed'), findsNothing);
      }
      await tester.pumpWidget(const SizedBox());
    });
  }

  for (final plannerStatus in [1, 2, 'ValidationFailed', 'LlmError']) {
    testWidgets('historical planner failure $plannerStatus survives approval', (
      tester,
    ) async {
      final api = FakeTriageApi()
        ..current = {
          ...assessment(level: 'HIGH', status: 'APPROVED_BY_DOCTOR'),
          'workflowStatus': 'AwaitingDispatch',
          'execution': [
            {
              'Step': 'Planner',
              'Result': {'Status': plannerStatus},
            },
            {
              'Step': 'DoctorApproved',
              'Result': {'Notes': 'Reviewed'},
            },
          ],
        };
      await show(tester, api);
      expect(stages(tester)[1].state, CareStageState.failed);
      expect(stages(tester)[2].state, CareStageState.completed);
      expect(stages(tester).last.state, CareStageState.pending);
      expect(find.textContaining('Assessment score:'), findsNothing);
      await tester.pumpWidget(const SizedBox());
    });
  }

  for (final failure in [
    {
      'Step': 'SafeFailure',
      'Result': {'ErrorMessage': 'Tool failed'},
    },
    {
      'step': 'DomainAnalysis',
      'result': {'assessmentFailed': true},
    },
  ]) {
    testWidgets(
      'saved failure event ${failure.keys.first} prevents false completion',
      (tester) async {
        final api = FakeTriageApi()
          ..current = {
            ...assessment(level: 'HIGH', status: 'APPROVED_BY_DOCTOR'),
            'workflowStatus': 'AwaitingDispatch',
            'execution': [failure],
          };
        await show(tester, api);
        expect(stages(tester)[1].state, CareStageState.failed);
        await tester.pumpWidget(const SizedBox());
      },
    );
  }

  for (final scenario in [
    (
      dispatch: 'Assigned',
      status: 'DISPATCH_ASSIGNED',
      state: CareStageState.inProgress,
    ),
    (
      dispatch: 'EnRoute',
      status: 'DISPATCH_ASSIGNED',
      state: CareStageState.inProgress,
    ),
    (
      dispatch: 'ArrivedOnSite',
      status: 'DISPATCH_ASSIGNED',
      state: CareStageState.inProgress,
    ),
    (
      dispatch: 'Completed',
      status: 'VISIT_COMPLETED',
      state: CareStageState.completed,
    ),
    (
      dispatch: 'Unknown',
      status: 'APPROVED_BY_DOCTOR',
      state: CareStageState.pending,
    ),
  ]) {
    testWidgets(
      'explicit visit ${scenario.dispatch} controls next care stage',
      (tester) async {
        final api = FakeTriageApi()
          ..current = {
            ...assessment(level: 'HIGH', status: scenario.status),
            'dispatchStatus': scenario.dispatch,
          };
        await show(tester, api);
        expect(stages(tester).last.state, scenario.state);
        verifyIcons(tester);
        await tester.pumpWidget(const SizedBox());
      },
    );
  }

  testWidgets('workflow completion alone never confirms downstream care', (
    tester,
  ) async {
    final api = FakeTriageApi()
      ..current = {
        ...assessment(level: 'HIGH', status: 'APPROVED_BY_DOCTOR'),
        'workflowStatus': 'Completed',
      };
    await show(tester, api);
    expect(stages(tester).last.state, CareStageState.pending);
    await tester.pumpWidget(const SizedBox());
  });

  testWidgets('nurse escalation is active review, not a completed care step', (
    tester,
  ) async {
    final api = FakeTriageApi()
      ..current = {
        ...assessment(level: 'HIGH', status: 'DISPATCH_ASSIGNED'),
        'dispatchStatus': 'ArrivedOnSite',
        'workflowStatus': 'Escalated',
      };
    await show(tester, api);
    expect(stages(tester).last.state, CareStageState.inProgress);
    expect(stages(tester).last.detail, contains('further care team review'));
    await tester.pumpWidget(const SizedBox());
  });

  testWidgets(
    'partial history waits for completion evidence and keeps failures sticky',
    (tester) async {
      final api = FakeTriageApi()
        ..current = {
          'id': 'case-1',
          'riskLevel': 'HIGH',
          'status': 'NEEDS_DOCTOR_APPROVAL',
        };
      await show(tester, api);
      expect(stages(tester)[1].state, CareStageState.pending);
      api.current = {...assessment(level: 'HIGH'), 'workflowStatus': 'Failed'};
      await tester.pump(const Duration(seconds: 5));
      await tester.pumpAndSettle();
      expect(stages(tester)[1].state, CareStageState.failed);
      api.current = {
        'id': 'case-1',
        'status': 'APPROVED_BY_DOCTOR',
        'workflowStatus': 'AwaitingDispatch',
      };
      await tester.pump(const Duration(seconds: 5));
      await tester.pumpAndSettle();
      expect(stages(tester)[1].state, CareStageState.failed);
      expect(stages(tester)[2].state, CareStageState.completed);
      await tester.pumpWidget(const SizedBox());
    },
  );

  testWidgets('running workflow is not a successfully completed assessment', (
    tester,
  ) async {
    final api = FakeTriageApi()
      ..current = {...assessment(level: 'LOW'), 'workflowStatus': 'Running'};
    await show(tester, api);
    expect(stages(tester)[1].state, CareStageState.inProgress);
    expect(find.text('AI assessment completed'), findsNothing);
    await tester.pumpWidget(const SizedBox());
  });

  testWidgets(
    'new audit notes cannot turn an unconfirmed status into approval',
    (tester) async {
      final api = FakeTriageApi()
        ..current = assessment(level: 'HIGH')
        ..statusError = Exception('offline')
        ..logs = [
          {
            'logMessage':
                'Triage ticket approved by doctor. Notes: New approval.',
          },
        ];
      await show(tester, api);
      expect(stages(tester)[2].state, CareStageState.inProgress);
      expect(find.text('New approval.'), findsNothing);
      await tester.pumpWidget(const SizedBox());
    },
  );

  testWidgets('status updates cannot replace another case’s assessment', (
    tester,
  ) async {
    final initial = assessment(level: 'HIGH');
    final api = FakeTriageApi()
      ..current = {...assessment(), 'id': 'other-case'};
    await tester.pumpWidget(
      triageHarness(TriageStatusScreen(triageData: initial, apiService: api)),
    );
    await tester.pumpAndSettle();
    expect(stages(tester)[2].state, CareStageState.inProgress);
    expect(find.textContaining('High risk'), findsOneWidget);
    expect(initial['riskLevel'], 'HIGH');
    await tester.pumpWidget(const SizedBox());
  });

  testWidgets(
    'overlapping status polls keep timeline stable until confirmation',
    (tester) async {
      final api = FakeTriageApi()
        ..current = assessment(level: 'HIGH')
        ..statusRequest = Completer<Map<String, dynamic>>();
      await show(tester, api);
      await tester.pump(const Duration(seconds: 15));
      expect(api.statusCalls, 1);
      expect(stages(tester)[2].state, CareStageState.inProgress);
      api.statusRequest!.complete(
        assessment(level: 'HIGH', status: 'APPROVED_BY_DOCTOR'),
      );
      await tester.pumpAndSettle();
      expect(stages(tester)[2].state, CareStageState.completed);
      final calls = api.statusCalls;
      await tester.pumpWidget(const SizedBox());
      await tester.pump(const Duration(seconds: 10));
      expect(api.statusCalls, calls);
    },
  );

  for (final size in [const Size(320, 568), const Size(390, 844)]) {
    for (final scale in [1.0, 2.0]) {
      for (final status in [
        'NEEDS_DOCTOR_APPROVAL',
        'APPROVED_BY_DOCTOR',
        'REJECTED',
      ]) {
        testWidgets('HIGH $status scrolls safely at $size and text $scale', (
          tester,
        ) async {
          tester.view.physicalSize = size;
          tester.view.devicePixelRatio = 1;
          addTearDown(tester.view.resetPhysicalSize);
          addTearDown(tester.view.resetDevicePixelRatio);
          final api = FakeTriageApi()
            ..current = {
              ...assessment(level: 'HIGH', status: status),
              'riskScore': 9,
              'symptoms': 'Severe chest pain.',
            };
          await show(tester, api, scale: scale);
          final warning = find.byKey(
            const ValueKey('triage-emergency-warning'),
          );
          expect(
            tester.getTopLeft(warning).dy,
            lessThan(tester.getTopLeft(find.byType(TriageCareTracker)).dy),
          );
          expect(
            find.textContaining(
              'Doctor approval does not replace emergency medical care',
            ),
            findsOneWidget,
          );
          await tapVisible(tester, find.text('Assessment explanation'));
          expect(
            find.text('Assessment explanation from the server.'),
            findsOneWidget,
          );
          await tapVisible(tester, find.text('View Triage Details'));
          expect(find.text('Triage details and history'), findsOneWidget);
          await tapVisible(tester, find.text('Close'));
          await tester.ensureVisible(find.text('Return to Home'));
          await tester.pumpAndSettle();
          expect(find.text('Return to Home').hitTestable(), findsOneWidget);
          expect(find.byTooltip('Emergency SOS').hitTestable(), findsOneWidget);
          expect(tester.takeException(), isNull);
          await tester.pumpWidget(const SizedBox());
        });
      }
    }
  }

  testWidgets('results SOS retains the contact-alert confirmation flow', (
    tester,
  ) async {
    final api = FakeTriageApi()..current = assessment(level: 'HIGH');
    await show(tester, api);
    await tapVisible(tester, find.byTooltip('Emergency SOS'));
    expect(find.byType(SosScreen), findsOneWidget);
    await tapVisible(tester, find.text('SOS'));
    expect(find.text('Send emergency alert?'), findsOneWidget);
    await tapVisible(tester, find.text('Cancel'));
    expect(find.text('Alert sent'), findsNothing);
    await tester.pumpWidget(const SizedBox());
  });
}
