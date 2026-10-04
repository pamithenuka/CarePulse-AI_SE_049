class EmergencyContact {
  final String? id;
  final String fullName;
  final String relationshipToPatient;
  final String phoneNumber;
  final bool isPrimary;

  EmergencyContact({
    this.id,
    required this.fullName,
    required this.relationshipToPatient,
    required this.phoneNumber,
    required this.isPrimary,
  });

  factory EmergencyContact.fromJson(Map<String, dynamic> json) => EmergencyContact(
        id: json['id'] as String?,
        fullName: json['fullName'] as String? ?? '',
        relationshipToPatient: json['relationshipToPatient'] as String? ?? '',
        phoneNumber: json['phoneNumber'] as String? ?? '',
        isPrimary: json['isPrimary'] as bool? ?? false,
      );

  Map<String, dynamic> toJson() => {
        'fullName': fullName,
        'relationshipToPatient': relationshipToPatient,
        'phoneNumber': phoneNumber,
        'isPrimary': isPrimary,
      };
}
