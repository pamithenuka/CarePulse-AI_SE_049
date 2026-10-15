using CarePulse.Api.Services.Ai;

namespace CarePulse.Ai.GoldenTests;

public class ValidationTransportPolicyTests
{
    [Theory]
    [InlineData(false, true, true, true)]
    [InlineData(true, false, true, true)]
    [InlineData(true, true, false, true)]
    [InlineData(true, true, true, false)]
    public void RevocationWorkaroundIsRestrictedToOptedInMacDevelopment(
        bool development, bool macOS, bool optedIn, bool expectedCheck)
    {
        Assert.Equal(expectedCheck,
            ValidationTransportPolicy.CheckCertificateRevocation(development, macOS, optedIn));
    }
}
