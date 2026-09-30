import 'package:flutter/material.dart';
import '../services/triage_api_service.dart';
import 'triage_status_screen.dart';

class TriageHistoryScreen extends StatefulWidget {
  const TriageHistoryScreen({super.key});
  @override
  State<TriageHistoryScreen> createState() => _TriageHistoryScreenState();
}
class _TriageHistoryScreenState extends State<TriageHistoryScreen> {
  late Future<List<dynamic>> _cases;
  @override
  void initState() { super.initState(); _cases = TriageApiService().getMine(); }
  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(title: const Text('My triage cases'), actions: [IconButton(icon: const Icon(Icons.refresh), onPressed: () => setState(() => _cases = TriageApiService().getMine()))]),
    body: FutureBuilder<List<dynamic>>(future: _cases, builder: (context, snapshot) {
      if (snapshot.hasError) return Center(child: Text(snapshot.error.toString()));
      if (!snapshot.hasData) return const Center(child: CircularProgressIndicator());
      if (snapshot.data!.isEmpty) return const Center(child: Text('No cases yet.'));
      return ListView(children: snapshot.data!.map((c) => ListTile(
        title: Text(c['status'].toString().replaceAll('_', ' ')), subtitle: Text(c['reason']?.toString() ?? ''),
        onTap: () => Navigator.push(context, MaterialPageRoute(builder: (_) => TriageStatusScreen(triageData: Map<String,dynamic>.from(c)))),
      )).toList());
    }),
  );
}
