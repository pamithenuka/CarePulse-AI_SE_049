class AiPlanStep {
  final String agent;
  final String task;
  final String status;

  AiPlanStep({required this.agent, required this.task, required this.status});

  factory AiPlanStep.fromJson(Map<String, dynamic> json) => AiPlanStep(
        agent: json['agent'] as String? ?? '',
        task: json['task'] as String? ?? '',
        status: json['status'] as String? ?? 'Pending',
      );
}

class AiWorkflow {
  final String id;
  final String patientProfileId;
  final String objective;
  final String? summary;
  final List<AiPlanStep> steps;
  final String status;
  final String? errorMessage;
  final String modelUsed;
  final String toolCallSummary;
  final String reviewStatus;
  final String? reviewedByName;
  final String? reviewNotes;
  final DateTime createdAt;

  AiWorkflow({
    required this.id,
    required this.patientProfileId,
    required this.objective,
    this.summary,
    required this.steps,
    required this.status,
    this.errorMessage,
    required this.modelUsed,
    required this.toolCallSummary,
    required this.reviewStatus,
    this.reviewedByName,
    this.reviewNotes,
    required this.createdAt,
  });

  factory AiWorkflow.fromJson(Map<String, dynamic> json) => AiWorkflow(
        id: json['id'] as String,
        patientProfileId: json['patientProfileId'] as String? ?? '',
        objective: json['objective'] as String? ?? '',
        summary: json['summary'] as String?,
        steps: ((json['steps'] as List<dynamic>?) ?? [])
            .map((e) => AiPlanStep.fromJson(e as Map<String, dynamic>))
            .toList(),
        status: json['status'] as String? ?? '',
        errorMessage: json['errorMessage'] as String?,
        modelUsed: json['modelUsed'] as String? ?? '',
        toolCallSummary: json['toolCallSummary'] as String? ?? '',
        reviewStatus: json['reviewStatus'] as String? ?? 'NotReviewed',
        reviewedByName: json['reviewedByName'] as String?,
        reviewNotes: json['reviewNotes'] as String?,
        createdAt: DateTime.parse(json['createdAt'] as String),
      );
}
