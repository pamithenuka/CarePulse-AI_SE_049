using FluentValidation;

namespace CarePulse.Api.DTOs.Dispatch;

public class CompleteOnsiteDto
{
    public int HeartRate { get; set; }
    public string BloodPressure { get; set; } = string.Empty;
    public double BodyTempC { get; set; }
    public int OxygenSaturation { get; set; }
    public string ClinicalNotes { get; set; } = string.Empty;
}

public class CompleteOnsiteDtoValidator : AbstractValidator<CompleteOnsiteDto>
{
    public CompleteOnsiteDtoValidator()
    {
        RuleFor(x => x.HeartRate).InclusiveBetween(0, 350);
        RuleFor(x => x.BloodPressure).NotEmpty().Must(value =>
        {
            var parts = value?.Split('/');
            return parts?.Length == 2 && int.TryParse(parts[0], out var systolic) && int.TryParse(parts[1], out var diastolic)
                && systolic >= 0 && systolic <= 350 && diastolic >= 0 && diastolic <= systolic;
        }).WithMessage("Use systolic/diastolic in 0–350 mmHg with systolic at least diastolic.");
        RuleFor(x => x.BodyTempC).InclusiveBetween(30, 45); // Reasonable limits
        RuleFor(x => x.OxygenSaturation).InclusiveBetween(0, 100);
        RuleFor(x => x.ClinicalNotes).NotEmpty().MaximumLength(2000);
    }
}
