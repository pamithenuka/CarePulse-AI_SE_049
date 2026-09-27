import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../providers/auth_provider.dart';
import '../providers/patient_provider.dart';
import '../widgets/loading_view.dart';
import 'auth/login_screen.dart';
import 'home/home_shell.dart';
import 'onboarding/profile_onboarding_screen.dart';

/// Top-level router: picks between the login flow, the one-time profile
/// onboarding form, and the main app shell based on session + profile state.
class RootScreen extends StatelessWidget {
  const RootScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final auth = context.watch<AuthProvider>();

    switch (auth.status) {
      case AuthStatus.unknown:
        return const Scaffold(body: LoadingView(message: 'Loading CarePulse...'));
      case AuthStatus.unauthenticated:
        return const LoginScreen();
      case AuthStatus.authenticated:
        return const _AuthenticatedGate();
    }
  }
}

class _AuthenticatedGate extends StatefulWidget {
  const _AuthenticatedGate();

  @override
  State<_AuthenticatedGate> createState() => _AuthenticatedGateState();
}

class _AuthenticatedGateState extends State<_AuthenticatedGate> {
  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      context.read<PatientProvider>().loadMyProfile();
    });
  }

  @override
  Widget build(BuildContext context) {
    final patient = context.watch<PatientProvider>();

    if (patient.isLoading && patient.profile == null && !patient.hasNoProfileYet) {
      return const Scaffold(body: LoadingView(message: 'Loading your profile...'));
    }
    if (patient.hasNoProfileYet) {
      return const ProfileOnboardingScreen();
    }
    if (patient.profile == null) {
      return Scaffold(
        body: Center(
          child: Padding(
            padding: const EdgeInsets.all(24),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                Text(patient.error ?? 'Something went wrong loading your profile.', textAlign: TextAlign.center),
                const SizedBox(height: 16),
                FilledButton(
                  onPressed: () => context.read<PatientProvider>().loadMyProfile(),
                  child: const Text('Try again'),
                ),
              ],
            ),
          ),
        ),
      );
    }
    return const HomeShell();
  }
}
