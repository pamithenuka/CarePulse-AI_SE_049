import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../models/ai_workflow.dart';
import '../../providers/patient_provider.dart';
import '../../widgets/empty_view.dart';
import '../../widgets/loading_view.dart';

/// Lets the Patient describe what they're experiencing (their own objective)
/// and view the resulting Agent 1 plan. Review/approve stays Doctor/Admin-only
/// on the web app - this screen only ever shows the plan's current review state.
class AiCarePlanScreen extends StatefulWidget {
  const AiCarePlanScreen({super.key});

  @override
  State<AiCarePlanScreen> createState() => _AiCarePlanScreenState();
}

class _AiCarePlanScreenState extends State<AiCarePlanScreen> {
  final _objectiveController = TextEditingController();
  bool _isSubmitting = false;
  String? _error;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) => context.read<PatientProvider>().loadAiPlans());
  }

  @override
  void dispose() {
    _objectiveController.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    final text = _objectiveController.text.trim();
    if (text.length < 5) {
      setState(() => _error = 'Describe what you\'re experiencing in a bit more detail.');
      return;
    }
    setState(() {
      _isSubmitting = true;
      _error = null;
    });
    final provider = context.read<PatientProvider>();
    final ok = await provider.createAiPlan(text);
    if (!mounted) return;
    setState(() {
      _isSubmitting = false;
      if (ok) {
        _objectiveController.clear();
      } else {
        _error = provider.error ?? 'Failed to generate a plan.';
      }
    });
  }

  @override
  Widget build(BuildContext context) {
    final patient = context.watch<PatientProvider>();

    return Scaffold(
      appBar: AppBar(title: const Text('AI Care Plan')),
      body: RefreshIndicator(
        onRefresh: () => context.read<PatientProvider>().loadAiPlans(),
        child: ListView(
          padding: const EdgeInsets.all(16),
          children: [
            Text('What are you experiencing?', style: Theme.of(context).textTheme.titleMedium),
            const SizedBox(height: 4),
            Text(
              'Agent 1 (Coordinator/Planner) reads your profile and your description to build a plan for your care team.',
              style: Theme.of(context).textTheme.bodySmall,
            ),
            const SizedBox(height: 12),
            TextField(
              controller: _objectiveController,
              maxLines: 3,
              decoration: const InputDecoration(
                labelText: 'Symptoms',
                hintText: 'e.g. "I have chest pain and shortness of breath"',
              ),
            ),
            if (_error != null) ...[
              const SizedBox(height: 8),
              Text(_error!, style: TextStyle(color: Theme.of(context).colorScheme.error)),
            ],
            const SizedBox(height: 12),
            FilledButton(
              onPressed: _isSubmitting ? null : _submit,
              child: _isSubmitting
                  ? const SizedBox(height: 20, width: 20, child: CircularProgressIndicator(strokeWidth: 2))
                  : const Text('Generate AI Plan'),
            ),
            const Divider(height: 32),
            Text('History', style: Theme.of(context).textTheme.titleMedium),
            const SizedBox(height: 8),
            if (patient.isLoading && patient.aiPlans.isEmpty)
              const LoadingView()
            else if (patient.aiPlans.isEmpty)
              const EmptyView(message: 'No AI plans have been generated yet.', icon: Icons.smart_toy_outlined)
            else
              ...patient.aiPlans.map((plan) => _PlanCard(plan: plan)),
          ],
        ),
      ),
    );
  }
}

class _PlanCard extends StatelessWidget {
  final AiWorkflow plan;

  const _PlanCard({required this.plan});

  Color _statusColor(BuildContext context) {
    switch (plan.status) {
      case 'PlanCreated':
        return Colors.green;
      case 'ValidationFailed':
      case 'LlmError':
        return Theme.of(context).colorScheme.error;
      default:
        return Theme.of(context).colorScheme.outline;
    }
  }

  @override
  Widget build(BuildContext context) {
    return Card(
      margin: const EdgeInsets.only(bottom: 12),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Expanded(child: Text(plan.objective, style: Theme.of(context).textTheme.titleSmall)),
                Chip(
                  label: Text(plan.status, style: const TextStyle(fontSize: 11)),
                  backgroundColor: _statusColor(context).withValues(alpha: 0.15),
                  visualDensity: VisualDensity.compact,
                ),
              ],
            ),
            if (plan.summary != null) ...[
              const SizedBox(height: 8),
              Text(plan.summary!),
            ],
            if (plan.errorMessage != null) ...[
              const SizedBox(height: 8),
              Text(plan.errorMessage!, style: TextStyle(color: Theme.of(context).colorScheme.error)),
            ],
            if (plan.steps.isNotEmpty) ...[
              const SizedBox(height: 8),
              ...plan.steps.map((step) => Padding(
                    padding: const EdgeInsets.only(bottom: 4),
                    child: Text('• ${step.agent}: ${step.task}', style: Theme.of(context).textTheme.bodySmall),
                  )),
            ],
            const SizedBox(height: 8),
            Row(
              children: [
                Icon(Icons.fact_check_outlined, size: 16, color: Theme.of(context).colorScheme.outline),
                const SizedBox(width: 4),
                Text(
                  plan.reviewStatus == 'NotReviewed'
                      ? 'Awaiting doctor review'
                      : '${plan.reviewStatus}${plan.reviewedByName != null ? ' by ${plan.reviewedByName}' : ''}',
                  style: Theme.of(context).textTheme.bodySmall,
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }
}
