import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../providers/patient_provider.dart';
import '../../theme/app_theme.dart';
import '../ai/ai_care_plan_screen.dart';
import '../contacts/emergency_contacts_screen.dart';
import '../documents/documents_screen.dart';
import '../emergency/sos_screen.dart';
import '../history/medical_history_screen.dart';
import '../profile/profile_screen.dart';
import '../../screens/symptom_intake_screen.dart';
import '../../screens/doctor_search_screen.dart';
import '../../widgets/tab_fade_in.dart';

/// Primary patient actions stay visible; secondary sections live in More.
class HomeShell extends StatefulWidget {
  const HomeShell({super.key});

  @override
  State<HomeShell> createState() => _HomeShellState();
}

class _HomeShellState extends State<HomeShell> {
  int _index = 0;
  final _scaffoldKey = GlobalKey<ScaffoldState>();

  void _selectSection(int index) {
    setState(() => _index = index);
    if (index == 4) context.read<PatientProvider>().loadAiPlans();
  }

  static const _tabs = [
    ProfileScreen(),
    MedicalHistoryScreen(),
    EmergencyContactsScreen(),
    DocumentsScreen(),
    AiCarePlanScreen(),
    SymptomIntakeScreen(),
    DoctorSearchScreen(),
  ];

  static const _destinations = [
    NavigationDestination(icon: Icon(Icons.person_outline), selectedIcon: Icon(Icons.person), label: 'Profile'),
    NavigationDestination(icon: Icon(Icons.medical_services_outlined), selectedIcon: Icon(Icons.medical_services), label: 'Triage'),
    NavigationDestination(icon: Icon(Icons.local_hospital_outlined), selectedIcon: Icon(Icons.local_hospital), label: 'Doctors'),
    NavigationDestination(icon: Icon(Icons.menu), label: 'More'),
  ];

  Widget _drawerItem(int index, IconData icon, String label) {
    return ListTile(
      leading: Container(
        padding: const EdgeInsets.all(10),
        decoration: BoxDecoration(
          color: AppColors.primary.withValues(alpha: 0.12),
          borderRadius: BorderRadius.circular(12),
        ),
        child: Icon(icon, color: AppColors.primary),
      ),
      title: Text(label),
      selected: _index == index,
      selectedTileColor: Theme.of(context).colorScheme.primaryContainer,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
      onTap: () {
        _scaffoldKey.currentState?.closeDrawer();
        _selectSection(index);
      },
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      key: _scaffoldKey,
      drawer: Drawer(
        child: SafeArea(
          child: ListView(
            padding: const EdgeInsets.all(16),
            children: [
              Container(
                padding: const EdgeInsets.all(18),
                decoration: BoxDecoration(
                  borderRadius: BorderRadius.circular(20),
                  gradient: const LinearGradient(
                    begin: Alignment.topLeft,
                    end: Alignment.bottomRight,
                    colors: [AppColors.deep, AppColors.primary],
                  ),
                ),
                child: Row(children: [
                  Container(
                    padding: const EdgeInsets.all(10),
                    decoration: BoxDecoration(color: Colors.white.withValues(alpha: 0.16), borderRadius: BorderRadius.circular(14)),
                    child: const Icon(Icons.favorite_rounded, color: Colors.white),
                  ),
                  const SizedBox(width: 14),
                  Expanded(
                    child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                      Text('Your care', style: Theme.of(context).textTheme.titleLarge?.copyWith(color: Colors.white)),
                      const SizedBox(height: 2),
                      Text('Records, contacts and your care plan',
                          style: Theme.of(context).textTheme.bodySmall?.copyWith(color: Colors.white70)),
                    ]),
                  ),
                  IconButton(
                    tooltip: 'Close menu',
                    icon: const Icon(Icons.close, color: Colors.white),
                    onPressed: () => _scaffoldKey.currentState?.closeDrawer(),
                  ),
                ]),
              ),
              const SizedBox(height: 16),
              _drawerItem(1, Icons.history_outlined, 'Medical History'),
              _drawerItem(2, Icons.contacts_outlined, 'Emergency Contacts'),
              _drawerItem(3, Icons.folder_outlined, 'Documents'),
              _drawerItem(4, Icons.smart_toy_outlined, 'AI Care Plan'),
            ],
          ),
        ),
      ),
      body: IndexedStack(index: _index, children: [
        for (var i = 0; i < _tabs.length; i++)
          Theme(
            data: buildAppTheme(),
            child: TabFadeIn(
              active: _index == i,
              child: i == 5 ? SymptomIntakeScreen(isActive: _index == 5) : _tabs[i],
            ),
          ),
      ]),
      // startFloat (bottom-left) deliberately, not the default endFloat: History,
      // Contacts and Documents each nest their own Scaffold with an "add" FAB at
      // the default bottom-right corner, and this outer FAB would otherwise paint
      // directly on top of - and swallow every tap intended for - those buttons.
      floatingActionButtonLocation: FloatingActionButtonLocation.startFloat,
      // Triage provides SOS in its app bar, outside the scrollable form.
      // Keep the existing floating access on the other patient sections.
      floatingActionButton: _index == 5 ? null : FloatingActionButton.extended(
        onPressed: () => Navigator.of(context).push(MaterialPageRoute(builder: (_) => const SosScreen())),
        backgroundColor: Theme.of(context).colorScheme.error,
        foregroundColor: Theme.of(context).colorScheme.onError,
        icon: const Icon(Icons.sos),
        label: const Text('SOS'),
      ),
      bottomNavigationBar: NavigationBar(
        selectedIndex: _index == 0 ? 0 : _index == 5 ? 1 : _index == 6 ? 2 : 3,
        onDestinationSelected: (index) {
          if (index == 3) {
            _scaffoldKey.currentState?.openDrawer();
          } else {
            _selectSection([0, 5, 6][index]);
          }
        },
        destinations: _destinations,
      ),
    );
  }
}
