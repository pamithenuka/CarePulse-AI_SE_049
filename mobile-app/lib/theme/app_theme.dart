import 'package:flutter/material.dart';

// Same palette as the React web app, so the whole product feels like one
// system: deep clinical teal, calm off-white, muted status colours.
class AppColors {
  static const bg = Color(0xFFF2F6F7);
  static const blue = Color(0xFF1F5FAE);
  static const lavender = Color(0xFF7050A3);
  static const mint = Color(0xFF14804A);
  static const surface = Color(0xFFFFFFFF);
  static const primary = Color(0xFF0A8088);
  static const primaryDark = Color(0xFF07565C);
  static const deep = Color(0xFF0B3D45);
  static const ink = Color(0xFF0F2A33);
  static const ink2 = Color(0xFF3C5760);
  static const muted = Color(0xFF647E88);
  static const border = Color(0xFFDFE9EC);
  static const borderStrong = Color(0xFFC6D6DB);
  static const open = Color(0xFF14804A);
  static const openBg = Color(0xFFE5F5EC);
  static const booked = Color(0xFFB45A0B);
  static const bookedBg = Color(0xFFFDF0DE);
  static const danger = Color(0xFFC0362F);
  static const dangerBg = Color(0xFFFDECEA);
}

/// Soft, neutral page background used behind every screen.
const appBackground = BoxDecoration(
  gradient: LinearGradient(
    begin: Alignment.topCenter,
    end: Alignment.bottomCenter,
    colors: [Color(0xFFF7FAFB), Color(0xFFEBF2F4)],
  ),
);

/// Page route transition: the new screen fades in while rising a few pixels.
class _FadeRiseTransitionsBuilder extends PageTransitionsBuilder {
  const _FadeRiseTransitionsBuilder();

  @override
  Widget buildTransitions<T>(
    PageRoute<T> route,
    BuildContext context,
    Animation<double> animation,
    Animation<double> secondaryAnimation,
    Widget child,
  ) {
    final curved = CurvedAnimation(parent: animation, curve: Curves.easeOutCubic, reverseCurve: Curves.easeInCubic);
    return FadeTransition(
      opacity: curved,
      child: SlideTransition(
        position: Tween<Offset>(begin: const Offset(0, 0.04), end: Offset.zero).animate(curved),
        child: child,
      ),
    );
  }
}

