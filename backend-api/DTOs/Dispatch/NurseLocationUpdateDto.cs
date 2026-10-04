using FluentValidation;

namespace CarePulse.Api.DTOs.Dispatch;

public class NurseLocationUpdateDto
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double SpeedKmh { get; set; }
    public double Heading { get; set; }
}

public class NurseLocationUpdateDtoValidator : AbstractValidator<NurseLocationUpdateDto>
{
    public NurseLocationUpdateDtoValidator()
    {
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);
        RuleFor(x => x.SpeedKmh).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Heading).InclusiveBetween(0, 360);
    }
}
