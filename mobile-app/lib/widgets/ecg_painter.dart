import 'dart:ui' show PathMetric;

import 'package:flutter/material.dart';

/// A heartbeat line that draws itself from left to right, with a glowing tip.
class EcgPainter extends CustomPainter {
  EcgPainter({required this.progress, this.color = const Color(0xFF6EE7DD)});

  final double progress;
  final Color color;

  static const _points = [
    Offset(0, .5), Offset(.2, .5), Offset(.24, .5), Offset(.28, .1), Offset(.34, .92), Offset(.38, .5),
    Offset(.58, .5), Offset(.62, .22), Offset(.67, .8), Offset(.7, .5), Offset(1, .5),
  ];

  @override
  void paint(Canvas canvas, Size size) {
    final path = Path()..moveTo(_points.first.dx * size.width, _points.first.dy * size.height);
    for (final p in _points.skip(1)) {
      path.lineTo(p.dx * size.width, p.dy * size.height);
    }
    final metric = path.computeMetrics().fold<PathMetric?>(null, (a, b) => a ?? b);
    if (metric == null) return;
    final drawn = metric.extractPath(0, metric.length * progress);

    final glow = Paint()
      ..style = PaintingStyle.stroke
      ..strokeWidth = 5
      ..strokeCap = StrokeCap.round
      ..strokeJoin = StrokeJoin.round
      ..color = color.withValues(alpha: 0.35)
      ..maskFilter = const MaskFilter.blur(BlurStyle.normal, 6);
    final line = Paint()
      ..style = PaintingStyle.stroke
      ..strokeWidth = 2.4
      ..strokeCap = StrokeCap.round
      ..strokeJoin = StrokeJoin.round
      ..color = color;
    canvas.drawPath(drawn, glow);
    canvas.drawPath(drawn, line);

    final tip = metric.getTangentForOffset(metric.length * progress)?.position;
    if (tip != null && progress > 0.01) {
      canvas.drawCircle(tip, 4.5, Paint()..color = Colors.white);
      canvas.drawCircle(tip, 9, Paint()..color = color.withValues(alpha: 0.35));
    }
  }

  @override
  bool shouldRepaint(EcgPainter old) => old.progress != progress || old.color != color;
}
