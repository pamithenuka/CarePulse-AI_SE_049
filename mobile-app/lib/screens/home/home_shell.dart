import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../providers/patient_provider.dart';
import '../ai/ai_care_plan_screen.dart';
import '../contacts/emergency_contacts_screen.dart';
import '../documents/documents_screen.dart';
import '../emergency/sos_screen.dart';
import '../history/medical_history_screen.dart';
import '../profile/profile_screen.dart';

/// Bottom-nav shell for the five patient-facing sections. The SOS button is a
/// persistent floating action button (not a tab) so it's reachable in one tap
/// from anywhere in the app, matching how emergency actions are surfaced on
/// the web app's patient detail page.
class HomeShell extends StatefulWidget {
  const HomeShell({super.key});

  @override
  State<HomeShell> createState() => _HomeShellState();
}

class _HomeShellState extends State<HomeShell> {
  int _index = 0;

  static const _tabs = [
    ProfileScreen(),
    MedicalHistoryScreen(),
    EmergencyContactsScreen(),
    DocumentsScreen(),
    AiCarePlanScreen(),
  ];

  static const _destinations = [
    NavigationDestination(icon: Icon(Icons.person_outline), selectedIcon: Icon(Icons.person), label: 'Profile'),
    NavigationDestination(icon: Icon(Icons.history_outlined), selectedIcon: Icon(Icons.history), label: 'History'),
    NavigationDestination(icon: Icon(Icons.contacts_outlined), selectedIcon: Icon(Icons.contacts), label: 'Contacts'),
    NavigationDestination(icon: Icon(Icons.folder_outlined), selectedIcon: Icon(Icons.folder), label: 'Documents'),
    NavigationDestination(icon: Icon(Icons.smart_toy_outlined), selectedIcon: Icon(Icons.smart_toy), label: 'AI Plan'),
  ];

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: IndexedStack(index: _index, children: _tabs),
      // startFloat (bottom-left) deliberately, not the default endFloat: History,
      // Contacts and Documents each nest their own Scaffold with an "add" FAB at
      // the default bottom-right corner, and this outer FAB would otherwise paint
      // directly on top of - and swallow every tap intended for - those buttons.
      floatingActionButtonLocation: FloatingActionButtonLocation.startFloat,
      floatingActionButton: FloatingActionButton.extended(
        onPressed: () => Navigator.of(context).push(MaterialPageRoute(builder: (_) => const SosScreen())),
        backgroundColor: Theme.of(context).colorScheme.error,
        foregroundColor: Theme.of(context).colorScheme.onError,
        icon: const Icon(Icons.sos),
        label: const Text('SOS'),
      ),
      bottomNavigationBar: NavigationBar(
        selectedIndex: _index,
        onDestinationSelected: (i) {
          setState(() => _index = i);
          if (i == 4) context.read<PatientProvider>().loadAiPlans();
        },
        destinations: _destinations,
      ),
    );
  }
}
