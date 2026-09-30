using FluentValidation;

namespace CarePulse.Api.DTOs.Dispatch;

public class AssignDispatchDto
{
    public Guid TriageTicketId { get; set; }
    public Guid DoctorId { get; set; }
    public bool AcknowledgeSafetyFlags { get; set; }
    public Guid NurseId { get; set; }
}

public class AssignDispatchDtoValidator : AbstractValidator<AssignDispatchDto>
{
    public AssignDispatchDtoValidator()
    {
        RuleFor(x => x.TriageTicketId).NotEmpty();
        // Doctor identity is derived from the authorized approval, never the client.
        RuleFor(x => x.NurseId).NotEmpty();
    }
}
