import 'slot.dart';

class AgentSearchResponse {
  final String reply;
  final List<AppointmentSlot> matchingSlots;

  AgentSearchResponse({required this.reply, required this.matchingSlots});

  factory AgentSearchResponse.fromJson(Map<String, dynamic> json) {
    final slotsJson = (json['matchingSlots'] as List?) ?? [];
    return AgentSearchResponse(
      reply: json['reply'] as String? ?? '',
      matchingSlots: slotsJson
          .map((e) => AppointmentSlot.fromJson(e as Map<String, dynamic>))
          .toList(),
    );
  }
}
