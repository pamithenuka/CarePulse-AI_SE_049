import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'providers/auth_provider.dart';
import 'providers/patient_provider.dart';
import 'screens/root_screen.dart';

import 'features/dispatch/screens/nurse_login_screen.dart';
import 'features/dispatch/screens/dispatch_dashboard_screen.dart';
import 'features/dispatch/screens/navigation_screen.dart';
import 'features/dispatch/screens/vitals_entry_screen.dart';

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
        theme: ThemeData(
          colorScheme: ColorScheme.fromSeed(seedColor: const Color(0xFF0F766E)),
          useMaterial3: true,
          appBarTheme: const AppBarTheme(centerTitle: false),
          inputDecorationTheme: const InputDecorationTheme(border: OutlineInputBorder()),
        ),
        initialRoute: '/login',
        onGenerateRoute: (settings) {
          switch (settings.name) {
            case '/':
              return MaterialPageRoute(builder: (_) => const RootScreen());
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
              return MaterialPageRoute(builder: (_) => const RootScreen());
          }
        },
      ),
    );
  }
}