ThemeData buildAppTheme({Color accent = AppColors.primary}) {
  final scheme = ColorScheme.fromSeed(
    seedColor: accent,
    primary: accent,
    surface: AppColors.surface,
    onSurface: AppColors.ink,
    error: AppColors.danger,
  );
  final base = ThemeData(
    useMaterial3: true,
    colorScheme: scheme,
    scaffoldBackgroundColor: Colors.transparent,
    splashFactory: InkSparkle.splashFactory,
    visualDensity: VisualDensity.standard,
  );

  RoundedRectangleBorder rounded(double r) => RoundedRectangleBorder(borderRadius: BorderRadius.circular(r));
  OutlineInputBorder outline(Color c, [double w = 1]) => OutlineInputBorder(
        borderRadius: BorderRadius.circular(14),
        borderSide: BorderSide(color: c, width: w),
      );

  return base.copyWith(
    // Smooth fade-and-rise between screens on every platform, plus soft hover and focus tints.
    pageTransitionsTheme: PageTransitionsTheme(
      builders: {for (final p in TargetPlatform.values) p: const _FadeRiseTransitionsBuilder()},
    ),
    hoverColor: accent.withValues(alpha: 0.06),
    focusColor: accent.withValues(alpha: 0.10),
    highlightColor: accent.withValues(alpha: 0.06),
    textTheme: base.textTheme.copyWith(
      headlineMedium: const TextStyle(fontSize: 28, fontWeight: FontWeight.w700, color: AppColors.ink, letterSpacing: -0.4, height: 1.2),
      headlineSmall: const TextStyle(fontSize: 24, fontWeight: FontWeight.w700, color: AppColors.ink, letterSpacing: -0.3, height: 1.25),
      titleLarge: const TextStyle(fontSize: 20, fontWeight: FontWeight.w700, color: AppColors.ink, letterSpacing: -0.2),
      titleMedium: const TextStyle(fontSize: 16, fontWeight: FontWeight.w600, color: AppColors.ink),
      titleSmall: const TextStyle(fontSize: 14, fontWeight: FontWeight.w600, color: AppColors.ink2),
      bodyLarge: const TextStyle(fontSize: 16, color: AppColors.ink, height: 1.45),
      bodyMedium: const TextStyle(fontSize: 14.5, color: AppColors.ink, height: 1.45),
      bodySmall: const TextStyle(fontSize: 12.5, color: AppColors.muted, height: 1.4),
      labelLarge: const TextStyle(fontSize: 14.5, fontWeight: FontWeight.w600),
    ),
    appBarTheme: const AppBarTheme(
      backgroundColor: AppColors.deep,
      foregroundColor: Colors.white,
      elevation: 0,
      scrolledUnderElevation: 0,
      centerTitle: false,
      iconTheme: IconThemeData(color: Colors.white),
      actionsIconTheme: IconThemeData(color: Colors.white),
      titleTextStyle: TextStyle(fontSize: 19, fontWeight: FontWeight.w700, color: Colors.white, letterSpacing: -0.2),
    ),
    navigationBarTheme: NavigationBarThemeData(
      backgroundColor: AppColors.surface,
      elevation: 8,
      height: 68,
      shadowColor: AppColors.ink.withValues(alpha: 0.15),
      surfaceTintColor: Colors.transparent,
      indicatorColor: accent.withValues(alpha: 0.14),
      indicatorShape: rounded(14),
      iconTheme: WidgetStateProperty.resolveWith((states) => IconThemeData(
            size: 24,
            color: states.contains(WidgetState.selected) ? accent : AppColors.muted,
          )),
      labelTextStyle: WidgetStateProperty.resolveWith((states) => TextStyle(
            fontSize: 12,
            fontWeight: states.contains(WidgetState.selected) ? FontWeight.w700 : FontWeight.w500,
            color: states.contains(WidgetState.selected) ? accent : AppColors.muted,
          )),
    ),
    filledButtonTheme: FilledButtonThemeData(
      style: FilledButton.styleFrom(
        minimumSize: const Size(48, 52),
        padding: const EdgeInsets.symmetric(horizontal: 22),
        shape: rounded(14),
        textStyle: const TextStyle(fontWeight: FontWeight.w700, fontSize: 15),
      ),
    ),
    elevatedButtonTheme: ElevatedButtonThemeData(
      style: ElevatedButton.styleFrom(
        backgroundColor: accent,
        foregroundColor: Colors.white,
        elevation: 0,
        minimumSize: const Size(48, 52),
        padding: const EdgeInsets.symmetric(horizontal: 22),
        shape: rounded(14),
        textStyle: const TextStyle(fontWeight: FontWeight.w700, fontSize: 15),
      ),
    ),
    outlinedButtonTheme: OutlinedButtonThemeData(
      style: OutlinedButton.styleFrom(
        minimumSize: const Size(48, 52),
        padding: const EdgeInsets.symmetric(horizontal: 22),
        foregroundColor: AppColors.ink2,
        side: const BorderSide(color: AppColors.borderStrong),
        shape: rounded(14),
        textStyle: const TextStyle(fontWeight: FontWeight.w600, fontSize: 15),
      ),
    ),
    textButtonTheme: TextButtonThemeData(
      style: TextButton.styleFrom(
        foregroundColor: AppColors.primaryDark,
        shape: rounded(10),
        textStyle: const TextStyle(fontWeight: FontWeight.w600),
      ),
    ),
    floatingActionButtonTheme: FloatingActionButtonThemeData(
      backgroundColor: accent,
      foregroundColor: Colors.white,
      elevation: 3,
      shape: rounded(16),
    ),
    dividerTheme: const DividerThemeData(color: AppColors.border, thickness: 1, space: 1),
    cardTheme: CardThemeData(
      color: AppColors.surface,
      elevation: 0,
      shadowColor: AppColors.ink.withValues(alpha: 0.08),
      surfaceTintColor: Colors.transparent,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(18),
        side: const BorderSide(color: AppColors.border),
      ),
      margin: EdgeInsets.zero,
    ),
    inputDecorationTheme: InputDecorationTheme(
      filled: true,
      fillColor: AppColors.surface,
      border: outline(AppColors.borderStrong),
      enabledBorder: outline(AppColors.borderStrong),
      focusedBorder: outline(accent, 2),
      errorBorder: outline(AppColors.danger),
      focusedErrorBorder: outline(AppColors.danger, 2),
      disabledBorder: outline(AppColors.border),
      contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 15),
      labelStyle: const TextStyle(color: AppColors.muted),
      floatingLabelStyle: TextStyle(color: accent, fontWeight: FontWeight.w600),
      hintStyle: const TextStyle(color: Color(0xFF9BB0B7)),
      errorStyle: const TextStyle(color: AppColors.danger, fontWeight: FontWeight.w500),
    ),
    chipTheme: base.chipTheme.copyWith(
      backgroundColor: const Color(0xFFEEF3F5),
      selectedColor: accent.withValues(alpha: 0.16),
      side: const BorderSide(color: AppColors.border),
      shape: rounded(999),
      labelStyle: const TextStyle(fontSize: 13, fontWeight: FontWeight.w600, color: AppColors.ink2),
    ),
    listTileTheme: ListTileThemeData(
      shape: rounded(14),
      iconColor: AppColors.ink2,
      titleTextStyle: const TextStyle(fontSize: 15, fontWeight: FontWeight.w600, color: AppColors.ink),
      subtitleTextStyle: const TextStyle(fontSize: 13, color: AppColors.muted),
    ),
    dialogTheme: DialogThemeData(
      backgroundColor: AppColors.surface,
      surfaceTintColor: Colors.transparent,
      shape: rounded(22),
      titleTextStyle: const TextStyle(fontSize: 19, fontWeight: FontWeight.w700, color: AppColors.ink),
    ),
    bottomSheetTheme: const BottomSheetThemeData(
      backgroundColor: AppColors.surface,
      surfaceTintColor: Colors.transparent,
      showDragHandle: true,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.vertical(top: Radius.circular(24))),
    ),
    snackBarTheme: SnackBarThemeData(
      behavior: SnackBarBehavior.floating,
      backgroundColor: AppColors.deep,
      contentTextStyle: const TextStyle(color: Colors.white, fontSize: 14),
      shape: rounded(12),
    ),
    progressIndicatorTheme: ProgressIndicatorThemeData(color: accent, linearTrackColor: AppColors.border),
    switchTheme: SwitchThemeData(
      thumbColor: WidgetStateProperty.resolveWith((s) => s.contains(WidgetState.selected) ? Colors.white : AppColors.muted),
      trackColor: WidgetStateProperty.resolveWith((s) => s.contains(WidgetState.selected) ? accent : AppColors.border),
    ),
    checkboxTheme: CheckboxThemeData(
      shape: rounded(5),
      fillColor: WidgetStateProperty.resolveWith((s) => s.contains(WidgetState.selected) ? accent : null),
    ),
  );
}
