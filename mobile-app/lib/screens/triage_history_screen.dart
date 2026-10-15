import 'package:flutter/material.dart';

import '../services/api_exception.dart';
import '../services/triage_api_service.dart';
import '../widgets/triage_widgets.dart';
import 'triage_status_screen.dart';

class TriageHistoryScreen extends StatefulWidget {
  final TriageApiService? apiService;
  const TriageHistoryScreen({super.key, this.apiService});

  @override
  State<TriageHistoryScreen> createState() => _TriageHistoryScreenState();
}

class _TriageHistoryScreenState extends State<TriageHistoryScreen> {
  late final _api = widget.apiService ?? TriageApiService();
  late Future<List<dynamic>> _cases;
  @override
  void initState() {
    super.initState();
    _cases = _api.getMine();
  }

  void _refresh() => setState(() => _cases = _api.getMine());

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(
      title: const Text('My triage cases'),
      actions: [
        IconButton(
          tooltip: 'Refresh history',
          icon: const Icon(Icons.refresh),
          onPressed: _refresh,
        ),
        const TriageSosAction(),
      ],
    ),
    body: SafeArea(
      child: FutureBuilder<List<dynamic>>(
        future: _cases,
        builder: (context, snapshot) {
          if (snapshot.hasError) {
            final error = snapshot.error;
            return ListView(
              padding: const EdgeInsets.all(20),
              children: [
                Text(
                  error is ApiException ? error.message : 'We could not load your history. Check your connection and try again.',
                ),
                const SizedBox(height: 12),
                OutlinedButton(
                  onPressed: _refresh,
                  child: const Text('Try again'),
                ),
              ],
            );
          }
          if (!snapshot.hasData) {
            return const Center(child: CircularProgressIndicator());
          }
          if (snapshot.data!.isEmpty) {
            return const Center(
              child: Padding(
                padding: EdgeInsets.all(24),
                child: Text(
                  'No cases are showing yet. If you just submitted symptoms, refresh before trying again.',
                ),
              ),
            );
          }
          return ListView.separated(
            padding: const EdgeInsets.all(16),
            itemCount: snapshot.data!.length,
            separatorBuilder: (_, _) => const SizedBox(height: 12),
            itemBuilder: (context, index) {
              final item = Map<String, dynamic>.from(snapshot.data![index]);
              return Card(
                child: ListTile(
                  title: Text(triageStatusLabel(item['status']?.toString())),
                  subtitle: Text(item['reason']?.toString() ?? ''),
                  trailing: const Icon(Icons.chevron_right),
                  onTap: () => Navigator.push(
                    context,
                    MaterialPageRoute(
                      builder: (_) => TriageStatusScreen(
                        triageData: item,
                        apiService: _api,
                      ),
                    ),
                  ),
                ),
              );
            },
          );
        },
      ),
    ),
  );
}
