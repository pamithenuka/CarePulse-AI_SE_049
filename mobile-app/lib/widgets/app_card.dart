import 'package:flutter/material.dart';

import '../theme/app_theme.dart';

/// Drop-in replacement for [Card] that lifts slightly and gains a soft shadow
/// when a mouse pointer hovers over it (web and desktop). On touch devices it
/// behaves like a normal card, and any [InkWell] inside it still shows its own
/// press ripple.
class AppCard extends StatefulWidget {
  const AppCard({super.key, required this.child, this.margin, this.clipBehavior, this.color});

  final Widget child;
  final EdgeInsetsGeometry? margin;
  final Clip? clipBehavior;
  final Color? color;

  @override
  State<AppCard> createState() => _AppCardState();
}

class _AppCardState extends State<AppCard> {
  bool _hover = false;

  @override
  Widget build(BuildContext context) {
    return MouseRegion(
      onEnter: (_) => setState(() => _hover = true),
      onExit: (_) => setState(() => _hover = false),
      child: AnimatedSlide(
        duration: const Duration(milliseconds: 180),
        curve: Curves.easeOutCubic,
        offset: Offset(0, _hover ? -0.012 : 0),
        child: AnimatedContainer(
          duration: const Duration(milliseconds: 180),
          curve: Curves.easeOutCubic,
          margin: widget.margin ?? EdgeInsets.zero,
          decoration: BoxDecoration(
            borderRadius: BorderRadius.circular(18),
            boxShadow: [
              BoxShadow(
                color: (_hover ? AppColors.primary : AppColors.ink).withValues(alpha: _hover ? 0.18 : 0.04),
                blurRadius: _hover ? 24 : 6,
                offset: Offset(0, _hover ? 10 : 2),
              ),
            ],
          ),
          child: Card(color: widget.color, clipBehavior: widget.clipBehavior, child: widget.child),
        ),
      ),
    );
  }
}
