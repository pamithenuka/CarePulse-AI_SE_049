import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../providers/auth_provider.dart';
import '../../utils/validators.dart';
import 'register_screen.dart';

/// Patient sign-in. Mirrors the Field Nurse sign-in layout so both roles feel the same.
class LoginScreen extends StatefulWidget {
  const LoginScreen({super.key});

  @override
  State<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends State<LoginScreen> {
  static const _teal = Color(0xFF0F766E);
  static const _deepTeal = Color(0xFF0B5F6B);
  static const _border = Color(0xFFE2E8F0);
  static const _hint = Color(0xFF94A3B8);

  final _formKey = GlobalKey<FormState>();
  final _emailController = TextEditingController();
  final _passwordController = TextEditingController();
  bool _obscurePassword = true;

  @override
  void dispose() {
    _emailController.dispose();
    _passwordController.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;
    final auth = context.read<AuthProvider>();
    await auth.login(_emailController.text.trim(), _passwordController.text);
  }

  InputDecoration _decoration({required String hint, required IconData icon, Widget? suffix}) {
    OutlineInputBorder border([Color color = _border, double width = 1]) => OutlineInputBorder(
          borderRadius: BorderRadius.circular(14),
          borderSide: BorderSide(color: color, width: width),
        );
    return InputDecoration(
      hintText: hint,
      hintStyle: const TextStyle(color: _hint),
      prefixIcon: Icon(icon, color: _hint),
      suffixIcon: suffix,
      filled: true,
      fillColor: Colors.white,
      border: border(),
      enabledBorder: border(),
      focusedBorder: border(_teal, 2),
    );
  }

  @override
  Widget build(BuildContext context) {
    final auth = context.watch<AuthProvider>();

    return Scaffold(
      backgroundColor: const Color(0xFFF4F6F8),
      body: SafeArea(
        child: Center(
          child: SingleChildScrollView(
            padding: const EdgeInsets.symmetric(horizontal: 32, vertical: 24),
            child: ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 440),
              child: Form(
                key: _formKey,
                child: Column(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    Container(
                      padding: const EdgeInsets.all(20),
                      decoration: BoxDecoration(
                        color: _teal.withValues(alpha: 0.1),
                        borderRadius: BorderRadius.circular(20),
                      ),
                      child: const Icon(Icons.person_rounded, color: _teal, size: 48),
                    ),
                    const SizedBox(height: 24),
                    const Text(
                      'CarePulse',
                      style: TextStyle(fontSize: 32, fontWeight: FontWeight.w700, color: _deepTeal, letterSpacing: -1),
                    ),
                    const SizedBox(height: 4),
                    const Text('Patient Portal', style: TextStyle(fontSize: 16, color: Color(0xFF64748B))),
                    const SizedBox(height: 48),
                    TextFormField(
                      controller: _emailController,
                      keyboardType: TextInputType.emailAddress,
                      style: const TextStyle(color: Color(0xFF334155)),
                      decoration: _decoration(hint: 'Email Address', icon: Icons.email_outlined),
                      validator: Validators.email,
                    ),
                    const SizedBox(height: 16),
                    TextFormField(
                      controller: _passwordController,
                      obscureText: _obscurePassword,
                      style: const TextStyle(color: Color(0xFF334155)),
                      decoration: _decoration(
                        hint: 'Password',
                        icon: Icons.lock_outline,
                        suffix: IconButton(
                          tooltip: _obscurePassword ? 'Show password' : 'Hide password',
                          icon: Icon(_obscurePassword ? Icons.visibility_off : Icons.visibility, color: _hint),
                          onPressed: () => setState(() => _obscurePassword = !_obscurePassword),
                        ),
                      ),
                      validator: (v) => (v == null || v.isEmpty) ? 'Password is required.' : null,
                      onFieldSubmitted: (_) => _submit(),
                    ),
                    if (auth.error != null) ...[
                      const SizedBox(height: 16),
                      Container(
                        width: double.infinity,
                        padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
                        decoration: BoxDecoration(
                          color: const Color(0xFFFDECEA),
                          borderRadius: BorderRadius.circular(12),
                          border: Border.all(color: const Color(0xFFF3C5C1)),
                        ),
                        child: Text(
                          auth.error!,
                          textAlign: TextAlign.center,
                          style: const TextStyle(color: Color(0xFF8F2520), fontWeight: FontWeight.w500),
                        ),
                      ),
                    ],
                    const SizedBox(height: 32),
                    SizedBox(
                      width: double.infinity,
                      height: 52,
                      child: FilledButton(
                        onPressed: auth.isLoading ? null : _submit,
                        style: FilledButton.styleFrom(
                          backgroundColor: _deepTeal,
                          foregroundColor: Colors.white,
                          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(14)),
                          elevation: 0,
                        ),
                        child: auth.isLoading
                            ? const SizedBox(
                                width: 24,
                                height: 24,
                                child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white),
                              )
                            : const Text('Sign In', style: TextStyle(fontSize: 16, fontWeight: FontWeight.w600)),
                      ),
                    ),
                    const SizedBox(height: 12),
                    TextButton(
                      onPressed: auth.isLoading
                          ? null
                          : () => Navigator.of(context).push(MaterialPageRoute(builder: (_) => const RegisterScreen())),
                      child: const Text(
                        "Don't have an account? Register",
                        style: TextStyle(fontSize: 13, color: _teal),
                      ),
                    ),
                    TextButton(
                      onPressed: () => Navigator.of(context).pushNamedAndRemoveUntil('/', (route) => false),
                      child: const Text(
                        'Not a Patient? Switch role',
                        style: TextStyle(fontSize: 13, color: _teal),
                      ),
                    ),
                  ],
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }
}
