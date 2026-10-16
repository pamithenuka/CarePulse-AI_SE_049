import 'dart:math' as math;

import 'package:flutter/material.dart';

import '../theme/app_theme.dart';
import '../widgets/ecg_painter.dart';
import '../widgets/fade_slide_in.dart';

/// Shown on every launch (see AppEntryScreen) and from each login screen's
/// "Switch role" link, since this single app binary serves two different
/// personas sharing one device: Patients and Field Nurses. Doctors are
/// web-admin only (login + AI Care Plan review happen there instead).
class RolePickerScreen extends StatefulWidget {
  final ValueChanged<String> onRoleSelected;

  const RolePickerScreen({super.key, required this.onRoleSelected});

  @override
  State<RolePickerScreen> createState() => _RolePickerScreenState();
}

class _RolePickerScreenState extends State<RolePickerScreen> with SingleTickerProviderStateMixin {
  // One slow looping clock drives the aurora, the logo pulse and the heartbeat line.
  late final AnimationController _clock = AnimationController(vsync: this, duration: const Duration(seconds: 20));

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    final reduce = MediaQuery.maybeOf(context)?.disableAnimations ?? false;
    if (reduce) {
      _clock.stop();
    } else if (!_clock.isAnimating) {
      _clock.repeat();
    }
  }

  @override
  void dispose() {
    _clock.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: AnimatedBuilder(
        animation: _clock,
        builder: (context, child) {
          final t = _clock.value;
          final wave = math.sin(t * 2 * math.pi);
          final wave2 = math.cos(t * 2 * math.pi);
          return Stack(
            children: [
              // Soft aurora glow drifting behind everything.
              _Glow(color: const Color(0xFF17B3A8), size: 520, alignment: Alignment(-0.9 + 0.12 * wave, -0.85 + 0.08 * wave2), opacity: 0.30),
              _Glow(color: const Color(0xFF2F7FD1), size: 460, alignment: Alignment(1.0 - 0.12 * wave2, -0.2 + 0.1 * wave), opacity: 0.20),
              _Glow(color: const Color(0xFF7BE0D3), size: 420, alignment: Alignment(0.2 + 0.15 * wave, 1.05 - 0.08 * wave2), opacity: 0.26),
              // A faint heartbeat line along the bottom edge.
              Positioned(
                left: 0,
                right: 0,
                bottom: 56,
                height: 56,
                child: IgnorePointer(
                  child: Opacity(
                    opacity: 0.55,
                    child: CustomPaint(painter: EcgPainter(progress: (t * 4) % 1, color: AppColors.primary)),
                  ),
                ),
              ),
              child!,
              Positioned(
                left: 0,
                right: 0,
                bottom: 14,
                child: IgnorePointer(
                  child: FadeSlideIn(
                    delay: const Duration(milliseconds: 700),
                    child: Text(
                      'Secure, role-based access',
                      textAlign: TextAlign.center,
                      style: Theme.of(context).textTheme.bodySmall?.copyWith(color: AppColors.muted, letterSpacing: 0.4),
                    ),
                  ),
                ),
              ),
            ],
          );
        },
        child: SafeArea(
          child: Center(
            child: SingleChildScrollView(
              padding: const EdgeInsets.fromLTRB(24, 24, 24, 90),
              child: ConstrainedBox(
                constraints: const BoxConstraints(maxWidth: 460),
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    FadeSlideIn(
                      offset: const Offset(0, -0.25),
                      duration: const Duration(milliseconds: 700),
                      child: Center(child: _PulsingLogo(clock: _clock)),
                    ),
                    const SizedBox(height: 22),
                    FadeSlideIn(
                      delay: const Duration(milliseconds: 120),
                      child: Text('CarePulse', textAlign: TextAlign.center, style: Theme.of(context).textTheme.headlineMedium),
                    ),
                    const SizedBox(height: 6),
                    FadeSlideIn(
                      delay: const Duration(milliseconds: 200),
                      child: Text(
                        'Who\'s using this device?',
                        textAlign: TextAlign.center,
                        style: Theme.of(context).textTheme.bodyLarge?.copyWith(color: AppColors.muted),
                      ),
                    ),
                    const SizedBox(height: 34),
                    FadeSlideIn(
                      delay: const Duration(milliseconds: 300),
                      offset: const Offset(-0.06, 0.1),
                      child: _RoleCard(
                        icon: Icons.person_outline_rounded,
                        title: 'Patient',
                        subtitle: 'Manage your profile, records and care plan',
                        onTap: () => widget.onRoleSelected('patient'),
                      ),
                    ),
                    const SizedBox(height: 16),
                    FadeSlideIn(
                      delay: const Duration(milliseconds: 400),
                      offset: const Offset(0.06, 0.1),
                      child: _RoleCard(
                        icon: Icons.medical_services_outlined,
                        title: 'Field Nurse',
                        subtitle: 'Dispatch, navigation and on-site vitals',
                        onTap: () => widget.onRoleSelected('nurse'),
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

class _Glow extends StatelessWidget {
  const _Glow({required this.color, required this.size, required this.alignment, required this.opacity});

  final Color color;
  final double size;
  final Alignment alignment;
  final double opacity;

  @override
  Widget build(BuildContext context) {
    return Align(
      alignment: alignment,
      child: IgnorePointer(
        child: Container(
          width: size,
          height: size,
          decoration: BoxDecoration(
            shape: BoxShape.circle,
            gradient: RadialGradient(colors: [color.withValues(alpha: opacity), color.withValues(alpha: 0)]),
          ),
        ),
      ),
    );
  }
}

/// The logo badge floats gently while a ring pulses outward like a heartbeat.
class _PulsingLogo extends StatelessWidget {
  const _PulsingLogo({required this.clock});

  final Animation<double> clock;

  @override
  Widget build(BuildContext context) {
    final phase = (clock.value * 20 / 2.6) % 1; // one pulse every 2.6 seconds
    final bob = math.sin(clock.value * 2 * math.pi * 3) * 3;
    return SizedBox(
      width: 150,
      height: 150,
      child: Stack(
        alignment: Alignment.center,
        children: [
          for (final shift in const [0.0, 0.5])
            Builder(builder: (context) {
              final p = (phase + shift) % 1;
              return Opacity(
                opacity: (1 - p) * 0.42,
                child: Container(
                  width: 78 + 70 * Curves.easeOut.transform(p),
                  height: 78 + 70 * Curves.easeOut.transform(p),
                  decoration: BoxDecoration(
                    shape: BoxShape.circle,
                    border: Border.all(color: AppColors.primary, width: 2),
                  ),
                ),
              );
            }),
          Transform.translate(
            offset: Offset(0, bob),
            child: Container(
              width: 78,
              height: 78,
              decoration: BoxDecoration(
                borderRadius: BorderRadius.circular(25),
                gradient: const LinearGradient(
                  begin: Alignment.topLeft,
                  end: Alignment.bottomRight,
                  colors: [Color(0xFF17B3A8), AppColors.primary],
                ),
                boxShadow: [
                  BoxShadow(color: AppColors.primary.withValues(alpha: 0.4), blurRadius: 26, offset: const Offset(0, 12)),
                ],
              ),
              child: const Icon(Icons.favorite_rounded, size: 40, color: Colors.white),
            ),
          ),
        ],
      ),
    );
  }
}

class _RoleCard extends StatefulWidget {
  final IconData icon;
  final String title;
  final String subtitle;
  final VoidCallback onTap;

  const _RoleCard({required this.icon, required this.title, required this.subtitle, required this.onTap});

  @override
  State<_RoleCard> createState() => _RoleCardState();
}

class _RoleCardState extends State<_RoleCard> {
  bool _hover = false;
  bool _pressed = false;

  @override
  Widget build(BuildContext context) {
    const radius = 22.0;
    final active = _hover || _pressed;
    return AnimatedScale(
      scale: _pressed ? 0.985 : 1,
      duration: const Duration(milliseconds: 120),
      curve: Curves.easeOut,
      child: AnimatedSlide(
        offset: Offset(0, _hover && !_pressed ? -0.035 : 0),
        duration: const Duration(milliseconds: 200),
        curve: Curves.easeOutCubic,
        child: AnimatedContainer(
          duration: const Duration(milliseconds: 220),
          curve: Curves.easeOutCubic,
          decoration: BoxDecoration(
            color: active ? const Color(0xFFF5FBFB) : Colors.white.withValues(alpha: 0.94),
            borderRadius: BorderRadius.circular(radius),
            border: Border.all(color: active ? AppColors.primary.withValues(alpha: 0.55) : AppColors.border, width: active ? 1.5 : 1),
            boxShadow: [
              BoxShadow(
                color: (active ? AppColors.primary : AppColors.ink).withValues(alpha: active ? 0.20 : 0.05),
                blurRadius: active ? 30 : 10,
                offset: Offset(0, active ? 14 : 4),
              ),
            ],
          ),
          child: Material(
            color: Colors.transparent,
            child: InkWell(
              borderRadius: BorderRadius.circular(radius),
              onTap: widget.onTap,
              onHover: (v) => setState(() => _hover = v),
              onHighlightChanged: (v) => setState(() => _pressed = v),
              child: Padding(
                padding: const EdgeInsets.all(18),
                child: Row(
                  children: [
                    AnimatedContainer(
                      duration: const Duration(milliseconds: 220),
                      width: 56,
                      height: 56,
                      decoration: BoxDecoration(
                        borderRadius: BorderRadius.circular(17),
                        gradient: LinearGradient(
                          begin: Alignment.topLeft,
                          end: Alignment.bottomRight,
                          colors: active
                              ? const [Color(0xFF17B3A8), AppColors.primary]
                              : [AppColors.primary.withValues(alpha: 0.10), AppColors.primary.withValues(alpha: 0.10)],
                        ),
                      ),
                      child: Icon(widget.icon, size: 28, color: active ? Colors.white : AppColors.primary),
                    ),
                    const SizedBox(width: 16),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(widget.title, style: Theme.of(context).textTheme.titleMedium),
                          const SizedBox(height: 3),
                          Text(widget.subtitle, style: Theme.of(context).textTheme.bodySmall),
                        ],
                      ),
                    ),
                    AnimatedSlide(
                      offset: Offset(active ? 0.25 : 0, 0),
                      duration: const Duration(milliseconds: 220),
                      curve: Curves.easeOutCubic,
                      child: Icon(Icons.arrow_forward_rounded, color: active ? AppColors.primary : AppColors.muted),
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
