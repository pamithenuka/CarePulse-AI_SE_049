import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:mobile_app/providers/auth_provider.dart';
import 'package:mobile_app/screens/auth/login_screen.dart';
import 'package:mobile_app/screens/auth/register_screen.dart';

/// Constructing AuthProvider() here does no I/O (see AuthProvider's
/// constructor) - only login()/register()/tryAutoLogin() touch the network or
/// secure storage, none of which this test triggers, since an empty-form
/// submit is rejected by Form validation before AuthProvider is ever called.
Widget _wrapWithProviders(Widget child) {
  return ChangeNotifierProvider(
    create: (_) => AuthProvider(),
    child: MaterialApp(home: child),
  );
}

void main() {
  testWidgets('LoginScreen shows validation errors on an empty submit', (WidgetTester tester) async {
    await tester.pumpWidget(_wrapWithProviders(const LoginScreen()));

    await tester.tap(find.widgetWithText(FilledButton, 'Log in'));
    await tester.pump();

    expect(find.text('Enter a valid email address.'), findsOneWidget);
    expect(find.text('Password is required.'), findsOneWidget);
  });

  testWidgets('LoginScreen renders the CarePulse branding and a register link', (WidgetTester tester) async {
    await tester.pumpWidget(_wrapWithProviders(const LoginScreen()));

    expect(find.text('CarePulse'), findsOneWidget);
    expect(find.text("Don't have an account? Register"), findsOneWidget);
  });

  testWidgets('tapping the register link navigates to RegisterScreen', (WidgetTester tester) async {
    await tester.pumpWidget(_wrapWithProviders(const LoginScreen()));

    expect(find.byType(RegisterScreen), findsNothing);

    await tester.tap(find.text("Don't have an account? Register"));
    await tester.pumpAndSettle();

    expect(find.byType(RegisterScreen), findsOneWidget);
    expect(find.text('Create your account'), findsOneWidget);
  });
}
