import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'providers/auth_provider.dart';
import 'providers/patient_provider.dart';
import 'screens/root_screen.dart';

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
        home: const RootScreen(),
      ),
    );
  }
}
