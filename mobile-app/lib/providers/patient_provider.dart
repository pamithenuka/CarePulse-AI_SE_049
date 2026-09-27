import 'package:flutter/foundation.dart';

import '../models/ai_workflow.dart';
import '../models/emergency_contact.dart';
import '../models/patient_profile.dart';
import '../services/ai_service.dart';
import '../services/api_client.dart';
import '../services/api_exception.dart';
import '../services/patient_service.dart';
import 'auth_provider.dart';

/// Holds the logged-in Patient's own profile (with its embedded medical
/// history/contacts/documents) plus their AI care plans, and re-fetches the
/// full profile after every mutation so all screens stay in sync from one
/// source of truth.
class PatientProvider extends ChangeNotifier {
  final PatientService _patientService;
  final AiService _aiService;
  AuthProvider _auth;

  PatientProfile? profile;
  List<AiWorkflow> aiPlans = [];
  bool isLoading = false;
  bool hasNoProfileYet = false;
  String? error;

  PatientProvider(AuthProvider auth, {PatientService? patientService, AiService? aiService})
      : _auth = auth,
        _patientService = patientService ?? PatientService(ApiClient()),
        _aiService = aiService ?? AiService(ApiClient());

  void updateAuth(AuthProvider auth) {
    _auth = auth;
    if (!auth.isAuthenticated) {
      profile = null;
      aiPlans = [];
      hasNoProfileYet = false;
    }
  }

  String get _token => _auth.token!;

  Future<T?> _run<T>(Future<T> Function() action, {bool notifyOnStart = true}) async {
    isLoading = true;
    error = null;
    if (notifyOnStart) notifyListeners();
    try {
      final result = await action();
      return result;
    } on ApiException catch (e) {
      error = e.message;
      return null;
    } catch (e) {
      error = 'Could not reach the server. Check your connection and try again.';
      return null;
    } finally {
      isLoading = false;
      notifyListeners();
    }
  }

  /// Loads the current patient's profile. On success, clears [hasNoProfileYet].
  /// On a 404, sets [hasNoProfileYet] so the UI can route to onboarding instead
  /// of showing a generic error.
  Future<void> loadMyProfile() async {
    isLoading = true;
    error = null;
    notifyListeners();
    try {
      profile = await _patientService.getMyProfile(_token);
      hasNoProfileYet = false;
    } on ApiException catch (e) {
      if (e.isNotFound) {
        hasNoProfileYet = true;
      } else {
        error = e.message;
      }
    } catch (_) {
      error = 'Could not reach the server. Check your connection and try again.';
    } finally {
      isLoading = false;
      notifyListeners();
    }
  }

  Future<bool> createProfile({
    required String fullName,
    required DateTime dateOfBirth,
    required String gender,
    String? bloodType,
    required String phoneNumber,
    String? address,
    required String nationalId,
    String? allergies,
    required List<EmergencyContact> emergencyContacts,
  }) async {
    final result = await _run(() => _patientService.createProfile(
          _token,
          fullName: fullName,
          dateOfBirth: dateOfBirth,
          gender: gender,
          bloodType: bloodType,
          phoneNumber: phoneNumber,
          address: address,
          nationalId: nationalId,
          allergies: allergies,
          emergencyContacts: emergencyContacts,
        ));
    if (result != null) {
      profile = result;
      hasNoProfileYet = false;
      return true;
    }
    return false;
  }

  // Only phoneNumber/address are patient-editable - backend-api's PatientService.UpdateProfileAsync
  // silently ignores every other field unless the requester is an Admin, so we don't expose them here.
  Future<bool> updateProfile({String? phoneNumber, String? address}) async {
    if (profile == null) return false;
    final result = await _run(() => _patientService.updateProfile(
          _token,
          profile!.id,
          phoneNumber: phoneNumber,
          address: address,
        ));
    if (result != null) {
      profile = result;
      return true;
    }
    return false;
  }

  Future<bool> addMedicalHistory({
    required String conditionName,
    String? notes,
    required DateTime diagnosedOn,
    required bool isChronic,
    String? currentMedications,
  }) async {
    if (profile == null) return false;
    final result = await _run(() => _patientService.addMedicalHistory(
          _token,
          profile!.id,
          conditionName: conditionName,
          notes: notes,
          diagnosedOn: diagnosedOn,
          isChronic: isChronic,
          currentMedications: currentMedications,
        ));
    if (result != null) {
      await loadMyProfile();
      return true;
    }
    return false;
  }

  Future<bool> addEmergencyContact(EmergencyContact contact) async {
    if (profile == null) return false;
    final result = await _run(() => _patientService.addEmergencyContact(_token, profile!.id, contact));
    if (result != null) {
      await loadMyProfile();
      return true;
    }
    return false;
  }

  Future<bool> updateEmergencyContact(String contactId, EmergencyContact contact) async {
    if (profile == null) return false;
    final result = await _run(() => _patientService.updateEmergencyContact(_token, profile!.id, contactId, contact));
    if (result != null) {
      await loadMyProfile();
      return true;
    }
    return false;
  }

  /// The backend requires at least one emergency contact to remain on file;
  /// callers should disable the delete action when only one contact is left
  /// so the resulting ApiException reads as an edge case, not the norm.
  Future<bool> deleteEmergencyContact(String contactId) async {
    if (profile == null) return false;
    final succeeded = await _run(() async {
      await _patientService.deleteEmergencyContact(_token, profile!.id, contactId);
      return true;
    });
    if (succeeded == true) {
      await loadMyProfile();
      return true;
    }
    return false;
  }

  Future<bool> uploadDocument({required Uint8List bytes, required String fileName, required String documentType}) async {
    if (profile == null) return false;
    final result = await _run(
        () => _patientService.uploadDocument(_token, profile!.id, bytes: bytes, fileName: fileName, documentType: documentType));
    if (result != null) {
      await loadMyProfile();
      return true;
    }
    return false;
  }

  Future<bool> deleteDocument(String documentId) async {
    if (profile == null) return false;
    final succeeded = await _run(() async {
      await _patientService.deleteDocument(_token, profile!.id, documentId);
      return true;
    });
    if (succeeded == true) {
      await loadMyProfile();
      return true;
    }
    return false;
  }

  Future<Uint8List?> downloadDocumentBytes(String documentId) {
    if (profile == null) return Future.value(null);
    return _run(() => _patientService.downloadDocumentBytes(_token, profile!.id, documentId), notifyOnStart: false);
  }

  Future<Map<String, dynamic>?> triggerEmergencyAlert() {
    if (profile == null) return Future.value(null);
    return _run(() => _patientService.triggerEmergencyAlert(_token, profile!.id));
  }

  Future<void> loadAiPlans() async {
    if (profile == null) return;
    final result = await _run(() => _aiService.getPlans(_token, profile!.id));
    if (result != null) aiPlans = result;
  }

  Future<bool> createAiPlan(String objective) async {
    if (profile == null) return false;
    final result = await _run(() => _aiService.createPlan(_token, profile!.id, objective));
    if (result != null) {
      aiPlans = [result, ...aiPlans];
      return true;
    }
    return false;
  }
}
