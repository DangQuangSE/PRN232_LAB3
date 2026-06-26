using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace PRN232.LMSSystem.StudentService.Helpers;

public class FptuStudentCodeAttribute : ValidationAttribute
{
    private static readonly Regex FptuCodeRegex = new(@"^[A-Z]{2}\d{5,6}$", RegexOptions.Compiled);

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
        {
            return new ValidationResult("Student code is required.");
        }

        var code = value.ToString()!.Trim().ToUpper();

        if (!FptuCodeRegex.IsMatch(code))
        {
            return new ValidationResult("Student code must match FPTU style (e.g. SE19886, CE18793).");
        }

        var prefix = code.Substring(0, 2);
        string[] validPrefixes = { "SE", "CE", "IA", "GD", "HE", "MC", "DE", "QE", "SA" };
        
        if (!validPrefixes.Contains(prefix))
        {
            return new ValidationResult($"Invalid FPTU student major prefix '{prefix}'. Must be one of: {string.Join(", ", validPrefixes)}.");
        }

        return ValidationResult.Success;
    }
}
