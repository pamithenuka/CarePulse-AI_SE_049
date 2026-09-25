using CarePulse.Api.Services.Notifications;

namespace CarePulse.Api.Tests.Fakes;

public class FakeNotificationService : INotificationService
{
    public List<(string PhoneNumber, string Message)> SentMessages { get; } = new();

    public Task<SmsSendResult> SendSmsAsync(string phoneNumber, string message, CancellationToken cancellationToken = default)
    {
        SentMessages.Add((phoneNumber, message));
        return Task.FromResult(new SmsSendResult(true, "fake-message-id", null));
    }
}
