import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/utils/validators.dart';

void main() {
  group('Validators.email', () {
    test('accepts a well-formed address', () {
      expect(Validators.email('patient@example.com'), isNull);
    });

    test('rejects a missing @', () {
      expect(Validators.email('not-an-email'), isNotNull);
    });
  });

  group('Validators.password', () {
    test('accepts 8+ characters', () {
      expect(Validators.password('longenough'), isNull);
    });

    test('rejects fewer than 8 characters', () {
      expect(Validators.password('short'), isNotNull);
    });
  });

  group('Validators.nationalId', () {
    test('accepts 9 digits + V', () {
      expect(Validators.nationalId('199012345V'), isNull);
    });

    test('accepts 9 digits + lowercase x', () {
      expect(Validators.nationalId('199012345x'), isNull);
    });

    test('accepts 12 digits', () {
      expect(Validators.nationalId('200012345678'), isNull);
    });

    test('rejects the wrong digit count', () {
      expect(Validators.nationalId('12345'), isNotNull);
    });
  });

  group('Validators.phoneNumber', () {
    test('accepts a 10-digit number starting with 0', () {
      expect(Validators.phoneNumber('0771234567'), isNull);
    });

    test('rejects a number without the leading 0', () {
      expect(Validators.phoneNumber('771234567'), isNotNull);
    });

    test('rejects a number that is too short', () {
      expect(Validators.phoneNumber('012345'), isNotNull);
    });
  });

  group('Validators.dateOfBirth', () {
    test('accepts a date in the past', () {
      expect(Validators.dateOfBirth(DateTime(1990, 1, 1)), isNull);
    });

    test('rejects a null date', () {
      expect(Validators.dateOfBirth(null), isNotNull);
    });

    test('rejects a date in the future', () {
      final tomorrow = DateTime.now().add(const Duration(days: 1));
      expect(Validators.dateOfBirth(tomorrow), isNotNull);
    });
  });

  group('Validators.requiredField', () {
    test('rejects null', () {
      expect(Validators.requiredField(null, label: 'Name'), isNotNull);
    });

    test('rejects blank/whitespace-only text', () {
      expect(Validators.requiredField('   ', label: 'Name'), isNotNull);
    });

    test('accepts non-empty text', () {
      expect(Validators.requiredField('Jane Doe', label: 'Name'), isNull);
    });
  });
}
