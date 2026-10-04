import 'emergency_contact.dart';
import 'medical_document.dart';
import 'medical_history.dart';

class PatientProfile {
  final String id;
  final String userId;
  final String fullName;
  final DateTime dateOfBirth;
  final int age;
  final String gender;
  final String? bloodType;
  final String phoneNumber;
  final String? address;
  final String nationalId;
  final String? profilePhotoUrl;
  final String? allergies;
  final String status;
  final DateTime? lastEmergencyBroadcastAt;
  final DateTime createdAt;
  final List<EmergencyContact> emergencyContacts;
  final List<MedicalHistory> medicalHistories;
  final List<MedicalDocument> medicalDocuments;

  PatientProfile({
    required this.id,
    required this.userId,
    required this.fullName,
    required this.dateOfBirth,
    required this.age,
    required this.gender,
    this.bloodType,
    required this.phoneNumber,
    this.address,
    required this.nationalId,
    this.profilePhotoUrl,
    this.allergies,
    required this.status,
    this.lastEmergencyBroadcastAt,
    required this.createdAt,
    required this.emergencyContacts,
    required this.medicalHistories,
    required this.medicalDocuments,
  });

  factory PatientProfile.fromJson(Map<String, dynamic> json) => PatientProfile(
        id: json['id'] as String,
        userId: json['userId'] as String? ?? '',
        fullName: json['fullName'] as String? ?? '',
        dateOfBirth: DateTime.parse(json['dateOfBirth'] as String),
        age: json['age'] as int? ?? 0,
        gender: json['gender'] as String? ?? '',
        bloodType: json['bloodType'] as String?,
        phoneNumber: json['phoneNumber'] as String? ?? '',
        address: json['address'] as String?,
        nationalId: json['nationalId'] as String? ?? '',
        profilePhotoUrl: json['profilePhotoUrl'] as String?,
        allergies: json['allergies'] as String?,
        status: json['status'] as String? ?? 'Active',
        lastEmergencyBroadcastAt: json['lastEmergencyBroadcastAt'] == null
            ? null
            : DateTime.parse(json['lastEmergencyBroadcastAt'] as String),
        createdAt: DateTime.parse(json['createdAt'] as String),
        emergencyContacts: ((json['emergencyContacts'] as List<dynamic>?) ?? [])
            .map((e) => EmergencyContact.fromJson(e as Map<String, dynamic>))
            .toList(),
        medicalHistories: ((json['medicalHistories'] as List<dynamic>?) ?? [])
            .map((e) => MedicalHistory.fromJson(e as Map<String, dynamic>))
            .toList(),
        medicalDocuments: ((json['medicalDocuments'] as List<dynamic>?) ?? [])
            .map((e) => MedicalDocument.fromJson(e as Map<String, dynamic>))
            .toList(),
      );
}
