import 'package:flutter/material.dart';
import '../api/api_client.dart';
import '../models/doctor.dart';
import '../theme/app_theme.dart';
import 'booking_screen.dart';
import 'agent_search_screen.dart';

class DoctorSearchScreen extends StatefulWidget {
  const DoctorSearchScreen({super.key});

  @override
  State<DoctorSearchScreen> createState() => _DoctorSearchScreenState();
}

class _DoctorSearchScreenState extends State<DoctorSearchScreen> {
  final _api = ApiClient();

  List<Doctor> _doctors = [];
  bool _loading = true;
  String? _error;

  String _searchText = '';
  String _selectedSpecialty = 'All';

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final doctors = await _api.getDoctors();
      setState(() => _doctors = doctors);
    } catch (e) {
      setState(() => _error = e.toString());
    } finally {
      setState(() => _loading = false);
    }
  }

  List<String> get _specialties {
    final set = _doctors.map((d) => d.specialty).toSet().toList()..sort();
    return ['All', ...set];
  }

  List<Doctor> get _filteredDoctors {
    return _doctors.where((d) {
      final matchesSpecialty =
          _selectedSpecialty == 'All' || d.specialty == _selectedSpecialty;
      final matchesSearch =
          _searchText.isEmpty ||
          d.fullName.toLowerCase().contains(_searchText.toLowerCase());
      return matchesSpecialty && matchesSearch;
    }).toList();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
            appBar: AppBar(
        title: const Text('Find a doctor'),
        actions: [
          IconButton(
            icon: const Icon(Icons.forum_outlined),
            tooltip: 'Ask CarePulse',
            onPressed: () {
              Navigator.of(context).push(
                MaterialPageRoute(builder: (_) => const AgentSearchScreen()),
              );
            },
          ),
        ],
      ),
      body: RefreshIndicator(
        onRefresh: _load,
        child: _buildBody(),
      ),
    );
  }

  Widget _buildBody() {
    if (_loading) {
      return const Center(child: CircularProgressIndicator());
    }

    if (_error != null) {
      return ListView(
        padding: const EdgeInsets.all(24),
        children: [
          const SizedBox(height: 60),
          Icon(Icons.wifi_off_rounded, size: 40, color: AppColors.muted),
          const SizedBox(height: 12),
          Text(
            "Couldn't reach the server.",
            textAlign: TextAlign.center,
            style: Theme.of(context).textTheme.titleMedium,
          ),
          const SizedBox(height: 6),
          Text(
            _error!,
            textAlign: TextAlign.center,
            style: Theme.of(context).textTheme.bodySmall,
          ),
          const SizedBox(height: 20),
          Center(
            child: OutlinedButton(onPressed: _load, child: const Text('Try again')),
          ),
        ],
      );
    }

    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        TextField(
          decoration: const InputDecoration(
            hintText: 'Search doctor by name',
            prefixIcon: Icon(Icons.search),
          ),
          onChanged: (v) => setState(() => _searchText = v),
        ),
        const SizedBox(height: 14),
        SizedBox(
          height: 36,
          child: ListView.separated(
            scrollDirection: Axis.horizontal,
            itemCount: _specialties.length,
            separatorBuilder: (_, __) => const SizedBox(width: 8),
            itemBuilder: (context, i) {
              final specialty = _specialties[i];
              final selected = specialty == _selectedSpecialty;
              return ChoiceChip(
                label: Text(specialty),
                selected: selected,
                onSelected: (_) => setState(() => _selectedSpecialty = specialty),
                selectedColor: AppColors.primary,
                backgroundColor: AppColors.surface,
                side: const BorderSide(color: AppColors.border),
                labelStyle: TextStyle(
                  color: selected ? Colors.white : AppColors.ink,
                  fontWeight: FontWeight.w600,
                  fontSize: 12.5,
                ),
              );
            },
          ),
        ),
        const SizedBox(height: 20),
        if (_filteredDoctors.isEmpty)
          Padding(
            padding: const EdgeInsets.symmetric(vertical: 60),
            child: Center(
              child: Text('No doctors match your search.', style: Theme.of(context).textTheme.bodySmall),
            ),
          )
        else
          ..._filteredDoctors.map((doctor) => _DoctorCard(doctor: doctor)),
      ],
    );
  }
}

class _DoctorCard extends StatelessWidget {
  final Doctor doctor;
  const _DoctorCard({required this.doctor});

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 10),
      child: Card(
        child: InkWell(
          borderRadius: BorderRadius.circular(10),
          onTap: () {
            Navigator.of(context).push(
              MaterialPageRoute(builder: (_) => BookingScreen(doctor: doctor)),
            );
          },
          child: Padding(
            padding: const EdgeInsets.all(16),
            child: Row(
              children: [
                CircleAvatar(
                  radius: 24,
                  backgroundColor: AppColors.openBg,
                  child: Text(
                    doctor.fullName.trim().isNotEmpty ? doctor.fullName.trim()[0] : '?',
                    style: const TextStyle(
                      color: AppColors.primaryDark,
                      fontWeight: FontWeight.w700,
                      fontSize: 18,
                    ),
                  ),
                ),
                const SizedBox(width: 14),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(doctor.fullName, style: Theme.of(context).textTheme.titleMedium),
                      const SizedBox(height: 3),
                      Text(doctor.specialty, style: Theme.of(context).textTheme.bodySmall),
                    ],
                  ),
                ),
                const Icon(Icons.chevron_right_rounded, color: AppColors.muted),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
