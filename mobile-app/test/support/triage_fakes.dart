import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/models/patient_profile.dart';
import 'package:mobile_app/providers/auth_provider.dart';
import 'package:mobile_app/providers/patient_provider.dart';
import 'package:mobile_app/services/triage_api_service.dart';
import 'package:mobile_app/theme/app_theme.dart';
import 'package:provider/provider.dart';

Map<String, dynamic> assessment({String level = 'LOW', String? status}) => {
  'id': 'case-1',
  'riskLevel': level,
  'riskScore': level == 'LOW'
      ? 2
      : level == 'MEDIUM'
      ? 5
      : 8,
  'status':
      status ??
      (level == 'HIGH'
          ? 'NEEDS_DOCTOR_APPROVAL'
          : level == 'MEDIUM'
          ? 'DOCTOR_CONSULTATION_RECOMMENDED'
          : 'COMPLETED'),
  'requiresDoctorApproval': level == 'HIGH',
  'symptoms': 'Headache since this morning',
  'reason': 'Assessment explanation from the server.',
  'recommendedAction': level == 'LOW'
      ? 'SELF_CARE_MONITORING'
      : level == 'MEDIUM'
      ? 'DOCTOR_CONSULTATION'
      : 'DOCTOR_APPROVAL_REQUIRED',
  'recommendedSpecialty': 'GENERAL_MEDICINE',
  'followUpRecommended': true,
  'assessmentFailed': false,
  'workflowStatus': level == 'HIGH' ? 'AwaitingApproval' : 'Completed',
};

class FakeTriageApi extends TriageApiService {
  Map<String, dynamic> current = assessment();
  List<dynamic> logs = [];
  List<dynamic> cases = [];
  Map<String, dynamic>? payload;
  int submissions = 0;
  int statusCalls = 0;
  int auditCalls = 0;
  Object? submissionError;
  Object? statusError;
  Object? auditError;
  Completer<Map<String, dynamic>>? submission;
  Completer<Map<String, dynamic>>? statusRequest;

  @override
  Future<Map<String, dynamic>> submitTriage({
    required String patientProfileId,
    required String symptoms,
    required String duration,
    required String severity,
    required List<String> additionalSymptoms,
    required bool hasPhotoAttachment,
    double? latitude,
    double? longitude,
  }) async {
    submissions++;
    payload = {
      'patientProfileId': patientProfileId,
      'symptoms': symptoms,
      'duration': duration,
      'severity': severity,
      'additionalSymptoms': additionalSymptoms,
      'hasPhotoAttachment': hasPhotoAttachment,
      'latitude': latitude,
      'longitude': longitude,
    };
    if (submissionError != null) throw submissionError!;
    return submission?.future ?? current;
  }

  @override
  Future<Map<String, dynamic>> getStatus(String id) async {
    statusCalls++;
    if (statusError != null) throw statusError!;
    return statusRequest?.future ?? Map.of(current);
  }

  @override
  Future<List<dynamic>> getAuditLog(String id) async {
    auditCalls++;
    if (auditError != null) throw auditError!;
    return List.of(logs);
  }

  @override
  Future<List<dynamic>> getMine() async => cases;
}

Widget triageHarness(
  Widget child, {
  bool withProfile = true,
  double textScale = 1,
  double bottomInset = 0,
  String? fontFamily,
  Map<String, WidgetBuilder>? routes,
}) {
  final patient = PatientProvider(AuthProvider());
  final theme = buildAppTheme();
  if (withProfile) {
    patient.profile = PatientProfile.fromJson({
      'id': 'patient-1',
      'dateOfBirth': '2000-01-01',
      'createdAt': '2026-01-01',
    });
  }
  return ChangeNotifierProvider(
    create: (_) => patient,
    child: MaterialApp(
      debugShowCheckedModeBanner: false,
      theme: fontFamily == null
          ? theme
          : theme.copyWith(
              textTheme: theme.textTheme.apply(fontFamily: fontFamily),
              primaryTextTheme: theme.primaryTextTheme.apply(
                fontFamily: fontFamily,
              ),
              appBarTheme: theme.appBarTheme.copyWith(
                titleTextStyle: theme.appBarTheme.titleTextStyle?.copyWith(
                  fontFamily: fontFamily,
                ),
              ),
            ),
      builder: (context, child) => MediaQuery(
        data: MediaQuery.of(context).copyWith(
          textScaler: TextScaler.linear(textScale),
          viewInsets: EdgeInsets.only(bottom: bottomInset),
        ),
        child: child!,
      ),
      routes: routes ?? {},
      home: child,
    ),
  );
}

Future<void> tapVisible(WidgetTester tester, Finder finder) async {
  await tester.pumpAndSettle();
  if (finder.evaluate().isEmpty) {
    await tester.scrollUntilVisible(
      finder,
      120,
      scrollable: find.byType(Scrollable).last,
    );
  }
  await tester.ensureVisible(finder);
  await tester.pumpAndSettle();
  await tester.tap(finder);
  await tester.pumpAndSettle();
}
