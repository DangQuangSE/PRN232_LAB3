using System.ComponentModel.DataAnnotations;
using PRN232.LMSSystem.StudentService.Helpers;

namespace PRN232.LMSSystem.StudentService.Models.Request;

public class CreateStudentRequest
{
    [Required(ErrorMessage = "StudentCode is required.")]
    [StringLength(10, ErrorMessage = "StudentCode must not exceed 10 characters.")]
    [FptuStudentCode]
    public string StudentCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "FullName is required.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "FullName must be between 2 and 100 characters.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Email must be a valid email address.")]
    [StringLength(100, ErrorMessage = "Email must not exceed 100 characters.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone is required.")]
    [Phone(ErrorMessage = "Phone must be a valid phone format.")]
    [RegularExpression(@"^0\d{9,10}$", ErrorMessage = "Phone must be a valid Vietnamese mobile number (10 or 11 digits starting with 0).")]
    public string Phone { get; set; } = string.Empty;

    [Range(2026, 2035, ErrorMessage = "Expected graduation year must be between 2026 and 2035.")]
    public int ExpectedGraduationYear { get; set; } = 2028;

    [Required(ErrorMessage = "DateOfBirth is required.")]
    public DateTime DateOfBirth { get; set; }
}
