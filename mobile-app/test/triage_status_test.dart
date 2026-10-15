import 'dart:async';
import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:mobile_app/screens/booking_screen.dart';
import 'package:mobile_app/screens/doctor_search_screen.dart';
import 'package:mobile_app/screens/emergency/sos_screen.dart';
import 'package:mobile_app/screens/home/home_shell.dart';
import 'package:mobile_app/screens/symptom_intake_screen.dart';
import 'package:mobile_app/screens/triage_history_screen.dart';
import 'package:mobile_app/screens/triage_status_screen.dart';

import 'support/triage_fakes.dart';

Future<void> showResult(
  WidgetTester tester,
  FakeTriageApi api, {
  Map<String, dynamic>? initial,
}) async {
  await tester.pumpWidget(
    triageHarness(
      TriageStatusScreen(triageData: initial ?? api.current, apiService: api),
    ),
  );
  await tester.pumpAndSettle();
}

void main() {
  for (final level in ['LOW', 'MEDIUM', 'HIGH']) {
    testWidgets(
      '$level shows backend explanation, specialty, action and symptoms',
      (tester) async {
        final api = FakeTriageApi()..current = assessment(level: level);
        await showResult(tester, api);
        expect(
          find.text(switch (level) {
            'LOW' => 'Self-Care and Monitoring Recommended',
            'MEDIUM' => 'Medical Consultation Recommended',
            _ => 'Urgent Medical Review Required',
          }),
          findsOneWidget,
        );
        expect(
          find.text('Assessment explanation from the server.'),
          findsNothing,
        );
        await tapVisible(tester, find.text('Assessment explanation'));
        expect(
          find.text('Assessment explanation from the server.'),
          findsOneWidget,
        );
        expect(find.text('General medicine'), findsOneWidget);
        expect(find.text('Headache since this morning'), findsOneWidget);
        expect(
          find.textContaining('Follow-up is recommended.'),
          findsOneWidget,
        );
        expect(find.text('DOCTOR_APPROVAL_REQUIRED'), findsNothing);
        expect(find.text('GENERAL_MEDICINE'), findsNothing);
        expect(
          find.text('Find a Doctor'),
          level == 'MEDIUM' ? findsOneWidget : findsNothing,
        );
        if (level == 'HIGH') {
          expect(find.text('Waiting for doctor review'), findsOneWidget);
        }
        await tester.pumpWidget(const SizedBox());
      },
    );
  }

  for (final status in ['APPROVED_BY_DOCTOR', 'REJECTED']) {
    testWidgets('$status shows only matching doctor decision and notes', (
      tester,
    ) async {
      final api = FakeTriageApi()
        ..current = assessment(level: 'HIGH', status: status)
        ..logs = [
          {
            'logMessage':
                'Triage ticket approved by doctor. Notes: Approved notes.',
            'createdAt': '2026-01-01T12:00:00Z',
          },
          {
            'logMessage':
                'Triage ticket rejected by doctor. Notes: Rejected notes.',
            'createdAt': '2026-01-01T12:01:00Z',
          },
        ];
      await showResult(tester, api);
      expect(
        find.text(
          status == 'REJECTED'
              ? 'Not approved by the doctor'
              : 'Approved by a doctor',
        ),
        findsOneWidget,
      );
      expect(
        find.text(status == 'REJECTED' ? 'Rejected notes.' : 'Approved notes.'),
        findsOneWidget,
      );
      expect(
        find.text(status == 'REJECTED' ? 'Approved notes.' : 'Rejected notes.'),
        findsNothing,
      );
      expect(find.text('Waiting for doctor review'), findsNothing);
      await tester.pumpWidget(const SizedBox());
    });
  }

  for (final level in ['LOW', 'MEDIUM', 'HIGH']) {
    testWidgets(
      '$level failed workflow is incomplete, not a completed risk result',
      (tester) async {
        final api = FakeTriageApi()
          ..current = {
            ...assessment(level: level),
            'workflowStatus': 'ManualReviewRequired',
            'requiresDoctorApproval': true,
            'status': 'NEEDS_DOCTOR_APPROVAL',
          };
        await showResult(tester, api);
        expect(
          find.text('Your assessment needs doctor review'),
          findsOneWidget,
        );
        expect(find.textContaining('Assessment score:'), findsNothing);
        expect(find.text('Find a Doctor'), findsNothing);
        expect(find.text('Recommended next step'), findsNothing);
        expect(find.text('Waiting for doctor review'), findsOneWidget);
        expect(
          find.textContaining(
            'Do not wait for this assessment or doctor review.',
          ),
          findsOneWidget,
        );
        await tester.pumpWidget(const SizedBox());
      },
    );
  }

  testWidgets(
    'five-second polling merges status and assessment without mutating input',
    (tester) async {
      final initial = assessment(level: 'HIGH');
      final api = FakeTriageApi()..current = Map.of(initial);
      await showResult(tester, api, initial: initial);
      expect(api.statusCalls, 1);
      await tester.pump(const Duration(seconds: 4));
      expect(api.statusCalls, 1);
      api.current = {
        ...assessment(level: 'HIGH', status: 'APPROVED_BY_DOCTOR'),
        'reason': 'Updated saved explanation.',
        'recommendedSpecialty': 'CARDIOLOGY',
        'workflowStatus': 'AwaitingDispatch',
      };
      await tester.pump(const Duration(seconds: 1));
      await tester.pumpAndSettle();
      expect(find.text('Approved by a doctor'), findsOneWidget);
      await tapVisible(tester, find.text('Assessment explanation'));
      expect(find.text('Updated saved explanation.'), findsOneWidget);
      expect(find.text('Cardiology'), findsOneWidget);
      expect(initial['status'], 'NEEDS_DOCTOR_APPROVAL');
      api.current = {
        'id': 'case-1',
        'status': 'DISPATCH_ASSIGNED',
        'dispatchStatus': 'EnRoute',
      };
      await tester.pump(const Duration(seconds: 5));
      await tester.pumpAndSettle();
      expect(find.text('Updated saved explanation.'), findsOneWidget);
      expect(find.text('Nurse visit: on the way'), findsOneWidget);
      final calls = api.statusCalls;
      await tester.pumpWidget(const SizedBox());
      await tester.pump(const Duration(seconds: 10));
      expect(api.statusCalls, calls);
    },
  );

  testWidgets(
    'overlapping polls are blocked and disposal during a request is safe',
    (tester) async {
      final api = FakeTriageApi()
        ..statusRequest = Completer<Map<String, dynamic>>();
      await tester.pumpWidget(
        triageHarness(
          TriageStatusScreen(
            triageData: assessment(level: 'HIGH'),
            apiService: api,
          ),
        ),
      );
      await tester.pump(const Duration(seconds: 15));
      expect(api.statusCalls, 1);
      expect(api.auditCalls, 0);
      await tester.pumpWidget(const SizedBox());
      api.statusRequest!.complete(assessment(level: 'HIGH'));
      await tester.pump();
      expect(api.auditCalls, 0);
      expect(tester.takeException(), isNull);
    },
  );

  testWidgets('audit failure does not conceal a confirmed approval', (
    tester,
  ) async {
    final api = FakeTriageApi()
      ..current = assessment(level: 'HIGH', status: 'APPROVED_BY_DOCTOR')
      ..auditError = Exception('offline');
    await showResult(tester, api, initial: assessment(level: 'HIGH'));
    expect(find.text('Approved by a doctor'), findsOneWidget);
    expect(
      find.textContaining('We could not refresh the history.'),
      findsOneWidget,
    );
    await tester.pumpWidget(const SizedBox());
  });

  testWidgets('status failure retains last decision and provides refresh', (
    tester,
  ) async {
    final api = FakeTriageApi()
      ..current = assessment(level: 'HIGH', status: 'REJECTED')
      ..statusError = Exception('offline');
    await showResult(tester, api);
    expect(find.text('Not approved by the doctor'), findsOneWidget);
    expect(
      find.textContaining('The last saved information is shown.'),
      findsOneWidget,
    );
    api.statusError = null;
    await tapVisible(tester, find.text('Refresh status'));
    expect(api.statusCalls, 2);
    await tester.pumpWidget(const SizedBox());
  });

  testWidgets('terminal rejection stops automatic requests', (tester) async {
    final api = FakeTriageApi()
      ..current = assessment(level: 'HIGH', status: 'REJECTED');
    await showResult(tester, api);
    await tester.pump(const Duration(seconds: 15));
    expect(api.statusCalls, 1);
    await tester.pumpWidget(const SizedBox());
  });

  testWidgets(
    'history hydrates symptoms missing from the history-list response',
    (tester) async {
      final api = FakeTriageApi()
        ..cases = [
          {
            'id': 'case-1',
            'status': 'COMPLETED',
            'riskScore': 2,
            'riskLevel': 'LOW',
          },
        ];
      await tester.pumpWidget(
        triageHarness(TriageHistoryScreen(apiService: api)),
      );
      await tester.pumpAndSettle();
      await tapVisible(tester, find.text('Assessment complete'));
      expect(find.text('Headache since this morning'), findsOneWidget);
      await tester.pumpWidget(const SizedBox());
    },
  );

  testWidgets('medium uses the unchanged doctor search to booking workflow', (
    tester,
  ) async {
    FlutterSecureStorage.setMockInitialValues({
      'carepulse_session': jsonEncode({
        'token': 'test-token',
        'expiresAt': DateTime.now()
            .add(const Duration(hours: 1))
            .toIso8601String(),
      }),
    });
    final requests = <String>[];
    await http.runWithClient(
      () async {
        final api = FakeTriageApi()..current = assessment(level: 'MEDIUM');
        await showResult(tester, api);
        await tapVisible(tester, find.text('Find a Doctor'));
        expect(find.byType(DoctorSearchScreen), findsOneWidget);
        await tapVisible(tester, find.text('Dr Test'));
        expect(find.byType(BookingScreen), findsOneWidget);
        expect(
          requests,
          containsAll(['/api/v1/doctors', '/api/v1/doctors/slots']),
        );
        await tester.pumpWidget(const SizedBox());
      },
      () => MockClient((request) async {
        requests.add(request.url.path);
        return http.Response(
          jsonEncode(
            request.url.path.endsWith('/slots')
                ? []
                : [
                    {
                      'id': 'doctor-1',
                      'fullName': 'Dr Test',
                      'specialty': 'General medicine',
                    },
                  ],
          ),
          200,
        );
      }),
    );
  });

  testWidgets('SOS opens the existing confirmation without sending an alert', (
    tester,
  ) async {
    await tester.pumpWidget(triageHarness(const SymptomIntakeScreen()));
    await tapVisible(tester, find.byTooltip('Emergency SOS'));
    expect(find.byType(SosScreen), findsOneWidget);
    await tapVisible(tester, find.text('SOS'));
    expect(find.text('Send emergency alert?'), findsOneWidget);
    expect(
      find.textContaining('notify all of your emergency contacts'),
      findsOneWidget,
    );
    await tapVisible(tester, find.text('Cancel'));
    expect(find.text('Alert sent'), findsNothing);
  });

  testWidgets(
    'triage shell has SOS access without an overlapping floating button',
    (tester) async {
      await tester.pumpWidget(triageHarness(const HomeShell()));
      await tester.pumpAndSettle();
      await tapVisible(tester, find.text('Triage'));
      expect(find.byType(FloatingActionButton), findsNothing);
      expect(find.byTooltip('Emergency SOS'), findsOneWidget);
      await tester.enterText(
        find.byType(TextFormField),
        'Keep this description',
      );
      await tapVisible(tester, find.text('Profile'));
      await tapVisible(tester, find.text('Triage'));
      expect(
        find.widgetWithText(TextFormField, 'Keep this description'),
        findsOneWidget,
      );
    },
  );
}
