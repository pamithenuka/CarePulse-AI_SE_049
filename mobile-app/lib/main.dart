import 'package:flutter/material.dart';
import 'screens/doctor_search_screen.dart';
import 'theme/app_theme.dart';

void main() {
  runApp(const CarePulsePatientApp());
}

class CarePulsePatientApp extends StatelessWidget {
  const CarePulsePatientApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'CarePulse',
      debugShowCheckedModeBanner: false,
      theme: buildAppTheme(),
      home: const DoctorSearchScreen(),
    );
  }
}
