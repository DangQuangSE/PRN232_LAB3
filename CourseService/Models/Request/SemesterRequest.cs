using System.ComponentModel.DataAnnotations;

namespace PRN232.LMSSystem.CourseService.Models.Request;

public class SemesterRequest
{
    [Required(ErrorMessage = "SemesterName is required.")]
    [MaxLength(100, ErrorMessage = "SemesterName must not exceed 100 characters.")]
    public string SemesterName { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }
}
