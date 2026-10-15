import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:geolocator/geolocator.dart';
import 'package:http/http.dart' as http;
import 'package:mobile_app/features/dispatch/services/location_service.dart';
import 'package:mobile_app/screens/home/home_shell.dart';
import 'package:mobile_app/screens/symptom_intake_screen.dart';
import 'package:mobile_app/screens/triage_status_screen.dart';

import 'support/triage_fakes.dart';

class FakeLocation extends LocationService {
  int permissions = 0;
  int positions = 0;
  bool allowed = true;
  @override
  Future<bool> requestPermission() async {
    permissions++;
    return allowed;
  }

  @override
  Future<Position> getCurrentPosition() async {
    positions++;
    return Position(
      latitude: 6.92,
      longitude: 79.86,
      timestamp: DateTime(2026),
      accuracy: 1,
      altitude: 0,
      altitudeAccuracy: 1,
      heading: 0,
      headingAccuracy: 1,
      speed: 0,
      speedAccuracy: 1,
    );
  }
}

Future<void> reachReview(WidgetTester tester) async {
  await tester.enterText(
    find.byType(TextFormField),
    'Headache since this morning',
  );
  await tapVisible(tester, find.text('Continue'));
  await tapVisible(tester, find.text('Review my symptoms'));
}

void main() {
  setUp(() => FlutterSecureStorage.setMockInitialValues({}));
  testWidgets('five-character and 1,000-character descriptions are accepted', (
    tester,
  ) async {
    for (final value in ['12345', 'a' * 1000]) {
      await tester.pumpWidget(
        triageHarness(
          SymptomIntakeScreen(
            key: ValueKey(value.length),
            apiService: FakeTriageApi(),
          ),
        ),
      );
      await tester.enterText(find.byType(TextFormField), value);
      await tapVisible(tester, find.text('Continue'));
      expect(find.text('Step 2 of 3'), findsOneWidget);
    }
  });

  testWidgets(
    'location is never requested until explicitly shared and can be removed',
    (tester) async {
      final api = FakeTriageApi()
        ..submission = Completer<Map<String, dynamic>>();
      final location = FakeLocation();
      await tester.pumpWidget(
        triageHarness(
          SymptomIntakeScreen(apiService: api, locationService: location),
        ),
      );
      await reachReview(tester);
      expect(location.permissions, 0);
      expect(location.positions, 0);
      await tapVisible(tester, find.text('Share current location'));
      expect(location.permissions, 1);
      expect(location.positions, 1);
      await tapVisible(tester, find.text('Remove shared location'));
      final submit = find.text('Assess My Symptoms');
      await tester.ensureVisible(submit);
      await tester.tap(submit);
      await tester.pump();
      expect(api.payload!['latitude'], isNull);
      expect(api.payload!['longitude'], isNull);
      await tester.pumpWidget(const SizedBox());
      api.submission!.complete(assessment());
      await tester.pump();
    },
  );

  testWidgets(
    'shared coordinates and selected additional symptoms reach submission',
    (tester) async {
      final api = FakeTriageApi()
        ..submission = Completer<Map<String, dynamic>>();
      await tester.pumpWidget(
        triageHarness(
          SymptomIntakeScreen(apiService: api, locationService: FakeLocation()),
        ),
      );
      await tester.enterText(find.byType(TextFormField), 'Headache');
      await tapVisible(tester, find.text('Continue'));
      await tapVisible(tester, find.text('Moderate'));
      await tapVisible(tester, find.text('Vomiting'));
      await tapVisible(tester, find.text('Review my symptoms'));
      await tapVisible(tester, find.text('Share current location'));
      await tester.ensureVisible(find.text('Assess My Symptoms'));
      await tester.tap(find.text('Assess My Symptoms'));
      await tester.pump();
      expect(api.payload!['severity'], 'Moderate');
      expect(api.payload!['additionalSymptoms'], ['Vomiting']);
      expect(api.payload!['latitude'], 6.92);
      expect(api.payload!['longitude'], 79.86);
      await tester.pumpWidget(const SizedBox());
      api.submission!.complete(assessment());
      await tester.pump();
    },
  );

  testWidgets(
    'location denial preserves the form and allows ordinary submission',
    (tester) async {
      final location = FakeLocation()..allowed = false;
      await tester.pumpWidget(
        triageHarness(
          SymptomIntakeScreen(
            apiService: FakeTriageApi(),
            locationService: location,
          ),
        ),
      );
      await reachReview(tester);
      await tapVisible(tester, find.text('Share current location'));
      expect(location.positions, 0);
      expect(find.text('Headache since this morning'), findsOneWidget);
      expect(
        tester
            .widget<FilledButton>(
              find.widgetWithText(FilledButton, 'Assess My Symptoms'),
            )
            .onPressed,
        isNotNull,
      );
    },
  );

  testWidgets(
    'network loss requires checking history and explicit resubmission',
    (tester) async {
      final api = FakeTriageApi()
        ..submissionError = http.ClientException('Connection lost');
      await tester.pumpWidget(
        triageHarness(SymptomIntakeScreen(apiService: api)),
      );
      await reachReview(tester);
      await tapVisible(tester, find.text('Assess My Symptoms'));
      expect(api.submissions, 1);
      expect(
        find.textContaining('Your case may already be saved.'),
        findsOneWidget,
      );
      api.submissionError = null;
      await tapVisible(
        tester,
        find.text('I checked my history — no case was saved'),
      );
      expect(api.submissions, 1);
      await tapVisible(tester, find.text('Assess My Symptoms'));
      expect(api.submissions, 2);
      expect(find.byType(TriageStatusScreen), findsOneWidget);
      await tester.pumpWidget(const SizedBox());
    },
  );

  testWidgets(
    'successful forms cannot be accidentally submitted again and offer a fresh form',
    (tester) async {
      final api = FakeTriageApi();
      await tester.pumpWidget(
        triageHarness(SymptomIntakeScreen(apiService: api)),
      );
      await reachReview(tester);
      await tapVisible(tester, find.text('Assess My Symptoms'));
      expect(find.byType(TriageStatusScreen), findsOneWidget);
      await tester.pageBack();
      await tester.pumpAndSettle();
      expect(api.submissions, 1);
      expect(find.text('View your assessment'), findsOneWidget);
      expect(
        tester
            .widget<IconButton>(
              find.ancestor(
                of: find.byTooltip('Edit Symptoms'),
                matching: find.byType(IconButton),
              ),
            )
            .onPressed,
        isNull,
      );
      await tapVisible(tester, find.text('Start a new assessment'));
      expect(find.text('Step 1 of 3'), findsOneWidget);
      expect(
        tester
            .widget<TextFormField>(find.byType(TextFormField))
            .controller!
            .text,
        isEmpty,
      );
      expect(api.submissions, 1);
    },
  );

  testWidgets(
    'Home uses the patient route rather than the launch role picker',
    (tester) async {
      final api = FakeTriageApi();
      await tester.pumpWidget(
        triageHarness(
          TriageStatusScreen(triageData: api.current, apiService: api),
          routes: {
            '/patient': (_) => const Scaffold(body: Text('Patient home')),
          },
        ),
      );
      await tester.pumpAndSettle();
      await tapVisible(tester, find.text('Return to Home'));
      expect(find.text('Patient home'), findsOneWidget);
      expect(find.byType(TriageStatusScreen), findsNothing);
      await tester.pump(const Duration(seconds: 10));
      expect(api.statusCalls, 1);
    },
  );

  testWidgets(
    'small screen, large text and keyboard keep all intake controls scrollable',
    (tester) async {
      tester.view.physicalSize = const Size(320, 568);
      tester.view.devicePixelRatio = 1;
      addTearDown(tester.view.resetPhysicalSize);
      addTearDown(tester.view.resetDevicePixelRatio);
      await tester.pumpWidget(
        triageHarness(
          SymptomIntakeScreen(apiService: FakeTriageApi()),
          textScale: 1.6,
          bottomInset: 200,
        ),
      );
      await tester.enterText(find.byType(TextFormField), 'Headache');
      await tapVisible(tester, find.text('Continue'));
      await tapVisible(tester, find.text('Severe'));
      await tapVisible(tester, find.text('Review my symptoms'));
      await tester.ensureVisible(find.text('Assess My Symptoms'));
      await tester.pumpAndSettle();
      expect(find.byTooltip('Emergency SOS'), findsOneWidget);
      expect(tester.takeException(), isNull);
    },
  );

  testWidgets(
    'small screen and large text allow results, details and SOS access',
    (tester) async {
      tester.view.physicalSize = const Size(320, 568);
      tester.view.devicePixelRatio = 1;
      addTearDown(tester.view.resetPhysicalSize);
      addTearDown(tester.view.resetDevicePixelRatio);
      final api = FakeTriageApi()..current = assessment(level: 'MEDIUM');
      await tester.pumpWidget(
        triageHarness(
          TriageStatusScreen(triageData: api.current, apiService: api),
          textScale: 1.6,
        ),
      );
      await tester.pumpAndSettle();
      await tapVisible(tester, find.text('View Triage Details'));
      expect(find.text('Triage details and history'), findsOneWidget);
      await tapVisible(tester, find.text('Close'));
      await tester.ensureVisible(find.text('Return to Home'));
      expect(find.byTooltip('Emergency SOS'), findsOneWidget);
      expect(tester.takeException(), isNull);
      await tester.pumpWidget(const SizedBox());
    },
  );

  testWidgets(
    'system back moves one intake step and does not affect an inactive tab',
    (tester) async {
      await tester.pumpWidget(triageHarness(const HomeShell()));
      await tester.pumpAndSettle();
      await tapVisible(tester, find.text('Triage'));
      await reachReview(tester);
      await tester.binding.handlePopRoute();
      await tester.pumpAndSettle();
      expect(find.text('Step 2 of 3'), findsOneWidget);
      await tapVisible(tester, find.text('Profile'));
      final scopes = tester.widgetList<PopScope>(
        find.byType(PopScope, skipOffstage: false),
      );
      expect(scopes.every((scope) => scope.canPop), isTrue);
    },
  );
}
