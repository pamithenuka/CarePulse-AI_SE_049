import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/utils/validators.dart';

/// Student 1 (QM): boundary and invalid-input tests that extend validators_test.dart.
void main() {
  group('Validators.nationalId boundaries', () {
    test('rejects 11 digits', () => expect(Validators.nationalId('19901234567'), isNotNull));
    test('rejects 13 digits', () => expect(Validators.nationalId('1990123456789'), isNotNull));
    test('rejects 12 digits ending in a letter', () => expect(Validators.nationalId('19901234567V'), isNotNull));
    test('rejects 10 digits + V', () => expect(Validators.nationalId('1990123456V'), isNotNull));
    test('rejects 9 digits with no suffix', () => expect(Validators.nationalId('199012345'), isNotNull));
    test('accepts uppercase X suffix', () => expect(Validators.nationalId('199012345X'), isNull));
    test('rejects null', () => expect(Validators.nationalId(null), isNotNull));
    test('trims surrounding spaces before checking', () => expect(Validators.nationalId(' 199012345V '), isNull));
    test('rejects an injection-style string', () => expect(Validators.nationalId("1' OR '1'='1"), isNotNull));
  });

  group('Validators.phoneNumber boundaries', () {
    test('rejects 9 digits', () => expect(Validators.phoneNumber('077123456'), isNotNull));
    test('rejects 11 digits', () => expect(Validators.phoneNumber('07712345678'), isNotNull));
    test('rejects letters', () => expect(Validators.phoneNumber('07712abcde'), isNotNull));
    test('rejects international prefix', () => expect(Validators.phoneNumber('+94771234567'), isNotNull));
    test('rejects null', () => expect(Validators.phoneNumber(null), isNotNull));
    test('trims surrounding spaces', () => expect(Validators.phoneNumber(' 0771234567 '), isNull));
  });

  group('Validators.password boundaries', () {
    test('rejects exactly 7 characters', () => expect(Validators.password('abcdefg'), isNotNull));
    test('accepts exactly 8 characters', () => expect(Validators.password('abcdefgh'), isNull));
    test('rejects null', () => expect(Validators.password(null), isNotNull));
    test('rejects empty string', () => expect(Validators.password(''), isNotNull));
  });

  group('Validators.email edge cases', () {
    test('rejects a missing domain dot', () => expect(Validators.email('a@b'), isNotNull));
    test('rejects embedded whitespace', () => expect(Validators.email('a b@c.com'), isNotNull));
    test('rejects null', () => expect(Validators.email(null), isNotNull));
    test('rejects empty string', () => expect(Validators.email(''), isNotNull));
    test('trims surrounding spaces', () => expect(Validators.email(' a@b.com '), isNull));
  });

  group('Validators.dateOfBirth boundaries', () {
    test('accepts today at midnight', () {
      final now = DateTime.now();
      expect(Validators.dateOfBirth(DateTime(now.year, now.month, now.day)), isNull);
    });

    test('rejects tomorrow', () {
      expect(Validators.dateOfBirth(DateTime.now().add(const Duration(days: 1))), isNotNull);
    });

    test('accepts a very old date', () => expect(Validators.dateOfBirth(DateTime(1900, 1, 1)), isNull));
  });

  group('Validators.requiredField', () {
    test('uses the supplied label in the message', () {
      expect(Validators.requiredField('  ', label: 'Full name'), 'Full name is required.');
    });

    test('rejects null', () => expect(Validators.requiredField(null), isNotNull));
  });
}
