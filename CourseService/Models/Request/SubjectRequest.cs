using System.ComponentModel.DataAnnotations;

namespace PRN232.LMSSystem.CourseService.Models.Request;

public class SubjectRequest
{
    [Required(ErrorMessage = "SubjectCode is required.")]
    [MaxLength(20, ErrorMessage = "SubjectCode must not exceed 20 characters.")]
    [RegularExpression("^[A-Z0-9]+$", ErrorMessage = "SubjectCode must contain only uppercase letters and digits.")]
    public string SubjectCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "SubjectName is required.")]
    [MaxLength(200, ErrorMessage = "SubjectName must not exceed 200 characters.")]
    public string SubjectName { get; set; } = string.Empty;

    [Range(1, 10, ErrorMessage = "Credit must be between 1 and 10.")]
    public int Credit { get; set; }
}
