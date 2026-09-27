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
        RuleFor(x => x.HeartRate).GreaterThan(0);
        RuleFor(x => x.BloodPressure).NotEmpty().Matches(@"^\d{2,3}\/\d{2,3}$").WithMessage("Format must be Systolic/Diastolic (e.g., 120/80)");
        RuleFor(x => x.BodyTempC).InclusiveBetween(30, 45); // Reasonable limits
        RuleFor(x => x.OxygenSaturation).InclusiveBetween(0, 100);
        RuleFor(x => x.ClinicalNotes).NotEmpty();
    }
}
