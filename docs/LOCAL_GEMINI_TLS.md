# Local Gemini certificate troubleshooting

If Agent 4 reports `RevocationStatusUnknown` on macOS, first check the system
clock and network/VPN. Semantic Kernel enables online certificate revocation
checking, which can fail even when normal TLS chain validation succeeds.

For an affected local machine, set `AI:AllowLocalMacOsRevocationWorkaround` to
`true` in the ignored `backend-api/appsettings.Local.json` and restart the API.
This option is effective only on macOS in the `Development` environment, and
only for the Validation Agent's named HTTP client. It skips revocation checking;
certificate trust, hostname and expiry validation remain enabled. Production
always checks revocation. Remove the option or set it to `false` once the local
revocation issue is resolved. Never commit your local configuration or API key.

Successful parsed responses produce `Agent 4 Gemini evaluation succeeded`.
`Agent 4 evaluating safety` alone only indicates that evaluation started.
The fallback warnings indicate that the Gemini evaluation did not complete.

Implementation reference: [Semantic Kernel HTTP client provider](https://github.com/microsoft/semantic-kernel/blob/main/dotnet/src/InternalUtilities/src/Http/HttpClientProvider.cs).
