import 'package:flutter/material.dart';


// Same palette as the React doctor app, so the whole product feels like
// one system: deep clinical teal, calm off-white, muted status colors.
class AppColors {
  static const bg = Color(0xFFF3F7F8);
  static const blue = Color(0xFF245DB0);
  static const lavender = Color(0xFF7050A3);
  static const mint = Color(0xFF24745C);
  static const surface = Color(0xFFFFFFFF);
  static const primary = Color(0xFF087F83);
  static const primaryDark = Color(0xFF075C63);
  static const ink = Color(0xFF17343D);
  static const muted = Color(0xFF5B6B68);
  static const border = Color(0xFFDDE4E2);
  static const open = Color(0xFF3D7A52);
  static const openBg = Color(0xFFEAF4EC);
  static const booked = Color(0xFFA85433);
  static const bookedBg = Color(0xFFF6ECE5);
}

ThemeData buildAppTheme({Color accent = AppColors.primary}) {
  final base = ThemeData(
    useMaterial3: true,
    colorScheme: ColorScheme.fromSeed(
      seedColor: accent,
      primary: accent,
      surface: AppColors.surface,
    ),
    scaffoldBackgroundColor: Colors.transparent,
  );

  return base.copyWith(
    textTheme: base.textTheme.copyWith(
      headlineSmall: TextStyle(
        fontSize: 24,
        fontWeight: FontWeight.w600,
        color: AppColors.ink,
      ),
      titleLarge: TextStyle(
        fontSize: 20,
        fontWeight: FontWeight.w600,
        color: AppColors.ink,
      ),
      titleMedium: TextStyle(
        fontSize: 16,
        fontWeight: FontWeight.w600,
        color: AppColors.ink,
      ),
      bodyMedium: TextStyle(fontSize: 14, color: AppColors.ink),
      bodySmall: TextStyle(fontSize: 12.5, color: AppColors.muted),
    ),
    appBarTheme: AppBarTheme(
      backgroundColor: const Color(0xFF163657),
      foregroundColor: Colors.white,
      elevation: 0,
      titleTextStyle: TextStyle(
        fontSize: 19,
        fontWeight: FontWeight.w600,
        color: Colors.white,
      ),
    ),
    navigationBarTheme: NavigationBarThemeData(
      backgroundColor: AppColors.surface,
      indicatorColor: Color.alphaBlend(accent.withValues(alpha: 0.15), Colors.white),
      labelTextStyle: WidgetStateProperty.all(const TextStyle(fontSize: 12, fontWeight: FontWeight.w600)),
    ),
    filledButtonTheme: FilledButtonThemeData(style: FilledButton.styleFrom(
      minimumSize: const Size(48, 48),
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
    )),
    outlinedButtonTheme: OutlinedButtonThemeData(style: OutlinedButton.styleFrom(
      minimumSize: const Size(48, 48),
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
    )),
    floatingActionButtonTheme: FloatingActionButtonThemeData(
      backgroundColor: accent, foregroundColor: Colors.white, elevation: 2,
    ),
    dividerTheme: const DividerThemeData(color: AppColors.border),
    cardTheme: CardThemeData(
      color: AppColors.surface,
      elevation: 1,
      shadowColor: accent.withValues(alpha: 0.10),
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(16),
        side: const BorderSide(color: AppColors.border),
      ),
      margin: EdgeInsets.zero,
    ),
    elevatedButtonTheme: ElevatedButtonThemeData(
      style: ElevatedButton.styleFrom(
        backgroundColor: accent,
        foregroundColor: Colors.white,
        padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 14),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
        textStyle: TextStyle(fontWeight: FontWeight.w600, fontSize: 14),
      ),
    ),
    inputDecorationTheme: InputDecorationTheme(
      filled: true,
      fillColor: AppColors.surface,
      border: OutlineInputBorder(
        borderRadius: BorderRadius.circular(12),
        borderSide: const BorderSide(color: AppColors.border),
      ),
      enabledBorder: OutlineInputBorder(
        borderRadius: BorderRadius.circular(12),
        borderSide: const BorderSide(color: AppColors.border),
      ),
      contentPadding: const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
    ),
  );
}
