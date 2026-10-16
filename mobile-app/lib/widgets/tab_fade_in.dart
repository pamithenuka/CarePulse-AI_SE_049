import 'package:flutter/material.dart';

/// Fades and slides a tab in every time it becomes the active one.
///
/// Used inside an [IndexedStack], so the tab's state is preserved; only the
/// entrance animation replays when the user switches to it.
class TabFadeIn extends StatefulWidget {
  const TabFadeIn({super.key, required this.active, required this.child});

  final bool active;
  final Widget child;

  @override
  State<TabFadeIn> createState() => _TabFadeInState();
}

class _TabFadeInState extends State<TabFadeIn> with SingleTickerProviderStateMixin {
  late final AnimationController _controller =
      AnimationController(vsync: this, duration: const Duration(milliseconds: 360), value: widget.active ? 1 : 0);
  late final Animation<double> _curve = CurvedAnimation(parent: _controller, curve: Curves.easeOutCubic);

  @override
  void didUpdateWidget(TabFadeIn oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (widget.active && !oldWidget.active) {
      if (MediaQuery.maybeOf(context)?.disableAnimations ?? false) {
        _controller.value = 1;
      } else {
        _controller.forward(from: 0);
      }
    }
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return FadeTransition(
      opacity: _curve,
      child: SlideTransition(
        position: Tween<Offset>(begin: const Offset(0, 0.025), end: Offset.zero).animate(_curve),
        child: widget.child,
      ),
    );
  }
}
