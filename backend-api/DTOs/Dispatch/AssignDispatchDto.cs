using FluentValidation;

namespace CarePulse.Api.DTOs.Dispatch;

public class AssignDispatchDto
{
    public Guid TriageTicketId { get; set; }
    public Guid DoctorId { get; set; }
    public Guid NurseId { get; set; }
}

public class AssignDispatchDtoValidator : AbstractValidator<AssignDispatchDto>
{
    public AssignDispatchDtoValidator()
    {
        RuleFor(x => x.TriageTicketId).NotEmpty();
        RuleFor(x => x.DoctorId).NotEmpty();
        RuleFor(x => x.NurseId).NotEmpty();
    }
}
