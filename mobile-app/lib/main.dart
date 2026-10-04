import 'package:flutter/material.dart';
import 'theme/app_theme.dart';
import 'package:provider/provider.dart';
import 'providers/auth_provider.dart';
import 'providers/patient_provider.dart';
import 'screens/app_entry_screen.dart';
import 'widgets/session_gate.dart';
import 'screens/root_screen.dart';

import 'features/dispatch/screens/nurse_login_screen.dart';
import 'features/dispatch/screens/dispatch_dashboard_screen.dart';
import 'features/dispatch/screens/navigation_screen.dart';
import 'features/dispatch/screens/vitals_entry_screen.dart';
import 'screens/doctor_search_screen.dart';

void main() {
  runApp(const CarePulseApp());
}

class CarePulseApp extends StatelessWidget {
  const CarePulseApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MultiProvider(
      providers: [
        ChangeNotifierProvider(create: (_) => AuthProvider()..tryAutoLogin()),
        ChangeNotifierProxyProvider<AuthProvider, PatientProvider>(
          create: (context) => PatientProvider(context.read<AuthProvider>()),
          update: (context, auth, previous) => (previous ?? PatientProvider(auth))..updateAuth(auth),
        ),
      ],
      child: MaterialApp(
        title: 'CarePulse',
        debugShowCheckedModeBanner: false,
        theme: buildAppTheme(),
        builder: (context, child) => DecoratedBox(
          decoration: const BoxDecoration(
            gradient: LinearGradient(
              begin: Alignment.topLeft,
              end: Alignment.bottomRight,
              colors: [Color(0xFFC7E9E6), Color(0xFFDAE6F5), Color(0xFFD9D6F0)],
            ),
          ),
          child: child ?? const SizedBox.shrink(),
        ),
        initialRoute: '/',
        onGenerateRoute: (settings) {
          switch (settings.name) {
            case '/':
              return MaterialPageRoute(builder: (_) => const AppEntryScreen());
            case '/patient':
              return MaterialPageRoute(builder: (_) => const RootScreen());
            case '/doctors':
              return MaterialPageRoute(builder: (_) => const SessionGate(role: 'Patient', child: DoctorSearchScreen()));
            case '/login':
              return MaterialPageRoute(builder: (_) => const NurseLoginScreen());
            case '/dispatch-dashboard':
              return MaterialPageRoute(builder: (_) => const SessionGate(role: 'Nurse', child: DispatchDashboardScreen()));
            case '/navigation':
              final dispatch = settings.arguments as Map<String, dynamic>;
              return MaterialPageRoute(builder: (_) => SessionGate(role: 'Nurse', child: NavigationScreen(dispatch: dispatch)));
            case '/vitals-entry':
              final dispatch = settings.arguments as Map<String, dynamic>;
              return MaterialPageRoute(builder: (_) => SessionGate(role: 'Nurse', child: VitalsEntryScreen(dispatch: dispatch)));
            default:
              return MaterialPageRoute(builder: (_) => const AppEntryScreen());
          }
        },
      ),
    );
  }
}

