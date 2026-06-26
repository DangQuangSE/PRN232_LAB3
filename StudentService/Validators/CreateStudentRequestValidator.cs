using FluentValidation;
using System.Text.RegularExpressions;
using PRN232.LMSSystem.StudentService.Models.Request;

namespace PRN232.LMSSystem.StudentService.Validators;

public class CreateStudentRequestValidator : AbstractValidator<CreateStudentRequest>
{
    private static readonly Regex FptuCodeRegex = new(@"^[A-Z]{2}\d{5,6}$", RegexOptions.Compiled);

    public CreateStudentRequestValidator()
    {
        RuleFor(x => x.StudentCode)
            .NotEmpty().WithMessage("StudentCode is required.")
            .Length(7, 8).WithMessage("StudentCode must be exactly 7 or 8 characters long.")
            .Must(BeValidFptuCode).WithMessage("StudentCode must match FPTU style (e.g. SE19886, CE18793) with a valid major prefix.");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("FullName is required.")
            .Length(2, 100).WithMessage("FullName must be between 2 and 100 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email must be a valid email address.")
            .MaximumLength(100).WithMessage("Email must not exceed 100 characters.");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Phone is required.")
            .Matches(@"^0\d{9,10}$").WithMessage("Phone must be a valid Vietnamese mobile number (10 or 11 digits starting with 0).");

        RuleFor(x => x.ExpectedGraduationYear)
            .InclusiveBetween(2026, 2035).WithMessage("Expected graduation year must be between 2026 and 2035.");

        RuleFor(x => x.DateOfBirth)
            .NotEmpty().WithMessage("DateOfBirth is required.")
            .Must(BeAtLeast16YearsOld).WithMessage("Student must be at least 16 years old.");
    }

    private bool BeValidFptuCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return false;
        var upperCode = code.Trim().ToUpper();
        if (!FptuCodeRegex.IsMatch(upperCode)) return false;

        var prefix = upperCode.Substring(0, 2);
        string[] validPrefixes = { "SE", "CE", "IA", "GD", "HE", "MC", "DE", "QE", "SA" };
        return validPrefixes.Contains(prefix);
    }

    private bool BeAtLeast16YearsOld(DateTime dob)
    {
        return dob <= DateTime.Today.AddYears(-16);
    }
}
