import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:mobile_app/main.dart';

void main() {
  testWidgets('CarePulse Nurse app smoke test', (WidgetTester tester) async {
    // Build our app and trigger a frame.
    await tester.pumpWidget(const CarePulseNurseApp());

    // Verify that the login screen renders with CarePulse branding.
    expect(find.text('CarePulse'), findsOneWidget);
    expect(find.text('Field Nurse Portal'), findsOneWidget);
    expect(find.text('Sign In'), findsOneWidget);
  });
}

