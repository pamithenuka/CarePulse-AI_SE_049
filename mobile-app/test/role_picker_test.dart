import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:mobile_app/screens/app_entry_screen.dart';
import 'package:mobile_app/screens/role_picker_screen.dart';

/// A minimal named-route table so AppEntryScreen's pushReplacementNamed calls
/// resolve to something visible, without pulling in the real Patient/Nurse
/// screens (and their own provider/network dependencies) into this test.
Widget _appWithFakeRoutes() {
  return MaterialApp(
    initialRoute: '/',
    onGenerateRoute: (settings) => MaterialPageRoute(
      builder: (_) => settings.name == '/'
          ? const AppEntryScreen()
          : Scaffold(body: Text('route:${settings.name}')),
    ),
  );
}

void main() {
  group('RolePickerScreen', () {
    testWidgets('shows both persona options', (WidgetTester tester) async {
      await tester.pumpWidget(MaterialApp(home: RolePickerScreen(onRoleSelected: (_) {})));

      expect(find.text('Patient'), findsOneWidget);
      expect(find.text('Field Nurse'), findsOneWidget);
      expect(find.text('Doctor'), findsNothing);
    });

    testWidgets('tapping Patient reports the patient role', (WidgetTester tester) async {
      String? selected;
      await tester.pumpWidget(MaterialApp(home: RolePickerScreen(onRoleSelected: (role) => selected = role)));

      await tester.tap(find.text('Patient'));
      await tester.pump();

      expect(selected, 'patient');
    });

    testWidgets('tapping Field Nurse reports the nurse role', (WidgetTester tester) async {
      String? selected;
      await tester.pumpWidget(MaterialApp(home: RolePickerScreen(onRoleSelected: (role) => selected = role)));

      await tester.tap(find.text('Field Nurse'));
      await tester.pump();

      expect(selected, 'nurse');
    });
  });

  group('AppEntryScreen', () {
    testWidgets('always shows the role picker on launch', (WidgetTester tester) async {
      await tester.pumpWidget(_appWithFakeRoutes());

      expect(find.byType(RolePickerScreen), findsOneWidget);
    });

    testWidgets('selecting Field Nurse navigates to the /login route', (WidgetTester tester) async {
      await tester.pumpWidget(_appWithFakeRoutes());

      await tester.tap(find.text('Field Nurse'));
      await tester.pumpAndSettle();

      expect(find.text('route:/login'), findsOneWidget);
    });

    testWidgets('selecting Patient navigates to the /patient route', (WidgetTester tester) async {
      await tester.pumpWidget(_appWithFakeRoutes());

      await tester.tap(find.text('Patient'));
      await tester.pumpAndSettle();

      expect(find.text('route:/patient'), findsOneWidget);
    });
  });
}
