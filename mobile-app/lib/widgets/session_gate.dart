import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../providers/auth_provider.dart';

class SessionGate extends StatelessWidget {
  final String role;
  final Widget child;
  const SessionGate({super.key, required this.role, required this.child});
  @override
  Widget build(BuildContext context) {
    final auth = context.watch<AuthProvider>();
    if (auth.status == AuthStatus.unknown) return const Scaffold(body: Center(child: CircularProgressIndicator()));
    if (auth.isAuthenticated && (auth.currentUser?.roles.contains(role) ?? false)) return child;
    return Scaffold(appBar: AppBar(title: const Text('Sign in required')), body: Center(child: ElevatedButton(
      onPressed: () => Navigator.pushReplacementNamed(context, role == 'Nurse' ? '/login' : '/patient'),
      child: Text('Sign in as $role'),
    )));
  }
}
