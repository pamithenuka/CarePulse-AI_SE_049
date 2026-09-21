class Doctor {
  final String id;
  final String fullName;
  final String specialty;

  Doctor({required this.id, required this.fullName, required this.specialty});

  factory Doctor.fromJson(Map<String, dynamic> json) {
    return Doctor(
      id: json['id'] as String,
      fullName: json['fullName'] as String,
      specialty: json['specialty'] as String,
    );
  }
}
