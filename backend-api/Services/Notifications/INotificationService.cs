namespace CarePulse.Api.Services.Notifications;

public record SmsSendResult(bool Succeeded, string? ProviderMessageId, string? Error);

/// <summary>
/// Abstraction over the SMS provider used for emergency contact notifications.
/// Swap <see cref="SimulatedSmsNotificationService"/> for a Twilio-backed
/// implementation in Program.cs once account credentials are available —
/// no caller code needs to change.
/// </summary>
public interface INotificationService
{
    Task<SmsSendResult> SendSmsAsync(string phoneNumber, string message, CancellationToken cancellationToken = default);
}
