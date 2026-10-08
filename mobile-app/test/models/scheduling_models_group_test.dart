import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/models/doctor.dart';
import 'package:mobile_app/models/slot.dart';

/// GROUP tests (not an individual student's contribution): parsing of the scheduling data
/// that the Flutter booking and doctor-search screens receive from the API.
void main() {
  Map<String, dynamic> validSlot() => {
        'slotId': 'slot-1',
        'doctorId': 'doc-1',
        'doctorName': 'Dr. Silva',
        'specialty': 'CARDIOLOGY',
        'slotStart': '2026-10-20T03:00:00Z',
        'slotEnd': '2026-10-20T03:30:00Z',
      };

  group('AppointmentSlot.fromJson', () {
    test('parses a valid slot and keeps the exact time as local time', () {
      final slot = AppointmentSlot.fromJson(validSlot());

      expect(slot.slotId, 'slot-1');
      expect(slot.doctorName, 'Dr. Silva');
      expect(slot.specialty, 'CARDIOLOGY');
      expect(slot.slotStart.isAtSameMomentAs(DateTime.utc(2026, 10, 20, 3, 0)), isTrue);
      expect(slot.slotEnd.difference(slot.slotStart), const Duration(minutes: 30));
      expect(slot.slotStart.isUtc, isFalse);
    });

    test('parses a time with a UTC offset to the same instant', () {
      final json = validSlot()..['slotStart'] = '2026-10-20T08:30:00+05:30';

      final slot = AppointmentSlot.fromJson(json);

      expect(slot.slotStart.isAtSameMomentAs(DateTime.utc(2026, 10, 20, 3, 0)), isTrue);
    });

    for (final field in ['slotId', 'doctorId', 'doctorName', 'specialty', 'slotStart', 'slotEnd']) {
      test('rejects a slot with a missing "$field"', () {
        final json = validSlot()..remove(field);

        expect(() => AppointmentSlot.fromJson(json), throwsA(anything));
      });
    }

    test('rejects an invalid date text', () {
      final json = validSlot()..['slotStart'] = 'next tuesday';

      expect(() => AppointmentSlot.fromJson(json), throwsFormatException);
    });

    test('rejects a wrong type for the id (number instead of text)', () {
      final json = validSlot()..['slotId'] = 42;

      expect(() => AppointmentSlot.fromJson(json), throwsA(isA<TypeError>()));
    });

    test('rejects null values', () {
      final json = validSlot()..['doctorName'] = null;

      expect(() => AppointmentSlot.fromJson(json), throwsA(isA<TypeError>()));
    });

    test('a list of slots keeps its order; an empty list gives an empty result', () {
      final slots = [validSlot(), validSlot()..['slotId'] = 'slot-2'].map(AppointmentSlot.fromJson).toList();

      expect(slots.map((s) => s.slotId), ['slot-1', 'slot-2']);
      expect(<Map<String, dynamic>>[].map(AppointmentSlot.fromJson).toList(), isEmpty);
    });
  });

  group('Doctor.fromJson', () {
    test('parses a valid doctor and ignores extra fields', () {
      final doctor = Doctor.fromJson({'id': 'd1', 'fullName': 'Dr. Silva', 'specialty': 'CARDIOLOGY', 'phoneNumber': '0771234567'});

      expect(doctor.id, 'd1');
      expect(doctor.fullName, 'Dr. Silva');
      expect(doctor.specialty, 'CARDIOLOGY');
    });

    for (final field in ['id', 'fullName', 'specialty']) {
      test('rejects a doctor with a missing "$field"', () {
        final json = {'id': 'd1', 'fullName': 'Dr. Silva', 'specialty': 'CARDIOLOGY'}..remove(field);

        expect(() => Doctor.fromJson(json), throwsA(anything));
      });
    }
  });
}
