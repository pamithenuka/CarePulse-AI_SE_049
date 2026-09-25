/// Mirrors the validation rules enforced both client-side on the web app
/// (web-admin/src/pages/RegisterPatientPage.js) and server-side via DataAnnotations
/// on CreatePatientProfileDto, so a patient gets the same feedback on mobile.
class Validators {
  Validators._();

  static final RegExp nicPattern = RegExp(r'^(\d{9}[VvXx]|\d{12})$');
  static final RegExp phonePattern = RegExp(r'^0\d{9}$');
  static final RegExp emailPattern = RegExp(r'^\S+@\S+\.\S+$');

  static String? requiredField(String? value, {String label = 'This field'}) {
    if (value == null || value.trim().isEmpty) return '$label is required.';
    return null;
  }

  static String? email(String? value) {
    if (value == null || !emailPattern.hasMatch(value.trim())) return 'Enter a valid email address.';
    return null;
  }

  static String? password(String? value) {
    if (value == null || value.length < 8) return 'Password must be at least 8 characters.';
    return null;
  }

  static String? nationalId(String? value) {
    if (value == null || !nicPattern.hasMatch(value.trim())) {
      return 'Enter 9 digits + V/X, or 12 digits.';
    }
    return null;
  }

  static String? phoneNumber(String? value) {
    if (value == null || !phonePattern.hasMatch(value.trim())) {
      return 'Enter a 10-digit number starting with 0.';
    }
    return null;
  }

  static String? dateOfBirth(DateTime? value) {
    if (value == null) return 'Date of birth is required.';
    if (value.isAfter(DateTime.now())) return 'Date of birth cannot be in the future.';
    return null;
  }
}
