// Basic widget test for the CarePulse patient app.
//
// This confirms the app boots correctly and the Doctor Search screen (the
// entry point patients see first) renders its key UI elements. Deliberately
// does NOT depend on a live backend response - the app bar title renders
// immediately regardless of whether the doctor list finished loading, so
// this test passes reliably whether or not backend-api happens to be
// running when `flutter test` executes.

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:mobile_app/main.dart';

void main() {
  testWidgets('App boots and shows the Find a doctor screen', (WidgetTester tester) async {
    await tester.pumpWidget(const CarePulsePatientApp());

    // Let the initial frame render (don't wait for network calls to
    // settle - we're only checking the static shell here).
    await tester.pump();

    // The app bar title should always be visible immediately, regardless
    // of whether the doctor list has finished loading, errored, or is
    // still in progress - this is the one thing guaranteed to be true.
    expect(find.text('Find a doctor'), findsOneWidget);
  });
}
