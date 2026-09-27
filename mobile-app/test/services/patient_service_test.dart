import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:mobile_app/models/emergency_contact.dart';
import 'package:mobile_app/services/api_client.dart';
import 'package:mobile_app/services/api_exception.dart';
import 'package:mobile_app/services/patient_service.dart';

Map<String, dynamic> _fakeProfileJson({String id = 'p1'}) => {
      'id': id,
      'userId': 'u1',
      'fullName': 'Jane Patient',
      'dateOfBirth': '1990-01-01',
      'age': 36,
      'gender': 'Female',
      'bloodType': 'O+',
      'phoneNumber': '0771234567',
      'address': '123 Main St',
      'nationalId': '199012345V',
      'allergies': 'Penicillin',
      'status': 'Active',
      'createdAt': DateTime.now().toIso8601String(),
      'emergencyContacts': [],
      'medicalHistories': [],
      'medicalDocuments': [],
    };

void main() {
  group('PatientService.createProfile', () {
    test('sends the profile fields and at least one emergency contact', () async {
      final mockClient = MockClient((request) async {
        expect(request.url.path, contains('/patients/profile'));
        final body = jsonDecode(request.body) as Map<String, dynamic>;
        expect(body['fullName'], 'Jane Patient');
        expect(body['dateOfBirth'], '1990-01-01');
        expect((body['emergencyContacts'] as List).length, 1);

        return http.Response(jsonEncode(_fakeProfileJson()), 200, headers: {'content-type': 'application/json'});
      });

      final service = PatientService(ApiClient(client: mockClient));
      final profile = await service.createProfile(
        'token',
        fullName: 'Jane Patient',
        dateOfBirth: DateTime(1990, 1, 1),
        gender: 'Female',
        phoneNumber: '0771234567',
        nationalId: '199012345V',
        emergencyContacts: [
          EmergencyContact(fullName: 'Next of Kin', relationshipToPatient: 'Spouse', phoneNumber: '0779999999', isPrimary: true),
        ],
      );

      expect(profile.fullName, 'Jane Patient');
      expect(profile.bloodType, 'O+');
    });
  });

  group('PatientService.getMyProfile', () {
    test('returns the profile on 200', () async {
      final mockClient = MockClient((request) async {
        expect(request.url.path, contains('/patients/me'));
        return http.Response(jsonEncode(_fakeProfileJson()), 200, headers: {'content-type': 'application/json'});
      });

      final service = PatientService(ApiClient(client: mockClient));
      final profile = await service.getMyProfile('token');

      expect(profile.id, 'p1');
    });

    test('throws a not-found ApiException when no profile exists yet', () async {
      final mockClient = MockClient((request) async {
        return http.Response(jsonEncode({'message': 'No patient profile has been created for this account yet.'}), 404);
      });

      final service = PatientService(ApiClient(client: mockClient));

      await expectLater(
        () => service.getMyProfile('token'),
        throwsA(isA<ApiException>().having((e) => e.isNotFound, 'isNotFound', isTrue)),
      );
    });
  });

  group('PatientService.updateProfile', () {
    test('omits null fields from the request body', () async {
      final mockClient = MockClient((request) async {
        final body = jsonDecode(request.body) as Map<String, dynamic>;
        expect(body.containsKey('phoneNumber'), isTrue);
        expect(body.containsKey('address'), isFalse);
        return http.Response(jsonEncode(_fakeProfileJson()), 200, headers: {'content-type': 'application/json'});
      });

      final service = PatientService(ApiClient(client: mockClient));
      await service.updateProfile('token', 'p1', phoneNumber: '0779999999');
    });
  });

  group('PatientService.uploadDocument', () {
    test('sends a multipart request with the file bytes and the numeric document type', () async {
      // Bytes + filename (not a dart:io File) - this is the cross-platform-safe
      // shape image_picker's XFile.readAsBytes() produces, including on Flutter Web.
      final bytes = utf8.encode('dummy document content');

      final mockClient = MockClient((request) async {
        // MockClient's basic handler always finalizes the request into a plain
        // http.Request (not http.MultipartRequest), with the encoded multipart
        // body already in request.body - so we assert on that wire format
        // instead of the original MultipartRequest object.
        expect(request.url.path, contains('/patients/p1/documents'));
        expect(request.headers['content-type'], contains('multipart/form-data'));
        expect(request.body, contains('name="file"'));
        expect(request.body, contains('dummy document content'));
        expect(request.body, contains('name="documentType"'));
        // Prescription=0, LabReport=1, ImagingScan=2, Other=3 - matches backend-api's enum order.
        expect(request.body, contains('\r\n1\r\n'));

        return http.Response(
          jsonEncode({
            'id': 'd1',
            'fileName': 'carepulse_test_upload.txt',
            'fileUrl': '/uploads/carepulse_test_upload.txt',
            'documentType': 1,
            'createdAt': DateTime.now().toIso8601String(),
          }),
          200,
          headers: {'content-type': 'application/json'},
        );
      });

      final service = PatientService(ApiClient(client: mockClient));
      final document = await service.uploadDocument(
        'token',
        'p1',
        bytes: bytes,
        fileName: 'carepulse_test_upload.txt',
        documentType: 'LabReport',
      );

      expect(document.documentType, 'LabReport');
    });
  });
}
