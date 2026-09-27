import 'dart:convert';
import 'dart:typed_data';

import 'package:http/http.dart' as http;

import '../config/api_config.dart';
import 'api_exception.dart';

/// Thin wrapper around package:http. Every call takes the caller's JWT
/// explicitly (rather than holding auth state itself) so it stays a simple,
/// stateless collaborator that any provider/service can use.
class ApiClient {
  final http.Client _client;

  ApiClient({http.Client? client}) : _client = client ?? http.Client();

  Uri _uri(String path, [Map<String, dynamic>? query]) {
    final normalized = path.startsWith('/') ? path.substring(1) : path;
    final uri = Uri.parse('${ApiConfig.baseUrl}/$normalized');
    if (query == null || query.isEmpty) return uri;
    return uri.replace(queryParameters: {
      ...uri.queryParameters,
      ...query.map((key, value) => MapEntry(key, value.toString())),
    });
  }

  Map<String, String> _headers(String? token, {bool json = true}) => {
        if (json) 'Content-Type': 'application/json',
        'Accept': 'application/json',
        if (token != null) 'Authorization': 'Bearer $token',
      };

  Future<dynamic> get(String path, {String? token, Map<String, dynamic>? query}) async {
    final response = await _client.get(_uri(path, query), headers: _headers(token, json: false));
    return _handle(response);
  }

  Future<dynamic> post(String path, {String? token, Object? body}) async {
    final response = await _client.post(_uri(path), headers: _headers(token), body: body == null ? null : jsonEncode(body));
    return _handle(response);
  }

  Future<dynamic> put(String path, {String? token, Object? body}) async {
    final response = await _client.put(_uri(path), headers: _headers(token), body: body == null ? null : jsonEncode(body));
    return _handle(response);
  }

  Future<dynamic> delete(String path, {String? token}) async {
    final response = await _client.delete(_uri(path), headers: _headers(token, json: false));
    return _handle(response);
  }

  /// Fetches raw bytes (e.g. for previewing an uploaded document image) rather
  /// than decoding JSON. Reuses [_handle]'s error mapping, but returns the raw
  /// body bytes on success instead of a decoded object.
  Future<Uint8List> getBytes(String path, {String? token}) async {
    final response = await _client.get(_uri(path), headers: _headers(token, json: false));
    if (response.statusCode >= 200 && response.statusCode < 300) {
      return response.bodyBytes;
    }
    _handle(response);
    return Uint8List(0);
  }

  /// Multipart document upload (image_picker bytes + form fields). Takes raw
  /// bytes rather than a dart:io File - MultipartFile.fromPath needs real
  /// filesystem access and is not supported on Flutter Web, where an
  /// image_picker XFile's path is a blob: URL, not a real file path.
  Future<dynamic> postMultipart(
    String path, {
    String? token,
    required Uint8List bytes,
    required String fileName,
    required String fileField,
    Map<String, String> fields = const {},
  }) async {
    final request = http.MultipartRequest('POST', _uri(path));
    if (token != null) request.headers['Authorization'] = 'Bearer $token';
    request.fields.addAll(fields);
    request.files.add(http.MultipartFile.fromBytes(fileField, bytes, filename: fileName));

    final streamed = await _client.send(request);
    final response = await http.Response.fromStream(streamed);
    return _handle(response);
  }

  dynamic _handle(http.Response response) {
    final status = response.statusCode;
    if (status >= 200 && status < 300) {
      if (response.body.isEmpty) return null;
      return jsonDecode(response.body);
    }

    String message = 'Something went wrong (status $status).';
    if (response.body.isNotEmpty) {
      try {
        final decoded = jsonDecode(response.body);
        if (decoded is Map && decoded['message'] is String) {
          message = decoded['message'] as String;
        } else if (decoded is Map && decoded['errors'] != null) {
          message = _flattenValidationErrors(decoded['errors']);
        }
      } catch (_) {
        // Response body wasn't JSON - fall back to the generic message above.
      }
    }
    if (status == 401) message = 'Your session has expired. Please log in again.';
    if (status == 403) message = 'You are not allowed to do that.';

    throw ApiException(status, message);
  }

  String _flattenValidationErrors(dynamic errors) {
    if (errors is Map) {
      return errors.values.expand((v) => v is List ? v : [v]).join('\n');
    }
    if (errors is List) return errors.join('\n');
    return 'Validation failed.';
  }
}
