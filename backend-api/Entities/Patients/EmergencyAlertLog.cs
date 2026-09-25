namespace CarePulse.Api.Entities.Patients;

public enum NotificationDeliveryStatus
{
    Sent,
    Failed
}

/// <summary>
/// Immutable audit-style record — intentionally does NOT inherit BaseEntity
/// (no soft delete) since an emergency alert log must never be hidden or edited.
/// </summary>
public class EmergencyAlertLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PatientProfileId { get; set; }
    public PatientProfile? PatientProfile { get; set; }

    public string TriggeredByUserId { get; set; } = string.Empty;
    public DateTime TriggeredAt { get; set; } = DateTime.UtcNow;

    // Snapshot of the patient's key details at the moment of the alert.
    public string? BloodGroupSnapshot { get; set; }
    public string? AllergiesSnapshot { get; set; }
    public string? ActiveMedicationsSnapshot { get; set; }
    public string? ChronicConditionsSnapshot { get; set; }

    public ICollection<EmergencyAlertNotification> Notifications { get; set; } = new List<EmergencyAlertNotification>();
}

public class EmergencyAlertNotification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmergencyAlertLogId { get; set; }
    public EmergencyAlertLog? EmergencyAlertLog { get; set; }

    public string ContactName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public NotificationDeliveryStatus DeliveryStatus { get; set; }
    public DateTime? DeliveredAt { get; set; }
}
