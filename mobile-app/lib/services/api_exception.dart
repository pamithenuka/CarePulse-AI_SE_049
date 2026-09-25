/// Thrown by [ApiClient] for any non-2xx response. [message] is either the
/// backend's `{ "message": "..." }` body or a generic fallback, so screens can
/// show it directly without knowing about HTTP status codes.
class ApiException implements Exception {
  final int statusCode;
  final String message;

  ApiException(this.statusCode, this.message);

  bool get isUnauthorized => statusCode == 401;
  bool get isForbidden => statusCode == 403;
  bool get isNotFound => statusCode == 404;

  @override
  String toString() => message;
}
