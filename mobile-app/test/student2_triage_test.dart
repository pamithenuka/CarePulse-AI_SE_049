import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/screens/symptom_intake_screen.dart';

void main() {
  group('Student 2 - Symptom Intake Screen Validation Tests', () {
    testWidgets('Empty symptoms show validation snackbar', (WidgetTester tester) async {
      await tester.pumpWidget(const MaterialApp(
        home: SymptomIntakeScreen(),
      ));

      final buttonFinder = find.text('SUBMIT SYMPTOMS');
      await tester.ensureVisible(buttonFinder);
      await tester.pumpAndSettle();
      
      await tester.tap(buttonFinder);
      await tester.pump();

      expect(find.text('Please describe your symptoms.'), findsOneWidget);
    });

    testWidgets('Initial values are set correctly', (WidgetTester tester) async {
      await tester.pumpWidget(const MaterialApp(
        home: SymptomIntakeScreen(),
      ));

      expect(find.text('What are you experiencing?'), findsOneWidget);
      expect(find.text('Since this morning'), findsOneWidget);
      expect(find.text('Mild'), findsOneWidget);
    });
  });
}
