import 'dart:typed_data';

import '../models/emergency_contact.dart';
import '../models/medical_document.dart';
import '../models/medical_history.dart';
import '../models/patient_profile.dart';
import 'api_client.dart';

class PatientService {
  final ApiClient _client;

  PatientService(this._client);

  // ---------- Profile ----------

  Future<PatientProfile> createProfile(
    String token, {
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
    final json = await _client.post('/patients/profile', token: token, body: {
      'fullName': fullName,
      'dateOfBirth': _dateOnly(dateOfBirth),
      'gender': gender,
      'bloodType': bloodType,
      'phoneNumber': phoneNumber,
      'address': address,
      'nationalId': nationalId,
      'allergies': allergies,
      'emergencyContacts': emergencyContacts.map((c) => c.toJson()).toList(),
    });
    return PatientProfile.fromJson(json as Map<String, dynamic>);
  }

  Future<PatientProfile> getProfile(String token, String patientId) async {
    final json = await _client.get('/patients/$patientId', token: token);
    return PatientProfile.fromJson(json as Map<String, dynamic>);
  }

  /// Resolves the logged-in Patient's own profile. Throws [ApiException] with
  /// [ApiException.isNotFound] true when they haven't completed onboarding yet.
  Future<PatientProfile> getMyProfile(String token) async {
    final json = await _client.get('/patients/me', token: token);
    return PatientProfile.fromJson(json as Map<String, dynamic>);
  }

  /// Only phoneNumber/address take effect for a Patient - backend-api ignores
  /// every other UpdatePatientProfileDto field unless the requester is Admin.
  Future<PatientProfile> updateProfile(
    String token,
    String patientId, {
    String? phoneNumber,
    String? address,
  }) async {
    final json = await _client.put('/patients/$patientId', token: token, body: {
      'phoneNumber': ?phoneNumber,
      'address': ?address,
    });
    return PatientProfile.fromJson(json as Map<String, dynamic>);
  }

  // ---------- Medical history ----------

  Future<List<MedicalHistory>> getMedicalHistory(String token, String patientId) async {
    final json = await _client.get('/patients/$patientId/history', token: token);
    return (json as List<dynamic>).map((e) => MedicalHistory.fromJson(e as Map<String, dynamic>)).toList();
  }

  Future<List<MedicalHistory>> addMedicalHistory(
    String token,
    String patientId, {
    required String conditionName,
    String? notes,
    required DateTime diagnosedOn,
    required bool isChronic,
    String? currentMedications,
  }) async {
    final json = await _client.post('/patients/$patientId/history', token: token, body: {
      'conditionName': conditionName,
      'notes': notes,
      'diagnosedOn': _dateOnly(diagnosedOn),
      'isChronic': isChronic,
      'currentMedications': currentMedications,
    });
    return (json as List<dynamic>).map((e) => MedicalHistory.fromJson(e as Map<String, dynamic>)).toList();
  }

  // ---------- Emergency contacts ----------

  Future<List<EmergencyContact>> getEmergencyContacts(String token, String patientId) async {
    final json = await _client.get('/patients/$patientId/emergency-contacts', token: token);
    return (json as List<dynamic>).map((e) => EmergencyContact.fromJson(e as Map<String, dynamic>)).toList();
  }

  Future<List<EmergencyContact>> addEmergencyContact(String token, String patientId, EmergencyContact contact) async {
    final json = await _client.post('/patients/$patientId/emergency-contacts', token: token, body: contact.toJson());
    return (json as List<dynamic>).map((e) => EmergencyContact.fromJson(e as Map<String, dynamic>)).toList();
  }

  Future<EmergencyContact> updateEmergencyContact(
      String token, String patientId, String contactId, EmergencyContact contact) async {
    final json = await _client.put('/patients/$patientId/emergency-contacts/$contactId', token: token, body: contact.toJson());
    return EmergencyContact.fromJson(json as Map<String, dynamic>);
  }

  Future<void> deleteEmergencyContact(String token, String patientId, String contactId) =>
      _client.delete('/patients/$patientId/emergency-contacts/$contactId', token: token);

  // ---------- Documents ----------

  Future<List<MedicalDocument>> getDocuments(String token, String patientId) async {
    final json = await _client.get('/patients/$patientId/documents', token: token);
    return (json as List<dynamic>).map((e) => MedicalDocument.fromJson(e as Map<String, dynamic>)).toList();
  }

  Future<MedicalDocument> uploadDocument(
    String token,
    String patientId, {
    required Uint8List bytes,
    required String fileName,
    required String documentType,
  }) async {
    final json = await _client.postMultipart(
      '/patients/$patientId/documents',
      token: token,
      bytes: bytes,
      fileName: fileName,
      fileField: 'file',
      fields: {'documentType': MedicalDocument.documentTypeValue(documentType).toString()},
    );
    return MedicalDocument.fromJson(json as Map<String, dynamic>);
  }

  Future<void> deleteDocument(String token, String patientId, String documentId) =>
      _client.delete('/patients/$patientId/documents/$documentId', token: token);

  /// Fetches the raw file bytes for in-app preview via the auth-protected
  /// download endpoint (not the bare `fileUrl`, which isn't authenticated).
  Future<Uint8List> downloadDocumentBytes(String token, String patientId, String documentId) =>
      _client.getBytes('/patients/$patientId/documents/$documentId/download', token: token);

  // ---------- Emergency alert (SOS) ----------

  Future<Map<String, dynamic>> triggerEmergencyAlert(String token, String patientId) async {
    final json = await _client.post('/patients/$patientId/emergency-alert', token: token);
    return json as Map<String, dynamic>;
  }

  String _dateOnly(DateTime date) =>
      '${date.year.toString().padLeft(4, '0')}-${date.month.toString().padLeft(2, '0')}-${date.day.toString().padLeft(2, '0')}';
}
