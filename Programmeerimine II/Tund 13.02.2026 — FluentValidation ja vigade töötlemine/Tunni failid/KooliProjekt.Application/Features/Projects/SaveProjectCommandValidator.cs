using FluentValidation;

namespace KooliProjekt.Application.Features.Projects;

public sealed class SaveProjectCommandValidator : AbstractValidator<SaveProjectCommand>
{
    public SaveProjectCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Project name is required.")
            .MaximumLength(200).WithMessage("Project name cannot exceed 200 characters.");
        RuleFor(x => x.Budget)
            .GreaterThanOrEqualTo(0).WithMessage("Budget must not be negative.");
        RuleFor(x => x.HourlyRate)
            .GreaterThanOrEqualTo(0).WithMessage("Hourly rate must not be negative.");
        RuleFor(x => x.Deadline)
            .GreaterThanOrEqualTo(x => x.StartDate).WithMessage("Deadline cannot be earlier than start date.");
    }
}
