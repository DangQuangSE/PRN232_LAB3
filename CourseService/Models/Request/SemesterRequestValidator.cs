using FluentValidation;

namespace PRN232.LMSSystem.CourseService.Models.Request;

public class SemesterRequestValidator : AbstractValidator<SemesterRequest>
{
    public SemesterRequestValidator()
    {
        RuleFor(x => x.SemesterName)
            .NotEmpty().WithMessage("SemesterName is required.")
            .MaximumLength(100).WithMessage("SemesterName must not exceed 100 characters.");

        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("StartDate is required.");

        RuleFor(x => x.EndDate)
            .NotEmpty().WithMessage("EndDate is required.")
            .GreaterThan(x => x.StartDate).WithMessage("EndDate must be after StartDate.");
    }
}
