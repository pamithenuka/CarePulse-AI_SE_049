import 'package:flutter/material.dart';

import 'role_picker_screen.dart';

/// The app's initial route. Always shows the persona picker - this single
/// app binary serves two personas (Patient, Field Nurse) sharing one device,
/// so the picker must stay reachable on every launch rather than locking a
/// device to whichever persona logged in first.
class AppEntryScreen extends StatelessWidget {
  const AppEntryScreen({super.key});

  void _selectRole(BuildContext context, String role) {
    Navigator.of(context).pushReplacementNamed(_routeForRole(role));
  }

  String _routeForRole(String role) {
    switch (role) {
      case 'patient':
        return '/patient';
      case 'nurse':
      default:
        return '/login';
    }
  }

  @override
  Widget build(BuildContext context) {
    return RolePickerScreen(onRoleSelected: (role) => _selectRole(context, role));
  }
}
