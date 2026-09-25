import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:mobile_app/services/ai_service.dart';
import 'package:mobile_app/services/api_client.dart';

Map<String, dynamic> _fakePlanJson({String status = 'PlanCreated'}) => {
      'id': 'w1',
      'patientProfileId': 'p1',
      'objective': 'Patient reports chest pain',
      'summary': '36-year-old female, reports chest pain',
      'steps': [
        {'agent': 'DomainAnalysis', 'task': 'Score cardiac risk', 'status': 'Pending'},
        {'agent': 'ActionTool', 'task': 'Find nearest cardiologist', 'status': 'Pending'},
        {'agent': 'Validation', 'task': 'Pause for doctor if risk is high', 'status': 'Pending'},
      ],
      'status': status,
      'errorMessage': null,
      'modelUsed': 'llama3.2:1b',
      'toolCallSummary': 'GetPatientContextAsync(p1) -> age=36',
      'reviewStatus': 'NotReviewed',
      'reviewedByName': null,
      'reviewNotes': null,
      'createdAt': DateTime.now().toIso8601String(),
    };

void main() {
  group('AiService.createPlan', () {
    test('posts the objective and returns the created plan', () async {
      final mockClient = MockClient((request) async {
        expect(request.method, 'POST');
        expect(request.url.path, contains('/patients/p1/ai-plan'));
        final body = jsonDecode(request.body) as Map<String, dynamic>;
        expect(body['objective'], 'Patient reports chest pain');

        return http.Response(jsonEncode(_fakePlanJson()), 200, headers: {'content-type': 'application/json'});
      });

      final service = AiService(ApiClient(client: mockClient));
      final plan = await service.createPlan('token', 'p1', 'Patient reports chest pain');

      expect(plan.status, 'PlanCreated');
      expect(plan.steps, hasLength(3));
    });
  });

  group('AiService.getPlans', () {
    test('returns the list of plans for the patient', () async {
      final mockClient = MockClient((request) async {
        expect(request.method, 'GET');
        expect(request.url.path, contains('/patients/p1/ai-plan'));

        return http.Response(
          jsonEncode([_fakePlanJson(), _fakePlanJson(status: 'ValidationFailed')]),
          200,
          headers: {'content-type': 'application/json'},
        );
      });

      final service = AiService(ApiClient(client: mockClient));
      final plans = await service.getPlans('token', 'p1');

      expect(plans, hasLength(2));
      expect(plans.last.status, 'ValidationFailed');
    });
  });
}
