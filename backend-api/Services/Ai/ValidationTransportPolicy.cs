namespace CarePulse.Api.Services.Ai;

public static class ValidationTransportPolicy
{
    public static bool CheckCertificateRevocation(bool isDevelopment, bool isMacOS, bool allowLocalWorkaround)
        => !(isDevelopment && isMacOS && allowLocalWorkaround);
}
