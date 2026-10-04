import '../models/ai_workflow.dart';
import 'api_client.dart';

class AiService {
  final ApiClient _client;

  AiService(this._client);

  Future<AiWorkflow> createPlan(String token, String patientId, String objective) async {
    final json = await _client.post('/patients/$patientId/ai-plan', token: token, body: {'objective': objective});
    return AiWorkflow.fromJson(json as Map<String, dynamic>);
  }

  Future<List<AiWorkflow>> getPlans(String token, String patientId) async {
    final json = await _client.get('/patients/$patientId/ai-plan', token: token);
    return (json as List<dynamic>).map((e) => AiWorkflow.fromJson(e as Map<String, dynamic>)).toList();
  }
}
