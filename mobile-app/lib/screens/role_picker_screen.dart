import 'package:flutter/material.dart';

/// Shown on every launch (see AppEntryScreen) and from each login screen's
/// "Switch role" link, since this single app binary serves two different
/// personas sharing one device: Patients and Field Nurses. Doctors are
/// web-admin only (login + AI Care Plan review happen there instead).
class RolePickerScreen extends StatelessWidget {
  final ValueChanged<String> onRoleSelected;

  const RolePickerScreen({super.key, required this.onRoleSelected});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: SafeArea(
        child: Center(
          child: Padding(
            padding: const EdgeInsets.all(24),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Icon(Icons.favorite, size: 56, color: Theme.of(context).colorScheme.primary),
                const SizedBox(height: 12),
                Text(
                  'CarePulse',
                  textAlign: TextAlign.center,
                  style: Theme.of(context).textTheme.headlineMedium?.copyWith(fontWeight: FontWeight.bold),
                ),
                const SizedBox(height: 4),
                Text(
                  'Who\'s using this device?',
                  textAlign: TextAlign.center,
                  style: Theme.of(context).textTheme.bodyMedium,
                ),
                const SizedBox(height: 32),
                _RoleCard(
                  icon: Icons.person_outline,
                  title: 'Patient',
                  subtitle: 'Manage your profile, records and care plan',
                  onTap: () => onRoleSelected('patient'),
                ),
                const SizedBox(height: 16),
                _RoleCard(
                  icon: Icons.medical_services_outlined,
                  title: 'Field Nurse',
                  subtitle: 'Dispatch, navigation and on-site vitals',
                  onTap: () => onRoleSelected('nurse'),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

class _RoleCard extends StatelessWidget {
  final IconData icon;
  final String title;
  final String subtitle;
  final VoidCallback onTap;

  const _RoleCard({required this.icon, required this.title, required this.subtitle, required this.onTap});

  @override
  Widget build(BuildContext context) {
    return Card(
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(12),
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Row(
            children: [
              Icon(icon, size: 32, color: Theme.of(context).colorScheme.primary),
              const SizedBox(width: 16),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(title, style: Theme.of(context).textTheme.titleMedium),
                    Text(subtitle, style: Theme.of(context).textTheme.bodySmall),
                  ],
                ),
              ),
              const Icon(Icons.chevron_right),
            ],
          ),
        ),
      ),
    );
  }
}
