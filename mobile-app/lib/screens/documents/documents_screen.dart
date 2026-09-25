import 'package:flutter/material.dart';
import 'package:image_picker/image_picker.dart';
import 'package:provider/provider.dart';

import '../../models/medical_document.dart';
import '../../providers/patient_provider.dart';
import '../../widgets/empty_view.dart';

class DocumentsScreen extends StatelessWidget {
  const DocumentsScreen({super.key});

  Future<void> _pickAndUpload(BuildContext context, ImageSource source) async {
    final picker = ImagePicker();
    final picked = await picker.pickImage(source: source, imageQuality: 85);
    if (picked == null || !context.mounted) return;

    final documentType = await showModalBottomSheet<String>(
      context: context,
      builder: (_) => _DocumentTypePicker(),
    );
    if (documentType == null || !context.mounted) return;

    final bytes = await picked.readAsBytes();
    if (!context.mounted) return;
    final provider = context.read<PatientProvider>();
    final ok = await provider.uploadDocument(bytes: bytes, fileName: picked.name, documentType: documentType);
    if (!context.mounted) return;
    if (!ok) {
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(provider.error ?? 'Upload failed.')));
    }
  }

  void _showUploadOptions(BuildContext context) {
    showModalBottomSheet(
      context: context,
      builder: (sheetContext) => SafeArea(
        child: Wrap(
          children: [
            ListTile(
              leading: const Icon(Icons.photo_camera_outlined),
              title: const Text('Take a photo'),
              onTap: () {
                Navigator.of(sheetContext).pop();
                _pickAndUpload(context, ImageSource.camera);
              },
            ),
            ListTile(
              leading: const Icon(Icons.photo_library_outlined),
              title: const Text('Choose from gallery'),
              onTap: () {
                Navigator.of(sheetContext).pop();
                _pickAndUpload(context, ImageSource.gallery);
              },
            ),
          ],
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final patient = context.watch<PatientProvider>();
    final documents = patient.profile?.medicalDocuments ?? [];

    return Scaffold(
      appBar: AppBar(title: const Text('Documents')),
      body: RefreshIndicator(
        onRefresh: () => context.read<PatientProvider>().loadMyProfile(),
        child: documents.isEmpty
            ? ListView(
                children: const [
                  SizedBox(height: 120),
                  EmptyView(message: 'No documents uploaded yet.\nTap + to add a lab report, prescription or scan.', icon: Icons.folder_open_outlined),
                ],
              )
            : ListView.builder(
                padding: const EdgeInsets.all(16),
                itemCount: documents.length,
                itemBuilder: (context, index) => _DocumentCard(document: documents[index]),
              ),
      ),
      floatingActionButton: FloatingActionButton(
        onPressed: () => _showUploadOptions(context),
        child: const Icon(Icons.add_a_photo_outlined),
      ),
    );
  }
}

class _DocumentTypePicker extends StatelessWidget {
  @override
  Widget build(BuildContext context) {
    return SafeArea(
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Padding(padding: const EdgeInsets.all(16), child: Text('What kind of document is this?', style: Theme.of(context).textTheme.titleMedium)),
          ...MedicalDocument.allTypes.map(
            (type) => ListTile(title: Text(type), onTap: () => Navigator.of(context).pop(type)),
          ),
        ],
      ),
    );
  }
}

class _DocumentCard extends StatelessWidget {
  final MedicalDocument document;

  const _DocumentCard({required this.document});

  Future<void> _preview(BuildContext context) async {
    final provider = context.read<PatientProvider>();
    final bytes = await provider.downloadDocumentBytes(document.id);
    if (!context.mounted) return;
    if (bytes == null) {
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(provider.error ?? 'Could not open document.')));
      return;
    }
    showDialog(
      context: context,
      builder: (dialogContext) => Dialog(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            InteractiveViewer(child: Image.memory(bytes, errorBuilder: (context, error, stackTrace) => const _UnsupportedPreview())),
            TextButton(onPressed: () => Navigator.of(dialogContext).pop(), child: const Text('Close')),
          ],
        ),
      ),
    );
  }

  Future<void> _confirmDelete(BuildContext context) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (dialogContext) => AlertDialog(
        title: const Text('Delete document'),
        content: Text('Delete "${document.fileName}"?'),
        actions: [
          TextButton(onPressed: () => Navigator.of(dialogContext).pop(false), child: const Text('Cancel')),
          TextButton(onPressed: () => Navigator.of(dialogContext).pop(true), child: const Text('Delete')),
        ],
      ),
    );
    if (confirmed == true && context.mounted) {
      final provider = context.read<PatientProvider>();
      final ok = await provider.deleteDocument(document.id);
      if (!ok && context.mounted) {
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(provider.error ?? 'Failed to delete document.')));
      }
    }
  }

  IconData get _icon {
    switch (document.documentType) {
      case 'Prescription':
        return Icons.receipt_long_outlined;
      case 'LabReport':
        return Icons.biotech_outlined;
      case 'ImagingScan':
        return Icons.medical_information_outlined;
      default:
        return Icons.insert_drive_file_outlined;
    }
  }

  @override
  Widget build(BuildContext context) {
    return Card(
      margin: const EdgeInsets.only(bottom: 12),
      child: ListTile(
        leading: Icon(_icon),
        title: Text(document.fileName),
        subtitle: Text('${document.documentType} • ${document.createdAt.year}-${document.createdAt.month.toString().padLeft(2, '0')}-${document.createdAt.day.toString().padLeft(2, '0')}'),
        onTap: () => _preview(context),
        trailing: IconButton(icon: const Icon(Icons.delete_outline), onPressed: () => _confirmDelete(context)),
      ),
    );
  }
}

class _UnsupportedPreview extends StatelessWidget {
  const _UnsupportedPreview();

  @override
  Widget build(BuildContext context) {
    return const Padding(
      padding: EdgeInsets.all(32),
      child: Text('This file type cannot be previewed in-app.'),
    );
  }
}
