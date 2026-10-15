import 'dart:io';
import 'dart:ui' as ui;

import 'package:flutter/material.dart';
import 'package:flutter/rendering.dart';
import 'package:flutter/services.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/screens/symptom_intake_screen.dart';
import 'package:mobile_app/screens/triage_status_screen.dart';

import 'support/triage_fakes.dart';

/// Opt-in rendered previews use test fixtures, never live patient information.
/// flutter test --dart-define=TRIAGE_PREVIEWS=true test/triage_preview_test.dart
void main() {
  testWidgets('render phone previews for intake and risk results', (
    tester,
  ) async {
    tester.view.physicalSize = const Size(390, 844);
    tester.view.devicePixelRatio = 1;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);
    await tester.runAsync(() async {
      final directory = File(Platform.resolvedExecutable).parent.parent.parent;
      final textFont = FontLoader('Roboto');
      for (final weight in ['regular', 'medium', 'bold']) {
        textFont.addFont(
          File('${directory.path}/material_fonts/roboto-$weight.ttf')
              .readAsBytes()
              .then((bytes) => ByteData.view(bytes.buffer)),
        );
      }
      await textFont.load();
      await (FontLoader('MaterialIcons')..addFont(
            File('${directory.path}/material_fonts/materialicons-regular.otf')
                .readAsBytes()
                .then((bytes) => ByteData.view(bytes.buffer)),
          ))
          .load();
    });
    final boundaryKey = GlobalKey();

    Widget preview(
      Widget screen, {
      double textScale = 1,
      double bottomInset = 0,
    }) => RepaintBoundary(
      key: boundaryKey,
      child: DecoratedBox(
        decoration: const BoxDecoration(
          gradient: LinearGradient(
            begin: Alignment.topLeft,
            end: Alignment.bottomRight,
            colors: [Color(0xFFC7E9E6), Color(0xFFDAE6F5), Color(0xFFD9D6F0)],
          ),
        ),
        child: triageHarness(
          screen,
          fontFamily: 'Roboto',
          textScale: textScale,
          bottomInset: bottomInset,
        ),
      ),
    );

    Future<void> capture(String name) async {
      // Material ink decorations may settle one frame after their widgets.
      await tester.pump(const Duration(milliseconds: 200));
      await tester.pumpAndSettle();
      expect(tester.takeException(), isNull);
      final boundary =
          boundaryKey.currentContext!.findRenderObject()!
              as RenderRepaintBoundary;
      await tester.runAsync(() async {
        final image = await boundary.toImage(pixelRatio: 2);
        try {
          final bytes = await image.toByteData(format: ui.ImageByteFormat.png);
          final file = File('build/triage-previews/$name.png');
          await file.parent.create(recursive: true);
          await file.writeAsBytes(bytes!.buffer.asUint8List());
        } finally {
          image.dispose();
        }
      });
    }

    final api = FakeTriageApi();
    await tester.pumpWidget(preview(SymptomIntakeScreen(apiService: api)));
    await capture('intake-step-1');
    await tester.enterText(
      find.byType(TextFormField),
      'I have had a headache since this morning.',
    );
    await tapVisible(tester, find.text('Continue'));
    await capture('intake-step-2');
    await tapVisible(tester, find.text('Review my symptoms'));
    await capture('intake-step-3');
    for (final scenario in [
      (name: 'small', size: const Size(320, 568), scale: 1.0, inset: 0.0),
      (name: 'large-text', size: const Size(320, 568), scale: 2.0, inset: 0.0),
      (
        name: 'small-keyboard',
        size: const Size(320, 568),
        scale: 1.0,
        inset: 240.0,
      ),
      (name: 'keyboard', size: const Size(390, 844), scale: 1.0, inset: 300.0),
    ]) {
      tester.view.physicalSize = scenario.size;
      await tester.pumpWidget(
        preview(
          SymptomIntakeScreen(key: ValueKey(scenario.name), apiService: api),
          textScale: scenario.scale,
          bottomInset: scenario.inset,
        ),
      );
      if (scenario.inset > 0) {
        await tester.enterText(
          find.byType(TextFormField),
          'I have had a headache since this morning.',
        );
      }
      await capture('intake-step-1-${scenario.name}');
      await tester.ensureVisible(find.text('Continue'));
      await capture('intake-step-1-${scenario.name}-continue');
    }
    tester.view.physicalSize = const Size(390, 844);
    for (final level in ['LOW', 'MEDIUM', 'HIGH']) {
      api.current = assessment(level: level);
      await tester.pumpWidget(
        preview(
          TriageStatusScreen(
            key: ValueKey(level),
            triageData: api.current,
            apiService: api,
          ),
        ),
      );
      await capture('result-${level.toLowerCase()}');
      await tester.pumpWidget(const SizedBox());
    }
    for (final scenario in [
      (name: 'pending', status: 'NEEDS_DOCTOR_APPROVAL', notes: null),
      (
        name: 'approved',
        status: 'APPROVED_BY_DOCTOR',
        notes: 'Contact the care team to discuss the authorized next step.',
      ),
      (
        name: 'rejected',
        status: 'REJECTED',
        notes: 'Please contact the care team for an alternative care plan.',
      ),
    ]) {
      api.current = {
        ...assessment(level: 'HIGH', status: scenario.status),
        'workflowStatus': scenario.name == 'pending'
            ? 'AwaitingApproval'
            : scenario.name == 'approved'
            ? 'AwaitingDispatch'
            : 'Rejected',
        'execution': [
          {
            'Step': 'Planner',
            'Result': {'Status': 0},
          },
          {
            'Step': 'DomainAnalysis',
            'Result': {'AssessmentFailed': false},
          },
          {'Step': 'Validation', 'Result': {}},
        ],
        'riskScore': 9,
        'symptoms': 'Severe chest pain.',
      };
      api.logs = [
        if (scenario.notes != null)
          {
            'triageTicketId': 'case-1',
            'logMessage':
                'Triage ticket ${scenario.name} by doctor. Notes: ${scenario.notes}',
            'createdAt': '2026-01-01T12:00:00Z',
          },
      ];
      await tester.pumpWidget(
        preview(
          TriageStatusScreen(
            key: ValueKey('high-${scenario.name}'),
            triageData: api.current,
            apiService: api,
          ),
        ),
      );
      await capture('result-high-${scenario.name}');
      await tester.ensureVisible(
        find.text(
          scenario.notes != null
              ? "Doctor's notes"
              : 'Waiting for doctor review',
        ),
      );
      await capture('result-high-${scenario.name}-review');
      await tester.pumpWidget(const SizedBox());
    }
  }, skip: !const bool.fromEnvironment('TRIAGE_PREVIEWS'));
}
