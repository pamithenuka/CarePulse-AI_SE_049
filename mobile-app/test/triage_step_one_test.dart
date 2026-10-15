import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/screens/symptom_intake_screen.dart';
import 'package:mobile_app/widgets/triage_widgets.dart';

import 'support/triage_fakes.dart';

String enteredText(WidgetTester tester) =>
    tester.widget<TextFormField>(find.byType(TextFormField)).controller!.text;

void main() {
  testWidgets('Step 1 actions have accessible labels and Android tap targets', (
    tester,
  ) async {
    final semantics = tester.ensureSemantics();
    await tester.pumpWidget(
      triageHarness(SymptomIntakeScreen(apiService: FakeTriageApi())),
    );
    await tester.pumpAndSettle();
    await expectLater(tester, meetsGuideline(androidTapTargetGuideline));
    await expectLater(tester, meetsGuideline(labeledTapTargetGuideline));
    semantics.dispose();
  });

  testWidgets(
    'friendly guidance and labeled progress preserve all three steps',
    (tester) async {
      final semantics = tester.ensureSemantics();
      await tester.pumpWidget(
        triageHarness(SymptomIntakeScreen(apiService: FakeTriageApi())),
      );
      expect(find.text('How are you feeling today?'), findsOneWidget);
      expect(find.text('How this works'), findsOneWidget);
      expect(find.textContaining('is not a diagnosis'), findsOneWidget);
      expect(find.text('Leave out names and contact details.'), findsOneWidget);
      expect(find.text('0 / 1,000'), findsOneWidget);
      expect(find.byType(LinearProgressIndicator), findsNothing);
      for (final label in ['Symptoms', 'Details', 'Review']) {
        expect(find.text(label), findsOneWidget);
      }
      expect(
        find.bySemanticsLabel('Symptoms, step 1 of 3, current'),
        findsOneWidget,
      );
      await tester.enterText(find.byType(TextFormField), 'Headache');
      await tapVisible(tester, find.text('Continue'));
      expect(
        tester
            .widget<TriageStepIndicator>(find.byType(TriageStepIndicator))
            .currentStep,
        1,
      );
      await tapVisible(tester, find.text('Review my symptoms'));
      expect(
        tester
            .widget<TriageStepIndicator>(find.byType(TriageStepIndicator))
            .currentStep,
        2,
      );
      expect(find.byTooltip('Emergency SOS'), findsOneWidget);
      semantics.dispose();
    },
  );

  testWidgets(
    'all examples append without replacing text or repeating phrases',
    (tester) async {
      await tester.pumpWidget(
        triageHarness(SymptomIntakeScreen(apiService: FakeTriageApi())),
      );
      const original = 'My throat has hurt since yesterday.';
      await tester.enterText(find.byType(TextFormField), original);
      final examples = {
        'Headache': 'I have a headache.',
        'Fever': 'I have a fever.',
        'Stomach pain': 'I have stomach pain.',
        'Cough': 'I have a cough.',
        'Dizziness': 'I feel dizzy.',
      };
      var expected = original;
      for (final entry in examples.entries) {
        final chip = find.widgetWithText(ActionChip, entry.key);
        await tapVisible(tester, chip);
        expected = '$expected ${entry.value}';
        expect(enteredText(tester), expected);
        expect(tester.widget<ActionChip>(chip).onPressed, isNull);
        // Even another tap on a disabled chip must leave the text untouched.
        await tapVisible(tester, chip);
        expect(enteredText(tester), expected);
      }
      final controller = tester
          .widget<TextFormField>(find.byType(TextFormField))
          .controller!;
      expect(controller.selection.baseOffset, expected.length);
      expect(find.text('${expected.length} / 1,000'), findsOneWidget);
    },
  );

  testWidgets('examples detect edited phrases and re-enable after removal', (
    tester,
  ) async {
    await tester.pumpWidget(
      triageHarness(SymptomIntakeScreen(apiService: FakeTriageApi())),
    );
    final chip = find.widgetWithText(ActionChip, 'Headache');
    await tester.enterText(find.byType(TextFormField), 'I HAVE A  HEADACHE!');
    await tester.pump();
    expect(tester.widget<ActionChip>(chip).onPressed, isNull);
    await tester.enterText(find.byType(TextFormField), 'My throat hurts.\n');
    await tapVisible(tester, chip);
    expect(enteredText(tester), 'My throat hurts.\nI have a headache.');
  });

  testWidgets('examples fit the exact limit and never truncate entered text', (
    tester,
  ) async {
    await tester.pumpWidget(
      triageHarness(SymptomIntakeScreen(apiService: FakeTriageApi())),
    );
    const phrase = 'I have a headache.';
    final original = 'a' * (1000 - phrase.length - 1);
    await tester.enterText(find.byType(TextFormField), original);
    await tapVisible(tester, find.widgetWithText(ActionChip, 'Headache'));
    expect(enteredText(tester), '$original $phrase');
    expect(enteredText(tester).length, 1000);
    expect(find.text('1000 / 1,000'), findsOneWidget);
    for (final chip in tester.widgetList<ActionChip>(find.byType(ActionChip))) {
      expect(chip.onPressed, isNull);
    }
    await tester.enterText(find.byType(TextFormField), 'b' * 999);
    await tester.pump();
    expect(
      tester
          .widget<ActionChip>(find.widgetWithText(ActionChip, 'Fever'))
          .onPressed,
      isNull,
    );
    expect(enteredText(tester), 'b' * 999);
  });

  testWidgets(
    'example text survives review edits and retains the API contract',
    (tester) async {
      final api = FakeTriageApi()
        ..submission = Completer<Map<String, dynamic>>();
      await tester.pumpWidget(
        triageHarness(SymptomIntakeScreen(apiService: api)),
      );
      await tester.enterText(
        find.byType(TextFormField),
        'Started this morning.',
      );
      await tapVisible(tester, find.widgetWithText(ActionChip, 'Fever'));
      await tapVisible(tester, find.text('Continue'));
      await tapVisible(tester, find.text('Review my symptoms'));
      await tapVisible(tester, find.byTooltip('Edit Symptoms'));
      expect(enteredText(tester), 'Started this morning. I have a fever.');
      expect(
        tester
            .widget<ActionChip>(find.widgetWithText(ActionChip, 'Fever'))
            .onPressed,
        isNull,
      );
      await tapVisible(tester, find.text('Continue'));
      await tapVisible(tester, find.text('Review my symptoms'));
      await tester.ensureVisible(find.text('Assess My Symptoms'));
      await tester.tap(find.text('Assess My Symptoms'));
      await tester.pump();
      expect(api.payload, {
        'patientProfileId': 'patient-1',
        'symptoms': 'Started this morning. I have a fever.',
        'duration': 'Since this morning',
        'severity': 'Mild',
        // Example chips only aid free text, not the additional-symptom selections.
        'additionalSymptoms': <String>[],
        'hasPhotoAttachment': false,
        'latitude': null,
        'longitude': null,
      });
      await tester.pumpWidget(const SizedBox());
      api.submission!.complete(assessment());
      await tester.pump();
    },
  );

  for (final scenario in [
    (size: const Size(320, 568), scale: 1.0, inset: 0.0),
    (size: const Size(320, 568), scale: 1.0, inset: 240.0),
    (size: const Size(390, 844), scale: 1.0, inset: 0.0),
    (size: const Size(390, 844), scale: 1.0, inset: 300.0),
    (size: const Size(320, 568), scale: 2.0, inset: 0.0),
    (size: const Size(320, 568), scale: 2.0, inset: 240.0),
  ]) {
    testWidgets('Step 1 remains usable at ${scenario.size}, '
        'text ${scenario.scale}, keyboard ${scenario.inset}', (tester) async {
      tester.view.physicalSize = scenario.size;
      tester.view.devicePixelRatio = 1;
      addTearDown(tester.view.resetPhysicalSize);
      addTearDown(tester.view.resetDevicePixelRatio);
      await tester.pumpWidget(
        triageHarness(
          SymptomIntakeScreen(apiService: FakeTriageApi()),
          textScale: scenario.scale,
          bottomInset: scenario.inset,
        ),
      );
      await tester.enterText(find.byType(TextFormField), 'Since this morning.');
      await tapVisible(tester, find.widgetWithText(ActionChip, 'Cough'));
      expect(enteredText(tester), 'Since this morning. I have a cough.');
      await tester.ensureVisible(find.text('How this works'));
      await tester.pumpAndSettle();
      expect(find.byTooltip('Emergency SOS').hitTestable(), findsOneWidget);
      final sosRect = tester.getRect(find.byTooltip('Emergency SOS'));
      // Offscreen scroll content can geometrically extend behind the app bar;
      // only the visible, clipped portion of the form is relevant to overlap.
      final fieldRect = tester
          .getRect(find.byType(TextFormField))
          .intersect(tester.getRect(find.byType(SingleChildScrollView)));
      expect(sosRect.overlaps(fieldRect), isFalse);
      await tester.ensureVisible(find.text('Continue'));
      await tester.pumpAndSettle();
      final button = find.widgetWithText(FilledButton, 'Continue');
      final buttonRect = tester.getRect(button);
      expect(buttonRect.height, greaterThanOrEqualTo(48));
      expect(
        buttonRect.bottom,
        lessThanOrEqualTo(scenario.size.height - scenario.inset),
      );
      expect(button.hitTestable(), findsOneWidget);
      expect(tester.takeException(), isNull);
      await tester.tap(button);
      await tester.pumpAndSettle();
      expect(find.text('Step 2 of 3'), findsOneWidget);
      expect(tester.takeException(), isNull);
    });
  }
}
