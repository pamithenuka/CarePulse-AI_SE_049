import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/screens/symptom_intake_screen.dart';
import 'package:mobile_app/screens/triage_history_screen.dart';
import 'package:mobile_app/services/api_exception.dart';

import 'support/triage_fakes.dart';

Future<void> review(WidgetTester tester, FakeTriageApi api) async {
  await tester.pumpWidget(triageHarness(SymptomIntakeScreen(apiService: api)));
  await tester.enterText(
    find.byType(TextFormField),
    '  Headache since this morning  ',
  );
  await tapVisible(tester, find.text('Continue'));
  await tapVisible(tester, find.text('Review my symptoms'));
}

void main() {
  testWidgets('empty and short symptoms stay on step one', (tester) async {
    final api = FakeTriageApi();
    await tester.pumpWidget(
      triageHarness(SymptomIntakeScreen(apiService: api)),
    );
    for (final value in ['', '   ', 'abcd']) {
      await tester.enterText(find.byType(TextFormField), value);
      await tapVisible(tester, find.text('Continue'));
      expect(find.text('Step 1 of 3'), findsOneWidget);
      expect(
        find.text('Please describe your symptoms (at least 5 characters).'),
        findsOneWidget,
      );
    }
    expect(api.submissions, 0);
  });

  testWidgets(
    'steps and review edits preserve text, severity, duration and chips',
    (tester) async {
      await tester.pumpWidget(
        triageHarness(SymptomIntakeScreen(apiService: FakeTriageApi())),
      );
      await tester.enterText(
        find.byType(TextFormField),
        'Headache since this morning',
      );
      await tapVisible(tester, find.text('Continue'));
      await tapVisible(tester, find.text('Started today'));
      await tapVisible(tester, find.text('4–7 days').last);
      await tapVisible(tester, find.text('Severe'));
      await tapVisible(tester, find.text('Dizziness'));
      await tapVisible(tester, find.text('Review my symptoms'));
      expect(find.text('Step 3 of 3'), findsOneWidget);
      expect(find.text('4–7 days'), findsOneWidget);
      expect(
        find.text('Severe — Significant symptoms affect my normal activities.'),
        findsOneWidget,
      );
      expect(find.text('Dizziness'), findsOneWidget);
      await tapVisible(tester, find.byTooltip('Edit Symptoms'));
      expect(
        find.widgetWithText(TextFormField, 'Headache since this morning'),
        findsOneWidget,
      );
      await tapVisible(tester, find.text('Continue'));
      expect(
        tester
            .widget<FilterChip>(find.widgetWithText(FilterChip, 'Dizziness'))
            .selected,
        isTrue,
      );
      await tapVisible(tester, find.text('Review my symptoms'));
      expect(find.text('4–7 days'), findsOneWidget);
    },
  );

  testWidgets(
    'submission trims text, keeps payload shape and blocks repeated taps',
    (tester) async {
      final api = FakeTriageApi()
        ..submission = Completer<Map<String, dynamic>>();
      await review(tester, api);
      final button = find.widgetWithText(FilledButton, 'Assess My Symptoms');
      await tester.ensureVisible(button);
      await tester.tap(button);
      await tester.tap(button);
      await tester.pump();
      expect(api.submissions, 1);
      expect(find.text('Assessing your symptoms…'), findsOneWidget);
      expect(api.payload, {
        'patientProfileId': 'patient-1',
        'symptoms': 'Headache since this morning',
        'duration': 'Since this morning',
        'severity': 'Mild',
        'additionalSymptoms': <String>[],
        'hasPhotoAttachment': false,
        'latitude': null,
        'longitude': null,
      });
      api.submission!.completeError(
        ApiException(400, 'Please check your details.'),
      );
      await tester.pumpAndSettle();
      expect(find.text('Please check your details.'), findsOneWidget);
      expect(find.text('Try assessment again'), findsOneWidget);
      await tapVisible(tester, find.byTooltip('Edit Symptoms'));
      expect(
        find.widgetWithText(TextFormField, '  Headache since this morning  '),
        findsOneWidget,
      );
    },
  );

  for (final error in [
    ApiException(503, 'Workflow failed'),
    ApiException(408, 'Request timed out'),
    TimeoutException('Timed out'),
  ]) {
    testWidgets('$error blocks uncertain resubmission and links to history', (
      tester,
    ) async {
      final api = FakeTriageApi()..submissionError = error;
      await review(tester, api);
      await tapVisible(tester, find.text('Assess My Symptoms'));
      expect(api.submissions, 1);
      expect(
        find.textContaining('Your case may already be saved.'),
        findsOneWidget,
      );
      expect(
        tester
            .widget<FilledButton>(
              find.widgetWithText(FilledButton, 'Assess My Symptoms'),
            )
            .onPressed,
        isNull,
      );
      await tapVisible(tester, find.text('Check triage history'));
      expect(find.byType(TriageHistoryScreen), findsOneWidget);
      expect(api.submissions, 1);
    });
  }

  testWidgets('missing profile does not send a request', (tester) async {
    final api = FakeTriageApi();
    await tester.pumpWidget(
      triageHarness(SymptomIntakeScreen(apiService: api), withProfile: false),
    );
    await tester.enterText(find.byType(TextFormField), 'Headache');
    await tapVisible(tester, find.text('Continue'));
    await tapVisible(tester, find.text('Review my symptoms'));
    await tapVisible(tester, find.text('Assess My Symptoms'));
    expect(api.submissions, 0);
    expect(
      find.textContaining('Complete your patient profile'),
      findsOneWidget,
    );
  });
}
