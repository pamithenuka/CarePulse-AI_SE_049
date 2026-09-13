import 'package:flutter/material.dart';
import 'features/dispatch/screens/nurse_login_screen.dart';
import 'features/dispatch/screens/dispatch_dashboard_screen.dart';
import 'features/dispatch/screens/navigation_screen.dart';
import 'features/dispatch/screens/vitals_entry_screen.dart';

void main() {
  runApp(const CarePulseNurseApp());
}

class CarePulseNurseApp extends StatelessWidget {
  const CarePulseNurseApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'CarePulse Nurse',
      debugShowCheckedModeBanner: false,
      theme: ThemeData(
        brightness: Brightness.dark,
        colorScheme: ColorScheme.fromSeed(
          seedColor: const Color(0xFF3B82F6),
          brightness: Brightness.dark,
        ),
        scaffoldBackgroundColor: const Color(0xFF0F172A),
        fontFamily: 'Roboto',
      ),
      initialRoute: '/login',
      onGenerateRoute: (settings) {
        switch (settings.name) {
          case '/login':
            return MaterialPageRoute(builder: (_) => const NurseLoginScreen());
          case '/dispatch-dashboard':
            return MaterialPageRoute(builder: (_) => const DispatchDashboardScreen());
          case '/navigation':
            final dispatch = settings.arguments as Map<String, dynamic>;
            return MaterialPageRoute(builder: (_) => NavigationScreen(dispatch: dispatch));
          case '/vitals-entry':
            final dispatch = settings.arguments as Map<String, dynamic>;
            return MaterialPageRoute(builder: (_) => VitalsEntryScreen(dispatch: dispatch));
          default:
            return MaterialPageRoute(builder: (_) => const NurseLoginScreen());
        }
      },
    );
  }
}

