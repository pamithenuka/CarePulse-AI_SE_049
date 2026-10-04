class MedicalDocument {
  final String id;
  final String fileName;
  final String fileUrl;
  final String documentType;
  final DateTime createdAt;

  MedicalDocument({
    required this.id,
    required this.fileName,
    required this.fileUrl,
    required this.documentType,
    required this.createdAt,
  });

  factory MedicalDocument.fromJson(Map<String, dynamic> json) => MedicalDocument(
        id: json['id'] as String,
        fileName: json['fileName'] as String? ?? '',
        fileUrl: json['fileUrl'] as String? ?? '',
        // The API serializes the DocumentType enum as its integer value.
        documentType: documentTypeLabel(json['documentType']),
        createdAt: DateTime.parse(json['createdAt'] as String),
      );

  // Order must match backend-api's MedicalDocumentType enum exactly - enums are
  // serialized as their integer ordinal (no JsonStringEnumConverter registered).
  static const List<String> _labels = ['Prescription', 'LabReport', 'ImagingScan', 'Other'];

  static String documentTypeLabel(dynamic rawValue) {
    if (rawValue is String) return rawValue;
    if (rawValue is int && rawValue >= 0 && rawValue < _labels.length) return _labels[rawValue];
    return 'Other';
  }

  static int documentTypeValue(String label) {
    final index = _labels.indexOf(label);
    return index == -1 ? _labels.length - 1 : index;
  }

  static List<String> get allTypes => _labels;
}
