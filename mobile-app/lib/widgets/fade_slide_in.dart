import 'package:flutter/material.dart';

/// Fades and slides its child in once, after an optional [delay].
///
/// It uses a single animation controller (no timers), so it is safe in widget
/// tests and respects the system "remove animations" setting.
class FadeSlideIn extends StatefulWidget {
  const FadeSlideIn({
    super.key,
    required this.child,
    this.delay = Duration.zero,
    this.duration = const Duration(milliseconds: 520),
    this.offset = const Offset(0, 0.12),
  });

  final Widget child;
  final Duration delay;
  final Duration duration;
  final Offset offset;

  @override
  State<FadeSlideIn> createState() => _FadeSlideInState();
}

class _FadeSlideInState extends State<FadeSlideIn> with SingleTickerProviderStateMixin {
  late final AnimationController _controller;
  late final Animation<double> _curve;

  @override
  void initState() {
    super.initState();
    final total = widget.delay + widget.duration;
    _controller = AnimationController(vsync: this, duration: total);
    final begin = total.inMilliseconds == 0 ? 0.0 : widget.delay.inMilliseconds / total.inMilliseconds;
    _curve = CurvedAnimation(parent: _controller, curve: Interval(begin, 1, curve: Curves.easeOutCubic));
    _controller.forward();
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    if (MediaQuery.maybeOf(context)?.disableAnimations ?? false) return widget.child;
    return FadeTransition(
      opacity: _curve,
      child: SlideTransition(
        position: Tween<Offset>(begin: widget.offset, end: Offset.zero).animate(_curve),
        child: widget.child,
      ),
    );
  }
}
