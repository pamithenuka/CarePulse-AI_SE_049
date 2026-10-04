class MedicalHistory {
  final String id;
  final String conditionName;
  final String? notes;
  final DateTime diagnosedOn;
  final bool isChronic;
  final String? currentMedications;
  final bool isResolved;
  final DateTime? resolvedOn;
  final DateTime createdAt;

  MedicalHistory({
    required this.id,
    required this.conditionName,
    this.notes,
    required this.diagnosedOn,
    required this.isChronic,
    this.currentMedications,
    required this.isResolved,
    this.resolvedOn,
    required this.createdAt,
  });

  factory MedicalHistory.fromJson(Map<String, dynamic> json) => MedicalHistory(
        id: json['id'] as String,
        conditionName: json['conditionName'] as String? ?? '',
        notes: json['notes'] as String?,
        diagnosedOn: DateTime.parse(json['diagnosedOn'] as String),
        isChronic: json['isChronic'] as bool? ?? false,
        currentMedications: json['currentMedications'] as String?,
        isResolved: json['isResolved'] as bool? ?? false,
        resolvedOn: json['resolvedOn'] == null ? null : DateTime.parse(json['resolvedOn'] as String),
        createdAt: DateTime.parse(json['createdAt'] as String),
      );
}
