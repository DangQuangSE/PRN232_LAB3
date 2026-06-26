using System.ComponentModel.DataAnnotations;

namespace PRN232.LMSSystem.CourseService.Models.Request;

public class EnrollmentRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "StudentId must be a positive integer.")]
    public int StudentId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "CourseId must be a positive integer.")]
    public int CourseId { get; set; }

    public DateTime EnrollDate { get; set; }

    [Required(ErrorMessage = "Status is required.")]
    [RegularExpression("^(Active|Completed|Dropped)$", ErrorMessage = "Status must be one of: Active, Completed, Dropped.")]
    public string Status { get; set; } = "Active";
}
