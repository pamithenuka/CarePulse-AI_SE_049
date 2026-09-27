class AppointmentSlot {
  final String slotId;
  final String doctorId;
  final String doctorName;
  final String specialty;
  final DateTime slotStart;
  final DateTime slotEnd;

  AppointmentSlot({
    required this.slotId,
    required this.doctorId,
    required this.doctorName,
    required this.specialty,
    required this.slotStart,
    required this.slotEnd,
  });

  factory AppointmentSlot.fromJson(Map<String, dynamic> json) {
    return AppointmentSlot(
      slotId: json['slotId'] as String,
      doctorId: json['doctorId'] as String,
      doctorName: json['doctorName'] as String,
      specialty: json['specialty'] as String,
      slotStart: DateTime.parse(json['slotStart'] as String).toLocal(),
      slotEnd: DateTime.parse(json['slotEnd'] as String).toLocal(),
    );
  }
}
