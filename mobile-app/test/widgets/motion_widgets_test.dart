import 'package:flutter/gestures.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/theme/app_theme.dart';
import 'package:mobile_app/widgets/app_card.dart';
import 'package:mobile_app/widgets/fade_slide_in.dart';
import 'package:mobile_app/widgets/tab_fade_in.dart';

Widget _app(Widget child, {bool disableAnimations = false}) => MaterialApp(
      theme: buildAppTheme(),
      home: MediaQuery(
        data: MediaQueryData(disableAnimations: disableAnimations),
        child: Scaffold(body: Center(child: child)),
      ),
    );

double _opacityOf(WidgetTester tester, Finder inside) =>
    tester.widget<FadeTransition>(find.descendant(of: inside, matching: find.byType(FadeTransition)).first).opacity.value;

void main() {
  group('AppCard', () {
    testWidgets('renders its child inside a Card and keeps the margin', (tester) async {
      await tester.pumpWidget(_app(const AppCard(margin: EdgeInsets.all(8), child: Text('hello'))));

      expect(find.text('hello'), findsOneWidget);
      expect(find.byType(Card), findsOneWidget);
    });

    testWidgets('lifts up when a mouse hovers and settles back when it leaves', (tester) async {
      await tester.pumpWidget(_app(const AppCard(child: SizedBox(width: 200, height: 80, child: Text('card')))));
      Offset slide() => tester.widget<AnimatedSlide>(find.byType(AnimatedSlide)).offset;
      expect(slide(), Offset.zero);

      final mouse = await tester.createGesture(kind: PointerDeviceKind.mouse);
      await mouse.addPointer(location: Offset.zero);
      addTearDown(mouse.removePointer);
      await mouse.moveTo(tester.getCenter(find.text('card')));
      await tester.pump();
      expect(slide().dy, lessThan(0), reason: 'hovering should raise the card');

      await mouse.moveTo(const Offset(1, 1));
      await tester.pump();
      expect(slide(), Offset.zero);
    });

    testWidgets('a tap on an InkWell inside still works', (tester) async {
      var taps = 0;
      await tester.pumpWidget(_app(AppCard(child: InkWell(onTap: () => taps++, child: const SizedBox(width: 120, height: 60, child: Text('tap me'))))));

      await tester.tap(find.text('tap me'));

      expect(taps, 1);
    });
  });

  group('FadeSlideIn', () {
    testWidgets('starts transparent and finishes fully visible', (tester) async {
      await tester.pumpWidget(_app(const FadeSlideIn(child: Text('content', key: ValueKey('c')))));
      final host = find.byType(FadeSlideIn);
      expect(_opacityOf(tester, host), 0);

      await tester.pumpAndSettle();

      expect(_opacityOf(tester, host), 1);
      expect(find.text('content'), findsOneWidget);
    });

    testWidgets('a delayed child is still hidden early and visible at the end', (tester) async {
      await tester.pumpWidget(_app(const FadeSlideIn(delay: Duration(milliseconds: 400), child: Text('later'))));
      final host = find.byType(FadeSlideIn);

      await tester.pump(const Duration(milliseconds: 200));
      expect(_opacityOf(tester, host), 0, reason: 'still inside the delay');

      await tester.pumpAndSettle();
      expect(_opacityOf(tester, host), 1);
    });

    testWidgets('skips the animation when the system asks to remove animations', (tester) async {
      await tester.pumpWidget(_app(const FadeSlideIn(child: Text('now')), disableAnimations: true));

      expect(find.descendant(of: find.byType(FadeSlideIn), matching: find.byType(FadeTransition)), findsNothing);
      expect(find.text('now'), findsOneWidget);
    });
  });

  group('TabFadeIn', () {
    testWidgets('replays the fade each time the tab becomes active and keeps its state', (tester) async {
      final active = ValueNotifier<bool>(false);
      await tester.pumpWidget(_app(ValueListenableBuilder<bool>(
        valueListenable: active,
        builder: (_, value, _) => TabFadeIn(active: value, child: const _Counter()),
      )));
      final host = find.byType(TabFadeIn);
      expect(_opacityOf(tester, host), 0, reason: 'an inactive tab starts hidden');

      active.value = true;
      await tester.pump();
      await tester.pump(const Duration(milliseconds: 80));
      expect(_opacityOf(tester, host), allOf(greaterThan(0), lessThan(1)));
      await tester.pumpAndSettle();
      expect(_opacityOf(tester, host), 1);

      await tester.tap(find.text('count 0'));
      await tester.pump();
      expect(find.text('count 1'), findsOneWidget);

      active.value = false;
      await tester.pump();
      active.value = true;
      await tester.pumpAndSettle();
      expect(find.text('count 1'), findsOneWidget, reason: 'state survives re-activation');
    });
  });
}

class _Counter extends StatefulWidget {
  const _Counter();

  @override
  State<_Counter> createState() => _CounterState();
}

class _CounterState extends State<_Counter> {
  int _n = 0;

  @override
  Widget build(BuildContext context) => TextButton(onPressed: () => setState(() => _n++), child: Text('count $_n'));
}
