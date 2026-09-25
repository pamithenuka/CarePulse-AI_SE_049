class AuthUser {
  final String userId;
  final String fullName;
  final String email;
  final List<String> roles;
  final String token;
  final DateTime expiresAt;

  AuthUser({
    required this.userId,
    required this.fullName,
    required this.email,
    required this.roles,
    required this.token,
    required this.expiresAt,
  });

  bool get isPatient => roles.contains('Patient');

  factory AuthUser.fromJson(Map<String, dynamic> json) => AuthUser(
        userId: json['userId'] as String,
        fullName: json['fullName'] as String,
        email: json['email'] as String,
        roles: (json['roles'] as List<dynamic>).map((r) => r.toString()).toList(),
        token: json['token'] as String,
        expiresAt: DateTime.parse(json['expiresAt'] as String),
      );

  Map<String, dynamic> toJson() => {
        'userId': userId,
        'fullName': fullName,
        'email': email,
        'roles': roles,
        'token': token,
        'expiresAt': expiresAt.toIso8601String(),
      };
}
