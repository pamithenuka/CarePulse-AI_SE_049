using CarePulse.Api.Services.Common;

namespace CarePulse.Api.Tests.Fakes;

public class FakeCurrentUserService : ICurrentUserService
{
    public FakeCurrentUserService(string? userId = "test-actor")
    {
        UserId = userId;
    }

    public string? UserId { get; }
}
