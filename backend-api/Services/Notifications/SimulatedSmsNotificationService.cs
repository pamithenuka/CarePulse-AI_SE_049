namespace CarePulse.Api.Services.Notifications;

/// <summary>
/// Development/test stand-in for a real SMS provider (e.g. Twilio). Logs the
/// message instead of sending it so emergency-alert flows can be built and
/// demoed before third-party credentials are configured.
/// </summary>
public class SimulatedSmsNotificationService : INotificationService
{
    private readonly ILogger<SimulatedSmsNotificationService> _logger;

    public SimulatedSmsNotificationService(ILogger<SimulatedSmsNotificationService> logger)
    {
        _logger = logger;
    }

    public Task<SmsSendResult> SendSmsAsync(string phoneNumber, string message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[SIMULATED SMS] To: {PhoneNumber} | Message: {Message}", phoneNumber, message);
        return Task.FromResult(new SmsSendResult(true, Guid.NewGuid().ToString(), null));
    }
}
