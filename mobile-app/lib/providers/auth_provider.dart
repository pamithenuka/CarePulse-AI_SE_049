import 'dart:convert';

import 'package:flutter/foundation.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

import '../models/auth_user.dart';
import '../services/api_client.dart';
import '../services/api_exception.dart';
import '../services/auth_service.dart';

enum AuthStatus { unknown, authenticated, unauthenticated }

/// Owns the session: current user, JWT, and persistence to secure storage so
/// the app doesn't force a fresh login every launch. Every other provider
/// reads [token]/[currentUser] from here rather than managing auth itself.
class AuthProvider extends ChangeNotifier {
  static const _storageKey = 'carepulse_session';

  final AuthService _authService;
  final FlutterSecureStorage _storage;

  AuthUser? _currentUser;
  AuthStatus _status = AuthStatus.unknown;
  bool _isLoading = false;
  String? _error;

  AuthProvider({AuthService? authService, FlutterSecureStorage? storage})
      : _authService = authService ?? AuthService(ApiClient()),
        _storage = storage ?? const FlutterSecureStorage();

  AuthUser? get currentUser => _currentUser;
  String? get token => _currentUser?.token;
  AuthStatus get status => _status;
  bool get isLoading => _isLoading;
  String? get error => _error;
  bool get isAuthenticated => _status == AuthStatus.authenticated;

  Future<void> tryAutoLogin() async {
    final raw = await _storage.read(key: _storageKey);
    if (raw == null) {
      _status = AuthStatus.unauthenticated;
      notifyListeners();
      return;
    }

    try {
      final user = AuthUser.fromJson(jsonDecode(raw) as Map<String, dynamic>);
      if (user.expiresAt.isBefore(DateTime.now())) {
        await _storage.delete(key: _storageKey);
        _status = AuthStatus.unauthenticated;
      } else {
        _currentUser = user;
        _status = AuthStatus.authenticated;
      }
    } catch (_) {
      await _storage.delete(key: _storageKey);
      _status = AuthStatus.unauthenticated;
    }
    notifyListeners();
  }

  Future<bool> login(String email, String password) => _run(() async {
        final user = await _authService.login(email, password);
        await _persist(user);
      });

  Future<bool> register(String fullName, String email, String password) => _run(() async {
        final user = await _authService.register(fullName, email, password);
        await _persist(user);
      });

  Future<void> logout() async {
    await _storage.delete(key: _storageKey);
    _currentUser = null;
    _status = AuthStatus.unauthenticated;
    notifyListeners();
  }

  Future<void> _persist(AuthUser user) async {
    _currentUser = user;
    _status = AuthStatus.authenticated;
    await _storage.write(key: _storageKey, value: jsonEncode(user.toJson()));
  }

  Future<bool> _run(Future<void> Function() action) async {
    _isLoading = true;
    _error = null;
    notifyListeners();
    try {
      await action();
      return true;
    } on ApiException catch (e) {
      _error = e.message;
      return false;
    } catch (e) {
      _error = 'Could not reach the server. Check your connection and try again.';
      return false;
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }
}
