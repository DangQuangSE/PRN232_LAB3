using System.ComponentModel.DataAnnotations;

namespace PRN232.LMSSystem.CourseService.Models.Request;

public class CourseRequest
{
    [Required(ErrorMessage = "CourseName is required.")]
    [MaxLength(100, ErrorMessage = "CourseName must not exceed 100 characters.")]
    public string CourseName { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "SemesterId must be a positive integer.")]
    public int SemesterId { get; set; }
}
